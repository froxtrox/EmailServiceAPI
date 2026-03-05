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
infra/                    # Terraform infrastructure
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

## Authentication

The Function App uses a **system-assigned managed identity** to authenticate with Azure Communication Services.

## Infrastructure

Infrastructure is managed with Terraform in the `infra/` directory.

1. Copy the example files:

   ```bash
   cp infra/terraform.tfvars.example infra/terraform.tfvars
   cp infra/backend.example.conf infra/backend.conf
   ```

2. Initialize and apply:

   ```bash
   cd infra
   terraform init -backend-config=backend.conf
   terraform plan
   terraform apply
   ```

## License

See [LICENSE.txt](LICENSE.txt).
