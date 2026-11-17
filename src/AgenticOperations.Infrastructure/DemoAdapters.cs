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

public sealed partial class OrderRequestExtractor : IOrderRequestExtractor
{
    // Known demo items. An unknown product must be refused, never silently mapped to a default item.
    private static readonly (string Phrase, string Item)[] KnownItems =
        [("product x", "PROD-X"), ("prod-x", "PROD-X"), ("product y", "PROD-Y"), ("prod-y", "PROD-Y")];

    public RequestedOrder Extract(string requestText)
    {
        // Prefer a number followed by a unit ("40 units"), so "site 2" or a PO number is not read as the quantity.
        var match = QuantityWithUnit().Match(requestText);
        if (!match.Success) match = AnyNumber().Match(requestText);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var quantity))
            throw new ArgumentException("No quantity could be extracted.");
        if (quantity < 1) throw new ArgumentException("The quantity must be at least 1.");

        var items = KnownItems.Where(k => requestText.Contains(k.Phrase, StringComparison.OrdinalIgnoreCase)).Select(k => k.Item).Distinct().ToArray();
        if (items.Length == 0) throw new ArgumentException("No known item was named. The demo catalogue has Product X and Product Y.");
        if (items.Length > 1) throw new ArgumentException("More than one item was named; create one proposal per item.");
        if (!requestText.Contains("antwerp", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("No known customer site was named. The demo customer is the Antwerp site.");

        return new RequestedOrder("ANTWERP", items[0], quantity, "Antwerp", requestText);
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\b(\d+)\s*(?:units?|pcs|pieces|stuks?)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex QuantityWithUnit();

    [System.Text.RegularExpressions.GeneratedRegex(@"\b(\d+)\b")]
    private static partial System.Text.RegularExpressions.Regex AnyNumber();
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
    private readonly Lock _gate = new();

    public void Append(string correlationId, string eventType, string detail)
    {
        lock (_gate)
        {
            var previous = _events.LastOrDefault()?.Hash ?? "GENESIS";
            var occurred = DateTimeOffset.UtcNow;
            _events.Add(new AuditEvent(occurred, correlationId, eventType, detail, previous, ComputeHash(previous, occurred, correlationId, eventType, detail)));
        }
    }

    public IReadOnlyList<AuditEvent> ForCorrelation(string correlationId)
    {
        lock (_gate) return _events.Where(e => e.CorrelationId == correlationId).ToList();
    }

    public IReadOnlyList<AuditEvent> All()
    {
        lock (_gate) return _events.ToList();
    }

    /// <summary>Recomputes every hash and link. Any edited, removed or reordered event breaks the chain.</summary>
    public static bool Verify(IReadOnlyList<AuditEvent> chain)
    {
        var previous = "GENESIS";
        foreach (var e in chain)
        {
            if (e.PreviousHash != previous || e.Hash != ComputeHash(e.PreviousHash, e.OccurredAtUtc, e.CorrelationId, e.EventType, e.Detail)) return false;
            previous = e.Hash;
        }
        return true;
    }

    private static string ComputeHash(string previous, DateTimeOffset occurred, string correlationId, string eventType, string detail) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{previous}|{occurred:O}|{correlationId}|{eventType}|{detail}")));
}
