# Architecture

## Logical components

```text
Email / Copilot Studio / API client
             │
             ▼
       Order proposal API (.NET)
             │
  structured extraction + validation
             │
             ▼
  IBusinessCentralClient ─────────► Business Central API (sandbox / production)
             │                              │
     inventory & rule evaluation            └─ customers, items, sales orders
             │
             ▼
   Pending proposal + approval gate
             │
             ▼
    Sales-order write + audit chain
```

## Business Central API mapping

| Domain intent | Business Central API resource | Notes |
| --- | --- | --- |
| Resolve customer | `customers` | Match controlled identifiers, not free text in production. |
| Resolve item | `items` | Preserve BC item number as system of record. |
| Check available inventory | `items` / custom inventory service | Real availability can include reservations and locations. |
| Create order | `salesOrders`, then `salesOrderLines` | Only after approval; store correlation ID for idempotency. |

## Security and controls

- OAuth 2.0 using Microsoft Entra ID; secrets stay in Key Vault or environment configuration.
- Least-privilege BC permission set; use a sandbox during development.
- An LLM may extract and explain, but cannot directly write ERP records.
- A named human approves each mutation.
- The audit chain links prior hashes so later changes are detectable.
- Production needs durable storage, retry policy, idempotency keys and monitoring.

## Design choices

The demo adapter is intentional. It makes the workflow reproducible, supports automated checks and avoids falsely implying access to a real tenant. The `IBusinessCentralClient` contract is the anti-corruption layer where the genuine BC v2.0 adapter belongs.
