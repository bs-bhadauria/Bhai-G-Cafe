using BhaiGCafe.Api.Models;

namespace BhaiGCafe.Api.Services;

public interface IOrderStore
{
    string Provider { get; }
    bool IsConfigured { get; }
    Task<List<OrderRecord>> GetAllAsync(CancellationToken cancellationToken);
    Task<OrderRecord?> FindAsync(string publicOrderId, CancellationToken cancellationToken);
    Task SaveAsync(OrderRecord order, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
}
