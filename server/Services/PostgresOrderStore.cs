using System.Text;
using BhaiGCafe.Api.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BhaiGCafe.Api.Services;

public sealed class PostgresOrderStore : IOrderStore
{
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PostgresOrderStore> _logger;

    public PostgresOrderStore(IConfiguration configuration, IWebHostEnvironment environment, ILogger<PostgresOrderStore> logger)
    {
        _connectionString = configuration.GetConnectionString("Postgres") ?? string.Empty;
        _environment = environment;
        _logger = logger;
    }

    public string Provider => "postgres";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureMigrationsTableAsync(connection, cancellationToken);

        var migrationsDirectory = Path.Combine(_environment.ContentRootPath, "Database", "Migrations");
        if (!Directory.Exists(migrationsDirectory))
        {
            _logger.LogWarning("PostgreSQL migrations directory was not found at {Path}.", migrationsDirectory);
            return;
        }

        var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var command = new NpgsqlCommand("select script_name from schema_migrations order by script_name;", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                applied.Add(reader.GetString(0));
            }
        }

        foreach (var migrationPath in Directory.GetFiles(migrationsDirectory, "*.sql").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var scriptName = Path.GetFileName(migrationPath);
            if (applied.Contains(scriptName))
            {
                continue;
            }

            var sql = await File.ReadAllTextAsync(migrationPath, cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var recordCommand = new NpgsqlCommand("insert into schema_migrations (script_name, applied_at_utc) values (@script_name, now() at time zone 'utc');", connection, transaction))
            {
                recordCommand.Parameters.AddWithValue("script_name", scriptName);
                await recordCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Applied PostgreSQL migration {Migration}.", scriptName);
        }
    }

    public async Task<List<OrderRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return [];
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var orders = new List<OrderRecord>();
        await using (var command = new NpgsqlCommand(BuildBaseOrderSelect() + " order by o.created_at_utc desc;", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                orders.Add(MapOrder(reader));
            }
        }

        if (orders.Count == 0)
        {
            return orders;
        }

        var itemsByOrderId = await LoadItemsByOrderIdsAsync(connection, orders.Select(order => order.Id).ToArray(), cancellationToken);
        return orders.Select(order => order with
        {
            Items = itemsByOrderId.TryGetValue(order.Id, out var items) ? items : []
        }).ToList();
    }

    public async Task<OrderRecord?> FindAsync(string publicOrderId, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return null;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        OrderRecord? order = null;
        await using (var command = new NpgsqlCommand(BuildBaseOrderSelect() + " where o.public_order_id = @public_order_id limit 1;", connection))
        {
            command.Parameters.AddWithValue("public_order_id", publicOrderId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                order = MapOrder(reader);
            }
        }

        if (order is null)
        {
            return null;
        }

        var itemsByOrderId = await LoadItemsByOrderIdsAsync(connection, [order.Id], cancellationToken);
        return order with
        {
            Items = itemsByOrderId.TryGetValue(order.Id, out var items) ? items : []
        };
    }

    public async Task SaveAsync(OrderRecord order, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("PostgreSQL is not configured.");
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var customerId = await UpsertCustomerAsync(connection, transaction, order.Customer, cancellationToken);
        var existingStatus = await GetExistingStatusAsync(connection, transaction, order.PublicOrderId, cancellationToken);
        await UpsertOrderAsync(connection, transaction, order, customerId, cancellationToken);
        await ReplaceOrderItemsAsync(connection, transaction, order, cancellationToken);
        await UpsertPaymentAsync(connection, transaction, order, cancellationToken);

        if (!string.Equals(existingStatus, order.Status, StringComparison.OrdinalIgnoreCase))
        {
            await InsertStatusHistoryAsync(connection, transaction, order.Id, order.Status, "system-sync", cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return 0;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("select count(*) from orders;", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task EnsureMigrationsTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            create table if not exists schema_migrations (
                script_name text primary key,
                applied_at_utc timestamptz not null
            );
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string BuildBaseOrderSelect()
    {
        return """
            select
                o.id,
                o.public_order_id,
                o.created_at_utc,
                o.currency,
                o.payment_method,
                o.status,
                o.delivery_type,
                o.delivery_address,
                o.table_number,
                o.special_instructions,
                o.subtotal,
                o.tax_amount,
                o.service_charge,
                o.delivery_fee,
                o.total_amount,
                c.full_name,
                c.email,
                c.phone,
                p.provider,
                p.status as payment_status,
                coalesce(p.provider_order_id, ''),
                coalesce(p.provider_payment_id, ''),
                p.paid_at_utc
            from orders o
            inner join customers c on c.id = o.customer_id
            left join payments p on p.order_id = o.id
            """;
    }

    private static OrderRecord MapOrder(NpgsqlDataReader record)
    {
        return new OrderRecord
        {
            Id = record.GetGuid(0),
            PublicOrderId = record.GetString(1),
            CreatedAtUtc = record.GetFieldValue<DateTimeOffset>(2),
            Currency = record.GetString(3),
            PaymentMethod = record.GetString(4),
            Status = record.GetString(5),
            Customer = new CustomerDetails
            {
                DeliveryType = record.GetString(6),
                Address = record.IsDBNull(7) ? string.Empty : record.GetString(7),
                TableNumber = record.IsDBNull(8) ? string.Empty : record.GetString(8),
                SpecialInstructions = record.IsDBNull(9) ? string.Empty : record.GetString(9),
                FullName = record.GetString(15),
                Email = record.GetString(16),
                Phone = record.GetString(17)
            },
            Pricing = new PricingBreakdown
            {
                Subtotal = record.GetDecimal(10),
                TaxAmount = record.GetDecimal(11),
                ServiceCharge = record.GetDecimal(12),
                DeliveryFee = record.GetDecimal(13),
                Total = record.GetDecimal(14)
            },
            Payment = new PaymentRecord
            {
                Provider = record.IsDBNull(18) ? string.Empty : record.GetString(18),
                Status = record.IsDBNull(19) ? "pending" : record.GetString(19),
                ProviderOrderId = record.IsDBNull(20) ? string.Empty : record.GetString(20),
                ProviderPaymentId = record.IsDBNull(21) ? string.Empty : record.GetString(21),
                PaidAtUtc = record.IsDBNull(22) ? null : record.GetFieldValue<DateTimeOffset>(22)
            }
        };
    }

    private static async Task<Guid> UpsertCustomerAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CustomerDetails customer, CancellationToken cancellationToken)
    {
        var existingId = Guid.Empty;
        await using (var findCommand = new NpgsqlCommand("select id from customers where email = @email and phone = @phone limit 1;", connection, transaction))
        {
            findCommand.Parameters.AddWithValue("email", customer.Email);
            findCommand.Parameters.AddWithValue("phone", customer.Phone);
            var scalar = await findCommand.ExecuteScalarAsync(cancellationToken);
            if (scalar is Guid id)
            {
                existingId = id;
            }
        }

        if (existingId != Guid.Empty)
        {
            await using var updateCommand = new NpgsqlCommand("""
                update customers
                set full_name = @full_name,
                    email = @email,
                    phone = @phone,
                    updated_at_utc = now() at time zone 'utc'
                where id = @id;
                """, connection, transaction);
            updateCommand.Parameters.AddWithValue("id", existingId);
            updateCommand.Parameters.AddWithValue("full_name", customer.FullName);
            updateCommand.Parameters.AddWithValue("email", customer.Email);
            updateCommand.Parameters.AddWithValue("phone", customer.Phone);
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            return existingId;
        }

        var customerId = Guid.NewGuid();
        await using (var insertCommand = new NpgsqlCommand("""
            insert into customers (id, full_name, email, phone, created_at_utc, updated_at_utc)
            values (@id, @full_name, @email, @phone, now() at time zone 'utc', now() at time zone 'utc');
            """, connection, transaction))
        {
            insertCommand.Parameters.AddWithValue("id", customerId);
            insertCommand.Parameters.AddWithValue("full_name", customer.FullName);
            insertCommand.Parameters.AddWithValue("email", customer.Email);
            insertCommand.Parameters.AddWithValue("phone", customer.Phone);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        return customerId;
    }

    private static async Task<string?> GetExistingStatusAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string publicOrderId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("select status from orders where public_order_id = @public_order_id limit 1;", connection, transaction);
        command.Parameters.AddWithValue("public_order_id", publicOrderId);
        return (string?)await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task UpsertOrderAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, OrderRecord order, Guid customerId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            insert into orders (
                id, public_order_id, created_at_utc, customer_id, currency, payment_method, status, delivery_type,
                delivery_address, table_number, special_instructions, subtotal, tax_amount, service_charge, delivery_fee, total_amount, updated_at_utc
            )
            values (
                @id, @public_order_id, @created_at_utc, @customer_id, @currency, @payment_method, @status, @delivery_type,
                @delivery_address, @table_number, @special_instructions, @subtotal, @tax_amount, @service_charge, @delivery_fee, @total_amount, now() at time zone 'utc'
            )
            on conflict (public_order_id) do update set
                customer_id = excluded.customer_id,
                currency = excluded.currency,
                payment_method = excluded.payment_method,
                status = excluded.status,
                delivery_type = excluded.delivery_type,
                delivery_address = excluded.delivery_address,
                table_number = excluded.table_number,
                special_instructions = excluded.special_instructions,
                subtotal = excluded.subtotal,
                tax_amount = excluded.tax_amount,
                service_charge = excluded.service_charge,
                delivery_fee = excluded.delivery_fee,
                total_amount = excluded.total_amount,
                updated_at_utc = now() at time zone 'utc';
            """, connection, transaction);

        command.Parameters.AddWithValue("id", order.Id);
        command.Parameters.AddWithValue("public_order_id", order.PublicOrderId);
        command.Parameters.AddWithValue("created_at_utc", order.CreatedAtUtc);
        command.Parameters.AddWithValue("customer_id", customerId);
        command.Parameters.AddWithValue("currency", order.Currency);
        command.Parameters.AddWithValue("payment_method", order.PaymentMethod);
        command.Parameters.AddWithValue("status", order.Status);
        command.Parameters.AddWithValue("delivery_type", order.Customer.DeliveryType);
        command.Parameters.AddWithValue("delivery_address", ToDbValue(order.Customer.Address));
        command.Parameters.AddWithValue("table_number", ToDbValue(order.Customer.TableNumber));
        command.Parameters.AddWithValue("special_instructions", ToDbValue(order.Customer.SpecialInstructions));
        command.Parameters.AddWithValue("subtotal", order.Pricing.Subtotal);
        command.Parameters.AddWithValue("tax_amount", order.Pricing.TaxAmount);
        command.Parameters.AddWithValue("service_charge", order.Pricing.ServiceCharge);
        command.Parameters.AddWithValue("delivery_fee", order.Pricing.DeliveryFee);
        command.Parameters.AddWithValue("total_amount", order.Pricing.Total);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ReplaceOrderItemsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, OrderRecord order, CancellationToken cancellationToken)
    {
        await using (var deleteCommand = new NpgsqlCommand("delete from order_items where order_id = @order_id;", connection, transaction))
        {
            deleteCommand.Parameters.AddWithValue("order_id", order.Id);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var item in order.Items)
        {
            await using var insertCommand = new NpgsqlCommand("""
                insert into order_items (id, order_id, menu_item_id, item_name, unit_price, quantity, line_total)
                values (@id, @order_id, @menu_item_id, @item_name, @unit_price, @quantity, @line_total);
                """, connection, transaction);
            insertCommand.Parameters.AddWithValue("id", Guid.NewGuid());
            insertCommand.Parameters.AddWithValue("order_id", order.Id);
            insertCommand.Parameters.AddWithValue("menu_item_id", item.MenuItemId);
            insertCommand.Parameters.AddWithValue("item_name", item.Name);
            insertCommand.Parameters.AddWithValue("unit_price", item.UnitPrice);
            insertCommand.Parameters.AddWithValue("quantity", item.Quantity);
            insertCommand.Parameters.AddWithValue("line_total", item.LineTotal);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task UpsertPaymentAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, OrderRecord order, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            insert into payments (order_id, provider, status, provider_order_id, provider_payment_id, paid_at_utc, updated_at_utc)
            values (@order_id, @provider, @status, @provider_order_id, @provider_payment_id, @paid_at_utc, now() at time zone 'utc')
            on conflict (order_id) do update set
                provider = excluded.provider,
                status = excluded.status,
                provider_order_id = excluded.provider_order_id,
                provider_payment_id = excluded.provider_payment_id,
                paid_at_utc = excluded.paid_at_utc,
                updated_at_utc = now() at time zone 'utc';
            """, connection, transaction);
        command.Parameters.AddWithValue("order_id", order.Id);
        command.Parameters.AddWithValue("provider", ToDbValue(order.Payment.Provider));
        command.Parameters.AddWithValue("status", order.Payment.Status);
        command.Parameters.AddWithValue("provider_order_id", ToDbValue(order.Payment.ProviderOrderId));
        command.Parameters.AddWithValue("provider_payment_id", ToDbValue(order.Payment.ProviderPaymentId));
        command.Parameters.AddWithValue("paid_at_utc", order.Payment.PaidAtUtc.HasValue ? order.Payment.PaidAtUtc.Value : DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertStatusHistoryAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid orderId, string status, string note, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            insert into order_status_history (order_id, status, note, changed_at_utc)
            values (@order_id, @status, @note, now() at time zone 'utc');
            """, connection, transaction);
        command.Parameters.AddWithValue("order_id", orderId);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("note", note);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Dictionary<Guid, List<OrderLine>>> LoadItemsByOrderIdsAsync(NpgsqlConnection connection, Guid[] orderIds, CancellationToken cancellationToken)
    {
        if (orderIds.Length == 0)
        {
            return [];
        }

        var items = new Dictionary<Guid, List<OrderLine>>();
        await using var command = new NpgsqlCommand("""
            select order_id, menu_item_id, item_name, unit_price, quantity, line_total
            from order_items
            where order_id = any(@order_ids)
            order by order_id, id;
            """, connection);
        command.Parameters.AddWithValue("order_ids", orderIds);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var orderId = reader.GetGuid(0);
            if (!items.TryGetValue(orderId, out var orderItems))
            {
                orderItems = [];
                items[orderId] = orderItems;
            }

            orderItems.Add(new OrderLine
            {
                MenuItemId = reader.GetString(1),
                Name = reader.GetString(2),
                UnitPrice = reader.GetDecimal(3),
                Quantity = reader.GetInt32(4),
                LineTotal = reader.GetDecimal(5)
            });
        }

        return items;
    }

    private static object ToDbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }
}
