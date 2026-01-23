using sobee_API.Domain;

namespace sobee_API.Services.Interfaces;

public interface IInventoryService
{
    Task<ServiceResult<bool>> ValidateAndDecrementAsync(IReadOnlyList<InventoryRequestItem> items);
}

public sealed record InventoryRequestItem(int ProductId, int Quantity);
