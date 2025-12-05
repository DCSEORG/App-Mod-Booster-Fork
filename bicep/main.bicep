// Main Bicep template for deploying the Expense Management System
targetScope = 'resourceGroup'

@description('Location for all resources')
param location string = 'uksouth'

@description('Unique suffix for resource names')
param uniqueSuffix string = uniqueString(resourceGroup().id)

@description('Azure AD admin Object ID for SQL Server')
param adminObjectId string

@description('Azure AD admin login name for SQL Server')
param adminLogin string

@description('Deploy GenAI resources (Azure OpenAI and Cognitive Search)')
param deployGenAI bool = false

// Generate lowercase names for resources
var appServiceName = 'app-expensemgmt-${toLower(uniqueSuffix)}'
var sqlServerName = 'sql-expensemgmt-${toLower(uniqueSuffix)}'
var managedIdentityName = 'mid-expensemgmt-${toLower(uniqueSuffix)}'

// Deploy App Service with Managed Identity
module appService 'modules/app-service.bicep' = {
  name: 'app-service-deployment'
  params: {
    location: location
    appServiceName: appServiceName
    managedIdentityName: managedIdentityName
  }
}

// Deploy Azure SQL Database
module azureSQL 'modules/azure-sql.bicep' = {
  name: 'azure-sql-deployment'
  params: {
    location: location
    sqlServerName: sqlServerName
    databaseName: 'expensedb'
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
    managedIdentityName: managedIdentityName
  }
}

// Deploy GenAI resources (conditionally)
module genAI 'modules/genai.bicep' = if (deployGenAI) {
  name: 'genai-deployment'
  params: {
    location: location
    openAILocation: 'swedencentral'
    uniqueSuffix: toLower(uniqueSuffix)
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityName string = appService.outputs.managedIdentityName
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
output sqlServerFqdn string = azureSQL.outputs.sqlServerFqdn
output sqlServerName string = azureSQL.outputs.sqlServerName
output databaseName string = azureSQL.outputs.databaseName
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''
output openAIName string = deployGenAI ? genAI.outputs.openAIName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genAI.outputs.searchName : ''
