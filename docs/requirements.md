# Requirements and UAT

## Functional requirements

- FR-01: The system extracts quantity, item and customer/site signals from a request.
- FR-02: The system looks up customer, item and inventory via an ERP boundary.
- FR-03: Insufficient inventory creates a visible backorder warning.
- FR-04: No sales order is created before a human approval.
- FR-05: Approval records the named approver and creates exactly one sales order.
- FR-06: Every material action is audit logged using a correlation ID.

## UAT scenarios

| ID | Given | When | Then |
| --- | --- | --- | --- |
| UAT-01 | Product X has 32 units available | Request 40 units | Proposal shows an 8-unit backorder. |
| UAT-02 | A proposal is pending | User views proposal | No ERP sales order exists. |
| UAT-03 | A proposal is pending | Authorised user approves | Order is created and audit entry contains approver. |
| UAT-04 | A proposal is already approved | User approves again | API rejects duplicate mutation. |
| UAT-05 | Request has no quantity | User submits it | API rejects the request with a validation error. |
