using AgenticOperations.Application;
using AgenticOperations.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IBusinessCentralClient, DemoBusinessCentralClient>();
builder.Services.AddSingleton<IOrderRequestExtractor, OrderRequestExtractor>();
builder.Services.AddSingleton<IProposalRepository, InMemoryProposalRepository>();
builder.Services.AddSingleton<IAuditTrail, HashChainedAuditTrail>();
builder.Services.AddSingleton<OrderProposalService>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { name = "Agentic Business Central Operations Platform", status = "ready", docs = "/api/demo" }));
app.MapGet("/api/demo", () => Results.Ok(new { request = "Please order another 40 units of product X for our Antwerp site.", note = "Create a proposal first; Business Central mutations require approval." }));

app.MapPost("/api/order-proposals", async (CreateProposalRequest request, OrderProposalService service, CancellationToken ct) =>
{
    try { return Results.Created("/api/order-proposals", await service.CreateAsync(new CreateProposalCommand(request.RequestText, request.CorrelationId), ct)); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
});

app.MapGet("/api/order-proposals/{id:guid}", (Guid id, IProposalRepository repository) => repository.Get(id) is { } proposal ? Results.Ok(proposal) : Results.NotFound());
app.MapGet("/api/order-proposals", (IProposalRepository repository) => Results.Ok(repository.GetAll().OrderByDescending(p => p.CreatedAtUtc)));
app.MapGet("/api/order-proposals/{id:guid}/audit", (Guid id, IProposalRepository repository, IAuditTrail audit) =>
    repository.Get(id) is { } proposal ? Results.Ok(audit.ForCorrelation(proposal.CorrelationId)) : Results.NotFound());
app.MapPost("/api/order-proposals/{id:guid}/approve", async (Guid id, ApprovalRequest request, OrderProposalService service, IAuditTrail audit, IProposalRepository repository, CancellationToken ct) =>
{
    try
    {
        var proposal = await service.ApproveAsync(id, request.ApprovedBy, ct);
        return Results.Ok(new { proposal, audit = audit.ForCorrelation(proposal.CorrelationId) });
    }
    catch (KeyNotFoundException) { return Results.NotFound(); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
});

app.Run();
public sealed record CreateProposalRequest(string RequestText, string? CorrelationId = null);
public sealed record ApprovalRequest(string ApprovedBy);
