// Azure SQL Database with Azure AD-Only Authentication
@description('Location for resources')
param location string

@description('SQL Server name')
param sqlServerName string

@description('Database name')
param databaseName string

@description('Azure AD admin Object ID')
param adminObjectId string

@description('Azure AD admin login name')
param adminLogin string

@description('Managed Identity Principal ID for database access')
param managedIdentityPrincipalId string

@description('Managed Identity name')
param managedIdentityName string

// Create SQL Server with Azure AD authentication
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: 'sqladmin' // Required but not used due to Azure AD-only auth
    administratorLoginPassword: guid(resourceGroup().id, sqlServerName) // Required but not used
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Configure Azure AD Administrator
resource sqlAdministrator 'Microsoft.Sql/servers/administrators@2021-11-01' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: adminLogin
    sid: adminObjectId
    tenantId: subscription().tenantId
    azureADOnlyAuthentication: true
  }
}

// Create Database
resource database 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
  }
}

// Allow Azure services to access server
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Outputs
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlServerName string = sqlServer.name
output databaseName string = database.name
