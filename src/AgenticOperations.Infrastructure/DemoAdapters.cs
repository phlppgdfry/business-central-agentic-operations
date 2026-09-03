using System.Security.Cryptography;
using System.Text;
using AgenticOperations.Application;
using AgenticOperations.Domain;

namespace AgenticOperations.Infrastructure;

public sealed class DemoBusinessCentralClient : IBusinessCentralClient
{
    private readonly Customer[] _customers = [new("ANTWERP", "Contoso Antwerp Site", "Antwerp")];
    private readonly Item[] _items = [new("PROD-X", "Product X"), new("PROD-Y", "Product Y")];
    private readonly Dictionary<string, int> _stock = new() { ["PROD-X"] = 32, ["PROD-Y"] = 120 };
    private int _sequence = 1000;
    public Task<Customer?> FindCustomerAsync(string hint, string site, CancellationToken ct) => Task.FromResult(_customers.FirstOrDefault(c => c.Number.Contains(hint, StringComparison.OrdinalIgnoreCase) || c.Site.Contains(site, StringComparison.OrdinalIgnoreCase)));
    public Task<Item?> FindItemAsync(string hint, CancellationToken ct) => Task.FromResult(_items.FirstOrDefault(i => i.Number.Contains(hint, StringComparison.OrdinalIgnoreCase) || i.Description.Contains(hint, StringComparison.OrdinalIgnoreCase)));
    public Task<InventoryStatus> GetInventoryAsync(string itemNumber, CancellationToken ct) => Task.FromResult(new InventoryStatus(itemNumber, _stock.GetValueOrDefault(itemNumber)));
    public Task<SalesOrder> CreateSalesOrderAsync(string customerNumber, string itemNumber, int quantity, int backorderQuantity, string correlationId, CancellationToken ct)
    {
        var number = $"SO-{Interlocked.Increment(ref _sequence)}";
        _stock[itemNumber] = Math.Max(0, _stock.GetValueOrDefault(itemNumber) - quantity);
        return Task.FromResult(new SalesOrder(number, customerNumber, new SalesOrderLine(itemNumber, quantity, backorderQuantity), DateTimeOffset.UtcNow));
    }
}

public sealed class OrderRequestExtractor : IOrderRequestExtractor
{
    public RequestedOrder Extract(string requestText)
    {
        var quantity = System.Text.RegularExpressions.Regex.Match(requestText, @"\b(\d+)\b") is var match && match.Success ? int.Parse(match.Groups[1].Value) : throw new ArgumentException("No quantity could be extracted.");
        var item = requestText.Contains("product y", StringComparison.OrdinalIgnoreCase) ? "PROD-Y" : "PROD-X";
        var site = requestText.Contains("antwerp", StringComparison.OrdinalIgnoreCase) ? "Antwerp" : "Antwerp";
        return new RequestedOrder("ANTWERP", item, quantity, site, requestText);
    }
}

public sealed class InMemoryProposalRepository : IProposalRepository
{
    private readonly Dictionary<Guid, OrderProposal> _items = new();
    public void Save(OrderProposal proposal) => _items[proposal.Id] = proposal;
    public OrderProposal? Get(Guid id) => _items.GetValueOrDefault(id);
    public IReadOnlyCollection<OrderProposal> GetAll() => _items.Values.ToArray();
}

public sealed class HashChainedAuditTrail : IAuditTrail
{
    private readonly List<AuditEvent> _events = [];
    public void Append(string correlationId, string eventType, string detail)
    {
        var previous = _events.LastOrDefault()?.Hash ?? "GENESIS";
        var occurred = DateTimeOffset.UtcNow;
        var payload = $"{previous}|{occurred:O}|{correlationId}|{eventType}|{detail}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        _events.Add(new AuditEvent(occurred, correlationId, eventType, detail, previous, hash));
    }
    public IReadOnlyList<AuditEvent> ForCorrelation(string correlationId) => _events.Where(e => e.CorrelationId == correlationId).ToList();
}
