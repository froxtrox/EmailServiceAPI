resource "azurerm_resource_group" "rg" {
  name     = "rg-${var.project_name}"
  location = var.location
  tags     = var.tags
}

resource "azurerm_storage_account" "sa" {
  name                     = replace("st${var.project_name}", "-", "")
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"
  tags                     = var.tags
}

resource "azurerm_service_plan" "asp" {
  name                = "asp-${var.project_name}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "Y1"
  tags                = var.tags
}

resource "azurerm_log_analytics_workspace" "law" {
  name                = "law-${var.project_name}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = var.tags
}

resource "azurerm_application_insights" "ai" {
  name                = "ai-${var.project_name}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  workspace_id        = azurerm_log_analytics_workspace.law.id
  application_type    = "web"
  tags                = var.tags
}

resource "azurerm_communication_service" "acs" {
  name                = "acs-${var.project_name}"
  resource_group_name = azurerm_resource_group.rg.name
  data_location       = "UK"
  tags                = var.tags
}

resource "azurerm_email_communication_service" "ecs" {
  name                = "ecs-${var.project_name}"
  resource_group_name = azurerm_resource_group.rg.name
  data_location       = "UK"
  tags                = var.tags
}

resource "azurerm_email_communication_service_domain" "domain" {
  name              = "AzureManagedDomain"
  email_service_id  = azurerm_email_communication_service.ecs.id
  domain_management = "AzureManaged"
}

resource "azurerm_communication_service_email_domain_association" "assoc" {
  communication_service_id = azurerm_communication_service.acs.id
  email_service_domain_id  = azurerm_email_communication_service_domain.domain.id
}

locals {
  sender_email = "DoNotReply@${azurerm_email_communication_service_domain.domain.mail_from_sender_domain}"
}

resource "azurerm_linux_function_app" "func" {
  name                       = "func-${var.project_name}"
  resource_group_name        = azurerm_resource_group.rg.name
  location                   = azurerm_resource_group.rg.location
  storage_account_name       = azurerm_storage_account.sa.name
  storage_account_access_key = azurerm_storage_account.sa.primary_access_key
  service_plan_id            = azurerm_service_plan.asp.id
  https_only                 = true
  tags                       = var.tags

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      dotnet_version              = "10.0"
      use_dotnet_isolated_runtime = true
    }

    cors {
      allowed_origins = var.allowed_origins
    }
  }

  app_settings = {
    "ACS_ENDPOINT"                         = "https://${azurerm_communication_service.acs.name}.communication.azure.com"
    "SENDER_EMAIL"                         = local.sender_email
    "RECIPIENT_EMAIL"                      = var.recipient_email
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.ai.connection_string
    "FUNCTIONS_WORKER_RUNTIME"             = "dotnet-isolated"
  }
}

resource "azurerm_role_assignment" "func_acs_contributor" {
  scope                = azurerm_communication_service.acs.id
  role_definition_name = "Contributor"
  principal_id         = azurerm_linux_function_app.func.identity[0].principal_id
}
