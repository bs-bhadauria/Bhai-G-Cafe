using System.Text.Json;
using BhaiGCafe.Api.Models;
using BhaiGCafe.Api.Options;
using Microsoft.Extensions.Options;

namespace BhaiGCafe.Api.Services;

public sealed class FileOrderStore : IOrderStore
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _ordersPath;

    public string Provider => "json-file";
    public bool IsConfigured => true;

    public FileOrderStore(IWebHostEnvironment environment, IOptions<StorageOptions> storageOptions)
    {
        var options = storageOptions.Value;
        var dataDirectory = Path.Combine(environment.ContentRootPath, options.DataDirectory);
        Directory.CreateDirectory(dataDirectory);
        _ordersPath = Path.Combine(dataDirectory, options.OrdersFile);
    }

    public async Task<List<OrderRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await ReadAllUnsafeAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<OrderRecord?> FindAsync(string publicOrderId, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var orders = await ReadAllUnsafeAsync(cancellationToken);
            return orders.FirstOrDefault(order => order.PublicOrderId.Equals(publicOrderId, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(OrderRecord order, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var orders = await ReadAllUnsafeAsync(cancellationToken);
            var existingIndex = orders.FindIndex(record => record.PublicOrderId.Equals(order.PublicOrderId, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                orders[existingIndex] = order;
            }
            else
            {
                orders.Add(order);
            }

            await using var stream = File.Create(_ordersPath);
            await JsonSerializer.SerializeAsync(stream, orders, new JsonSerializerOptions
            {
                WriteIndented = true
            }, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var orders = await ReadAllUnsafeAsync(cancellationToken);
            return orders.Count;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<OrderRecord>> ReadAllUnsafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_ordersPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_ordersPath);
        return await JsonSerializer.DeserializeAsync<List<OrderRecord>>(stream, cancellationToken: cancellationToken) ?? [];
    }
}
