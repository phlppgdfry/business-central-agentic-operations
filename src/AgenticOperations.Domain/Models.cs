namespace AgenticOperations.Domain;

public sealed record Customer(string Number, string Name, string Site);
public sealed record Item(string Number, string Description);
public sealed record InventoryStatus(string ItemNumber, int AvailableQuantity);
public sealed record RequestedOrder(string CustomerHint, string ItemHint, int Quantity, string RequestedSite, string OriginalText);
public sealed record SalesOrderLine(string ItemNumber, int Quantity, int BackorderQuantity);
public sealed record SalesOrder(string Number, string CustomerNumber, SalesOrderLine Line, DateTimeOffset CreatedAtUtc);

public enum ProposalStatus { PendingApproval, Approved, Rejected }

public sealed class OrderProposal
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string CorrelationId { get; init; }
    public required RequestedOrder Request { get; init; }
    public required Customer Customer { get; init; }
    public required Item Item { get; init; }
    public required InventoryStatus Inventory { get; init; }
    public required string Explanation { get; init; }
    public ProposalStatus Status { get; set; } = ProposalStatus.PendingApproval;
    public string? ApprovedBy { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record AuditEvent(DateTimeOffset OccurredAtUtc, string CorrelationId, string EventType, string Detail, string PreviousHash, string Hash);
