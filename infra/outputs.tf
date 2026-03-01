output "function_app_url" {
  value = "https://${azurerm_linux_function_app.func.default_hostname}"
}

output "function_app_name" {
  value = azurerm_linux_function_app.func.name
}

output "sender_email" {
  value = local.sender_email
}

output "resource_group_name" {
  value = azurerm_resource_group.rg.name
}
