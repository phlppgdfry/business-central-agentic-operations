using AgenticOperations.Domain;

namespace AgenticOperations.Application;

public interface IBusinessCentralClient
{
    Task<Customer?> FindCustomerAsync(string hint, string site, CancellationToken cancellationToken);
    Task<Item?> FindItemAsync(string hint, CancellationToken cancellationToken);
    Task<InventoryStatus> GetInventoryAsync(string itemNumber, CancellationToken cancellationToken);
    Task<SalesOrder> CreateSalesOrderAsync(string customerNumber, string itemNumber, int quantity, int backorderQuantity, string correlationId, CancellationToken cancellationToken);
}

public interface IOrderRequestExtractor { RequestedOrder Extract(string requestText); }
public interface IAuditTrail { void Append(string correlationId, string eventType, string detail); IReadOnlyList<AuditEvent> ForCorrelation(string correlationId); }
public interface IProposalRepository { void Save(OrderProposal proposal); OrderProposal? Get(Guid id); }

public sealed record CreateProposalCommand(string RequestText, string? CorrelationId = null);
