variable "subscription_id" {
  type      = string
  sensitive = true
}

variable "project_name" {
  type    = string
  default = "email-service-api"
}

variable "location" {
  type    = string
  default = "uksouth"
}

variable "recipient_email" {
  type      = string
  sensitive = true
}

variable "allowed_origins" {
  type    = list(string)
  default = ["http://localhost:4200"]
}

variable "tags" {
  type = map(string)
  default = {
    managed_by = "terraform"
  }
}
