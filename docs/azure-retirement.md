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

The Basic registry generated recurring charges even while the demo was idle. Stopping only the container apps would leave registry charges running. The project resource group is therefore being removed after local verification; other Azure projects and subscriptions are outside this cleanup.

## Preserved alternative

- Source, Dockerfiles and Bicep infrastructure remain in the repository. Container images can be rebuilt from source.
- `bash scripts/run-local.sh` runs the API and dashboard using the .NET 10 SDK on localhost.
- Docker Compose offers local image builds and loopback-only published ports.
- Screenshots, a screenshot-tour video and an offline HTML showcase preserve the demonstration.
- CI remains active, while deployment runs only on manual dispatch with `enable_paid_azure` explicitly enabled. Pushing to `main` no longer recreates paid resources.

The demo uses local seeded adapters and in-memory state. No Azure OpenAI, Business Central sandbox, database or external API is required. Restarting the API resets records and inventory. Cloud in-memory records are not a durable database and are not migrated.

Already accrued charges and outstanding invoices remain payable. Removing this project does not guarantee zero charges from unrelated Azure resources.

## Cleanup verification

Pending execution after preservation and local verification. This record will be updated with the actual Azure result.
