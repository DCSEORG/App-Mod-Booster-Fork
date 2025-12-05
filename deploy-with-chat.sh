#!/bin/bash
set -e

echo "=========================================="
echo "Expense Management System with Chat UI"
echo "Deployment (including GenAI)"
echo "=========================================="
echo ""

# Configuration
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
DEPLOY_GENAI=true

# Get Azure AD user information for SQL Server admin
echo "Getting Azure AD user information..."
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo "Admin Object ID: $ADMIN_OBJECT_ID"
echo "Admin Login: $ADMIN_LOGIN"
echo ""

# Create resource group
echo "Creating resource group: $RESOURCE_GROUP in $LOCATION..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none
echo "✓ Resource group created"
echo ""

# Deploy Bicep infrastructure with GenAI
echo "Deploying infrastructure with GenAI resources (this may take 10-15 minutes)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file bicep/main.bicep \
    --parameters location=$LOCATION \
    --parameters adminObjectId=$ADMIN_OBJECT_ID \
    --parameters adminLogin=$ADMIN_LOGIN \
    --parameters deployGenAI=$DEPLOY_GENAI \
    --output json)

echo "✓ Infrastructure deployed"
echo ""

# Extract deployment outputs
echo "Extracting deployment outputs..."
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.sqlServerFqdn.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.sqlServerName.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.databaseName.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityClientId.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceUrl.value')
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.searchEndpoint.value')

echo "App Service: $APP_SERVICE_NAME"
echo "SQL Server: $SQL_SERVER_FQDN"
echo "Database: $DATABASE_NAME"
echo "Managed Identity: $MANAGED_IDENTITY_NAME"
echo "OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "OpenAI Model: $OPENAI_MODEL_NAME"
echo "Search Endpoint: $SEARCH_ENDPOINT"
echo ""

# Configure App Service settings (including GenAI settings)
echo "Configuring App Service settings with GenAI..."
az webapp config appsettings set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
        "SqlConnection__Server=$SQL_SERVER_FQDN" \
        "SqlConnection__Database=$DATABASE_NAME" \
        "SqlConnection__ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
        "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
        "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
        "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
        "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
        "Search__Endpoint=$SEARCH_ENDPOINT" \
    --output none

echo "✓ App Service settings configured"
echo ""

# Wait for SQL Server to be fully ready
echo "Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30
echo ""

# Add current IP to SQL firewall
echo "Adding current IP to SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)

# Allow Azure services access
echo "Allowing Azure services access to SQL Server..."
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowAllAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none

# Add deployment IP
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowDeploymentIP" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none

echo "✓ Firewall rules configured"
echo ""

# Wait for firewall rules to propagate
echo "Waiting 15 seconds for firewall rules to propagate..."
sleep 15
echo ""

# Install required Python packages
echo "Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity
echo "✓ Python dependencies installed"
echo ""

# Update Python scripts with actual server names (Mac-compatible)
echo "Updating Python scripts with deployment values..."
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
echo "✓ Python scripts updated"
echo ""

# Import database schema
echo "Importing database schema..."
python3 run-sql.py
echo "✓ Database schema imported"
echo ""

# Update script.sql with managed identity name (Mac-compatible)
echo "Configuring managed identity database access..."
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Configure database roles for managed identity
echo "Configuring database roles..."
python3 run-sql-dbrole.py
echo "✓ Database roles configured"
echo ""

# Create stored procedures
echo "Creating stored procedures..."
python3 run-sql-stored-procs.py
echo "✓ Stored procedures created"
echo ""

# Build and publish the application
echo "Building and publishing application..."
cd app
dotnet publish -c Release -o ./publish
echo "✓ Application built"
echo ""

# Create deployment zip (files at root, not in subdirectory)
echo "Creating deployment package..."
cd publish
zip -r ../app.zip . > /dev/null
cd ..
echo "✓ Deployment package created"
echo ""

# Deploy application to App Service
echo "Deploying application to App Service..."
az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --src-path ./app.zip \
    --type zip \
    --output none

echo "✓ Application deployed"
echo ""

cd ..

# Summary
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "App Service URL: $APP_SERVICE_URL/Index"
echo "Chat UI URL: $APP_SERVICE_URL/chat"
echo "API Docs (Swagger): $APP_SERVICE_URL/swagger"
echo ""
echo "SQL Server: $SQL_SERVER_FQDN"
echo "Database: $DATABASE_NAME"
echo ""
echo "Azure OpenAI: $OPENAI_ENDPOINT"
echo "Model: $OPENAI_MODEL_NAME"
echo ""
echo "Note: Navigate to $APP_SERVICE_URL/Index to view the application"
echo "      Use $APP_SERVICE_URL/chat for the AI-powered chat interface"
echo ""
echo "To run the app locally:"
echo "1. Run 'az login' to authenticate"
echo "2. Update appsettings.json connection string to use:"
echo "   \"Authentication=Active Directory Default\""
echo ""
