using AgenticOperations.Domain;

namespace AgenticOperations.Application;

public sealed class OrderProposalService(IBusinessCentralClient businessCentral, IOrderRequestExtractor extractor, IProposalRepository proposals, IAuditTrail audit)
{
    public async Task<OrderProposal> CreateAsync(CreateProposalCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RequestText)) throw new ArgumentException("A customer request is required.");
        var correlationId = command.CorrelationId ?? Guid.NewGuid().ToString("N");
        var request = extractor.Extract(command.RequestText);
        audit.Append(correlationId, "RequestExtracted", $"Quantity={request.Quantity}; customerHint={request.CustomerHint}; itemHint={request.ItemHint}");

        var customer = await businessCentral.FindCustomerAsync(request.CustomerHint, request.RequestedSite, cancellationToken)
            ?? throw new InvalidOperationException("No Business Central customer matched the request.");
        var item = await businessCentral.FindItemAsync(request.ItemHint, cancellationToken)
            ?? throw new InvalidOperationException("No Business Central item matched the request.");
        var inventory = await businessCentral.GetInventoryAsync(item.Number, cancellationToken);
        var backorder = Math.Max(0, request.Quantity - inventory.AvailableQuantity);
        var explanation = backorder == 0
            ? $"{request.Quantity} unit(s) are available. Ready for approval."
            : $"Only {inventory.AvailableQuantity} unit(s) are available. Approval will create an order for {request.Quantity}, including a {backorder}-unit backorder.";
        var proposal = new OrderProposal { CorrelationId = correlationId, Request = request, Customer = customer, Item = item, Inventory = inventory, Explanation = explanation };
        proposals.Save(proposal);
        audit.Append(correlationId, "ProposalCreated", $"Proposal={proposal.Id}; status={proposal.Status}; backorder={backorder}");
        return proposal;
    }

    public async Task<OrderProposal> ApproveAsync(Guid id, string approvedBy, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(approvedBy)) throw new ArgumentException("An approver is required.");
        var proposal = proposals.Get(id) ?? throw new KeyNotFoundException("Order proposal was not found.");
        if (proposal.Status != ProposalStatus.PendingApproval) throw new InvalidOperationException("Only pending proposals can be approved.");
        var backorder = Math.Max(0, proposal.Request.Quantity - proposal.Inventory.AvailableQuantity);
        proposal.SalesOrder = await businessCentral.CreateSalesOrderAsync(proposal.Customer.Number, proposal.Item.Number, proposal.Request.Quantity, backorder, proposal.CorrelationId, cancellationToken);
        proposal.Status = ProposalStatus.Approved;
        proposal.ApprovedBy = approvedBy;
        audit.Append(proposal.CorrelationId, "SalesOrderCreated", $"SalesOrder={proposal.SalesOrder.Number}; approvedBy={approvedBy}");
        return proposal;
    }
}
