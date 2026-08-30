# Business process: AI-assisted sales-order intake

## AS-IS

1. Customer sends a free-text email.
2. Operations manually identifies customer and item.
3. Staff checks inventory and creates the sales order.
4. Exceptions and rationale are scattered across inboxes and ERP notes.

Risks: transcription errors, slow response, inconsistent stock checks and weak traceability.

## TO-BE

1. Incoming request is parsed into customer, item, quantity and site.
2. The workflow retrieves master data and availability from Business Central.
3. Rules calculate an expected backorder and present a transparent recommendation.
4. An authorised user approves or rejects the proposal.
5. Only approved proposals create a BC sales order.
6. The request, proposal, approver and resulting order are recorded in an audit trail.

## Roles

| Role | Responsibility |
| --- | --- |
| Customer | Submits order request. |
| Operations agent | Reviews exceptions and approves/rejects. |
| BC service account | Performs constrained ERP reads/writes after approval. |
| AI extraction service | Produces structured, reviewable data; never writes ERP records. |
