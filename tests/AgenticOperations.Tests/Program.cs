using AgenticOperations.Application;
using AgenticOperations.Domain;
using AgenticOperations.Infrastructure;

var repository = new InMemoryProposalRepository();
var audit = new HashChainedAuditTrail();
var service = new OrderProposalService(new DemoBusinessCentralClient(), new OrderRequestExtractor(), repository, audit);
var proposal = await service.CreateAsync(new CreateProposalCommand("Please order another 40 units of product X for our Antwerp site.", "test-order-001"), CancellationToken.None);
Assert(proposal.Status == ProposalStatus.PendingApproval, "Proposal must await approval.");
Assert(proposal.Inventory.AvailableQuantity == 32, "Demo stock must be 32.");
Assert(proposal.Request.Quantity == 40, "Quantity extraction failed.");
var approved = await service.ApproveAsync(proposal.Id, "manager@contoso.example", CancellationToken.None);
Assert(approved.Status == ProposalStatus.Approved, "Proposal must be approved.");
Assert(approved.SalesOrder?.Line.BackorderQuantity == 8, "Backorder must be 8.");
Assert(audit.ForCorrelation("test-order-001").Count == 3, "Expected extraction, proposal and order audit events.");
Console.WriteLine("All end-to-end checks passed.");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
