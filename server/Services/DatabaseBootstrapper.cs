using BhaiGCafe.Api.Models;
using BhaiGCafe.Api.Options;
using Microsoft.Extensions.Options;

namespace BhaiGCafe.Api.Services;

public sealed class DatabaseBootstrapper
{
    private readonly IOrderStore _orderStore;
    private readonly PostgresOrderStore _postgresOrderStore;
    private readonly FileOrderStore _fileOrderStore;
    private readonly DatabaseOptions _databaseOptions;
    private readonly ILogger<DatabaseBootstrapper> _logger;

    public DatabaseBootstrapper(
        IOrderStore orderStore,
        PostgresOrderStore postgresOrderStore,
        FileOrderStore fileOrderStore,
        IOptions<DatabaseOptions> databaseOptions,
        ILogger<DatabaseBootstrapper> logger)
    {
        _orderStore = orderStore;
        _postgresOrderStore = postgresOrderStore;
        _fileOrderStore = fileOrderStore;
        _databaseOptions = databaseOptions.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!_postgresOrderStore.IsConfigured)
        {
            _logger.LogInformation("PostgreSQL connection string not configured. Falling back to {Provider} storage.", _orderStore.Provider);
            return;
        }

        if (_databaseOptions.AutoMigrateOnStartup)
        {
            await _postgresOrderStore.EnsureSchemaAsync(cancellationToken);
        }

        if (_databaseOptions.ImportLegacyJsonOnStartup)
        {
            await ImportLegacyOrdersAsync(cancellationToken);
        }
    }

    private async Task ImportLegacyOrdersAsync(CancellationToken cancellationToken)
    {
        var existingCount = await _postgresOrderStore.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            _logger.LogInformation("Skipping legacy JSON import because database already contains {Count} orders.", existingCount);
            return;
        }

        var legacyOrders = await _fileOrderStore.GetAllAsync(cancellationToken);
        if (legacyOrders.Count == 0)
        {
            _logger.LogInformation("No legacy JSON orders found to import.");
            return;
        }

        foreach (var order in legacyOrders.OrderBy(order => order.CreatedAtUtc))
        {
            await _postgresOrderStore.SaveAsync(order, cancellationToken);
        }

        _logger.LogInformation("Imported {Count} legacy orders into PostgreSQL.", legacyOrders.Count);
    }
}
