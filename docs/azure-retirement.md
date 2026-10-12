# Azure demo retirement

The owner requested that the demo be preserved in the repository, verified locally and its recurring project hosting costs stopped on October 12, 2026.

## Former resources

Resource group: `rg-agentic-operations-dev` (Azure subscription ending `5515`).

| Resource | Purpose |
| --- | --- |
| `agenticopsphilippe2026` | Basic Container Registry, created September 3, 2026 |
| `agenticops-api` | Container App for the demo API |
| `agenticops-dashboard` | Container App for the dashboard |
| `agenticops-aca` | Container Apps environment |
| `agenticops-logs` | Log Analytics workspace |
| `agenticops-identity` | User-assigned identity for image pulls |

The Basic registry generated recurring charges even while the demo was idle. Stopping only the container apps would leave registry charges running. Removal of the project resource group was requested after local verification; other Azure projects and subscriptions are outside this cleanup.

## Preserved alternative

- Source, Dockerfiles and Bicep infrastructure remain in the repository. Container images can be rebuilt from source.
- `bash scripts/run-local.sh` runs the API and dashboard using the .NET 10 SDK on localhost.
- Docker Compose offers local image builds and loopback-only published ports.
- Screenshots, a screenshot-tour video and an offline HTML showcase preserve the demonstration.
- CI remains active, while deployment runs only on manual dispatch with `enable_paid_azure` explicitly enabled. Pushing to `main` no longer recreates paid resources.

The demo uses local seeded adapters and in-memory state. No Azure OpenAI, Business Central sandbox, database or external API is required. Restarting the API resets records and inventory. Cloud in-memory records are not a durable database and are not migrated.

Already accrued charges and outstanding invoices remain payable. Removing this project does not guarantee zero charges from unrelated Azure resources.

## Cleanup verification

On October 12, 2026, after commit `b425921` was pushed and GitHub CI passed, `az group delete --subscription …5515 --name rg-agentic-operations-dev --yes --no-wait` was accepted.

Subsequent resource inventory confirmed removal of the registry, API, dashboard, workspace and identity. This removes the identified recurring registry cost source and the two running applications.

Azure's final environment cleanup was still asynchronous at the last check: only `agenticops-aca` remained, with `provisioningState: ScheduledForDelete`, no deployment errors and a Consumption workload profile. The resource group was `Deleting`. This is not a claim that full resource-group deletion was already complete.

Local verification passed: Release build (zero warnings/errors), existing end-to-end checks, browser create/approve workflow (`SO-1001`, eight-unit backorder), three audit events and browser console with zero warnings/errors. Offline showcase verification loaded all three screenshots and its 12-second video. Docker and mobile layouts were not tested.
