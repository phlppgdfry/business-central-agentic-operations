using System.Text.Json;
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

// Approving twice, or twice at the same time, creates exactly one sales order.
var second = await service.CreateAsync(new CreateProposalCommand("Please order 5 units of product Y for Antwerp.", "test-order-002"), CancellationToken.None);
var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
{
    try { await service.ApproveAsync(second.Id, "manager@contoso.example", CancellationToken.None); return true; }
    catch (InvalidOperationException) { return false; }
}));
Assert(results.Count(ok => ok) == 1, "Concurrent approvals must create exactly one sales order.");
Assert(audit.ForCorrelation("test-order-002").Count(e => e.EventType == "SalesOrderCreated") == 1, "Exactly one SalesOrderCreated event expected.");

// The audit chain verifies, and any edit breaks it.
var chain = audit.All();
Assert(HashChainedAuditTrail.Verify(chain), "An untouched audit chain must verify.");
var tampered = chain.ToList();
tampered[1] = tampered[1] with { Detail = tampered[1].Detail.Replace("status=PendingApproval", "status=Approved") };
Assert(!HashChainedAuditTrail.Verify(tampered), "An edited audit event must break the chain.");
Assert(!HashChainedAuditTrail.Verify(chain.Skip(1).ToList()), "A removed audit event must break the chain.");

// Shared fixtures, also checked against the browser port (scripts/check-browser-port.mjs).
var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "parity", "extraction-cases.json")));
foreach (var c in fixtures.RootElement.GetProperty("cases").EnumerateArray())
{
    var name = c.GetProperty("name").GetString()!;
    var expected = c.GetProperty("expected");
    var fresh = new OrderProposalService(new DemoBusinessCentralClient(), new OrderRequestExtractor(), new InMemoryProposalRepository(), new HashChainedAuditTrail());
    try
    {
        var p = await fresh.CreateAsync(new CreateProposalCommand(c.GetProperty("request").GetString()!), CancellationToken.None);
        Assert(!expected.TryGetProperty("error", out _), $"{name}: expected a refusal, got a proposal.");
        Assert(p.Item.Number == expected.GetProperty("item").GetString(), $"{name}: wrong item {p.Item.Number}.");
        Assert(p.Request.Quantity == expected.GetProperty("quantity").GetInt32(), $"{name}: wrong quantity {p.Request.Quantity}.");
        Assert(p.Inventory.AvailableQuantity == expected.GetProperty("available").GetInt32(), $"{name}: wrong stock.");
        Assert(Math.Max(0, p.Request.Quantity - p.Inventory.AvailableQuantity) == expected.GetProperty("backorder").GetInt32(), $"{name}: wrong backorder.");
    }
    catch (ArgumentException ex)
    {
        Assert(expected.TryGetProperty("error", out var error) && ex.Message.Contains(error.GetString()!), $"{name}: unexpected refusal '{ex.Message}'.");
    }
}

Console.WriteLine("All end-to-end checks passed.");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
