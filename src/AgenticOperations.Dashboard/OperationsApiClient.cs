using System.Net.Http.Json;

namespace AgenticOperations.Dashboard;

public sealed class OperationsApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<OrderProposalView>> GetProposalsAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<OrderProposalView>>("/api/order-proposals", ct) ?? [];

    public async Task<OrderProposalView> CreateDemoProposalAsync(CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync("/api/order-proposals", new { requestText = "Please order another 40 units of product X for our Antwerp site." }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderProposalView>(ct))!;
    }

    public async Task<OrderProposalView> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync($"/api/order-proposals/{id}/approve", new { approvedBy = "operations.manager@contoso.example" }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApprovalResponse>(ct))!.Proposal;
    }
}

public sealed record OrderProposalView(Guid Id, string CorrelationId, RequestedOrderView Request, CustomerView Customer, ItemView Item, InventoryView Inventory, string Explanation, string Status, string? ApprovedBy, SalesOrderView? SalesOrder, DateTimeOffset CreatedAtUtc);
public sealed record RequestedOrderView(string CustomerHint, string ItemHint, int Quantity, string RequestedSite, string OriginalText);
public sealed record CustomerView(string Number, string Name, string Site);
public sealed record ItemView(string Number, string Description);
public sealed record InventoryView(string ItemNumber, int AvailableQuantity);
public sealed record SalesOrderView(string Number, string CustomerNumber, SalesOrderLineView Line, DateTimeOffset CreatedAtUtc);
public sealed record SalesOrderLineView(string ItemNumber, int Quantity, int BackorderQuantity);
public sealed record ApprovalResponse(OrderProposalView Proposal);
