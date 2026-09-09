# MonkeyBook infrastructure

Bicep templates that deploy MonkeyBook to **Azure App Service** (not Container Apps).

## What gets created

| Resource | Purpose |
| --- | --- |
| App Service Plan (Linux, B1) | Hosts the three .NET services |
| 3x Web App (.NET 10) | `postsapi`, `feedapi`, `processor`, each with a system assigned identity |
| Static Web App (Free) | The Vue frontend |
| Service Bus (Standard) | Topic `post-created` with subscription `background-processor` |
| PostgreSQL Flexible Server (B1ms) | Database `monkeybookdb`, public access with the "allow azure services" rule |
| Key Vault (RBAC) | Holds the database connection string, the apps read it through a key vault reference |
| Log Analytics + Application Insights | Telemetry, the Aspire service defaults export to it |

Role assignments: PostsApi gets *Service Bus Data Sender*, the background processor gets
*Service Bus Data Receiver*, and both the feed api and the processor get *Key Vault Secrets User*.

## Why the app changed

Azure App Service has no Dapr sidecar, so the Dapr pub/sub only works locally.
Both services now look at the `ServiceBus:Namespace` setting:

* **empty** (local `aspire run`) - Dapr and Redis, exactly like before.
* **set** (Azure) - `Azure.Messaging.ServiceBus` with the app service managed identity.

The Vite dev proxy also disappears in Azure, so the apis enable CORS for the static web app
origin (`Cors:AllowedOrigins:0`) and the frontend loads the api urls from `/config.json` at
startup. The deployment writes that file, so one build works for every environment.

## Deploy

```powershell
./infra/deploy.ps1 -ResourceGroupName rg-monkeybook-dev -PostgresAdminPassword (Read-Host -AsSecureString)
```

Use `-InfraOnly` to skip the application deployment.

## Deploy from GitHub Actions

`.github/workflows/deploy.yml` does the same thing. It needs these secrets:

* `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` - an app registration with a
  federated credential for this repository, with Contributor and User Access Administrator on
  the subscription or resource group (User Access Administrator is needed for the role assignments).
* `POSTGRES_ADMIN_PASSWORD` - the PostgreSQL administrator password.

## Notes

* The background processor applies the EF Core migrations and seeds the monkeys on startup.
* Always On is enabled so the processor keeps listening to the Service Bus subscription.
* Redis is not deployed, it only existed as the local Dapr broker.
