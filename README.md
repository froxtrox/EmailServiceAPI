# EmailServiceAPI

An Azure Functions API for sending emails via Azure Communication Services.

## Tech Stack

- **.NET 8**
- **Azure Functions v4** (isolated worker model)
- **Azure Communication Services** (Email)
- **Terraform** (infrastructure)

## Project Structure

```
EmailServiceAPI/          # Azure Functions app
EmailServiceAPI.Tests/    # Unit tests
```

## Local Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Terraform](https://developer.hashicorp.com/terraform/install)

### Setup

1. Copy the example settings file and fill in your values:

   ```bash
   cp EmailServiceAPI/local.settings.example.json EmailServiceAPI/local.settings.json
   ```

2. Run the function app:

   ```bash
   cd EmailServiceAPI
   func start
   ```

### Running Tests

```bash
dotnet test
```

## Infrastructure

Infrastructure is managed with Terraform and hosted in my Terraform Samples repository:

**[froxtrox/TerraformSolutionSamples — Email Service](https://github.com/froxtrox/TerraformSolutionSamples/tree/main/Email%20Service)**

### Resources Provisioned

| Resource | Purpose |
|---|---|
| `azurerm_linux_function_app` | Hosts the .NET 8 isolated-worker function app |
| `azurerm_service_plan` (Y1) | Linux Consumption plan |
| `azurerm_storage_account` | Required by Azure Functions runtime |
| `azurerm_communication_service` | ACS resource for email dispatch |
| `azurerm_email_communication_service` | Email service with Azure Managed Domain |
| `azurerm_application_insights` | Telemetry and structured logging |
| `azurerm_log_analytics_workspace` | Backing store for Application Insights and ACS diagnostic logs |
| `azurerm_monitor_diagnostic_setting` | Routes ACS email send/delivery/engagement logs to Log Analytics |

### Authentication

The Function App uses a **system-assigned managed identity** with the `Contributor` role scoped to the Communication Service — no connection strings or API keys are stored in app settings.

### Deploying Infrastructure

1. Clone [TerraformSolutionSamples](https://github.com/froxtrox/TerraformSolutionSamples) and navigate to `Email Service/`
2. Copy the example vars file and fill in your values:
   ```bash
   cp terraform.tfvars.example terraform.tfvars
   ```
3. Initialize and apply:
   ```bash
   terraform init
   terraform plan
   terraform apply
   ```

### Deploying the Application

After infrastructure is provisioned, publish the function app from Visual Studio using the included publish profile, or via Azure CLI:

```bash
az webapp deploy --resource-group <rg> --name <func-name> --src-path <path-to.zip> --type zip
```

## License

See [LICENSE.txt](LICENSE.txt).
