![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# App-Mod-Booster

A project to show how GitHub coding agent can turn screenshots of a legacy app into a working proof-of-concept for a cloud native Azure replacement if the legacy database schema is also provided.

## 🚀 Overview

This repository demonstrates modernizing a legacy expense management application into a cloud-native Azure application with:
- ASP.NET Core 8 (LTS) web application with Razor Pages
- Azure SQL Database with Azure AD-only authentication
- User-assigned managed identity for secure authentication
- REST APIs with Swagger documentation
- Azure OpenAI integration for AI-powered chat assistant
- Infrastructure as Code using Bicep
- Automated deployment scripts

## ✨ Features

### Core Application
- **Modern UI**: Clean, responsive interface with gradient design
- **Dashboard**: Overview of expenses, pending approvals, and quick actions
- **Expense Management**: Create, view, filter, and manage expenses
- **User Management**: View users and their roles
- **API Documentation**: Interactive Swagger UI for testing APIs

### AI-Powered Features (Optional)
- **Chat Assistant**: Natural language interface using Azure OpenAI GPT-4o
- **Function Calling**: AI can execute database operations via chat
- **Smart Responses**: Context-aware answers with proper formatting

### Security Features
- Azure AD-only authentication for SQL Database (MCAPS compliant)
- User-assigned managed identity for all service connections
- No connection strings or secrets in code
- HTTPS only for App Service
- Stored procedures for all database operations
- Role-based access control (RBAC)

## 📋 Prerequisites

- Azure subscription
- Azure CLI installed (`az`)
- .NET 8 SDK (for local development)
- Python 3.x with `pyodbc` and `azure-identity` packages
- Git

## 🎯 Quick Start

### Option 1: Fork and Modernize Your Own App

1. **Fork this repository**

2. **Replace the sample files**:
   - Put your legacy app screenshots in `Legacy-Screenshots/`
   - Put your database schema SQL in `Database-Schema/database_schema.sql`

3. **Use the GitHub Copilot Agent**:
   - Open the coding agent in GitHub
   - Select the "app-mod-booster" agent
   - Tell it: "modernise my app"
   - Wait 20-30 minutes for it to generate the code
   - Review and approve the pull request

4. **Deploy to Azure**:
   ```bash
   # Open in Codespaces or clone locally
   az login
   bash deploy.sh  # Basic deployment
   # OR
   bash deploy-with-chat.sh  # With AI chat features
   ```

### Option 2: Deploy This Sample

1. **Clone the repository**:
   ```bash
   git clone https://github.com/DCSEORG/App-Mod-Booster-Fork.git
   cd App-Mod-Booster-Fork
   ```

2. **Login to Azure**:
   ```bash
   az login
   az account set --subscription "Your-Subscription-Name"
   ```

3. **Deploy without AI features**:
   ```bash
   bash deploy.sh
   ```
   
   **OR Deploy with AI chat**:
   ```bash
   bash deploy-with-chat.sh
   ```

4. **Access your application**:
   - Main App: `https://app-expensemgmt-xxxxx.azurewebsites.net/Index`
   - API Docs: `https://app-expensemgmt-xxxxx.azurewebsites.net/swagger`
   - Chat UI: `https://app-expensemgmt-xxxxx.azurewebsites.net/chat`

## 📁 Repository Structure

```
.
├── bicep/                          # Infrastructure as Code
│   ├── main.bicep                  # Main orchestration template
│   └── modules/
│       ├── app-service.bicep       # App Service + Managed Identity
│       ├── azure-sql.bicep         # Azure SQL Database
│       └── genai.bicep             # Azure OpenAI + Search
├── app/                            # ASP.NET Core application
│   ├── Controllers/                # REST API controllers
│   ├── Models/                     # Data models
│   ├── Pages/                      # Razor Pages UI
│   ├── Services/                   # Business logic services
│   ├── wwwroot/                    # Static files (CSS, JS, chat.html)
│   └── app.zip                     # Pre-built deployment package
├── Database-Schema/                # Database schema files
│   └── database_schema.sql         # SQL schema and seed data
├── Legacy-Screenshots/             # Original app screenshots
│   ├── exp1.png
│   ├── exp2.png
│   └── exp3.png
├── prompts/                        # Agent instruction files
├── stored-procedures.sql           # Database stored procedures
├── script.sql                      # Managed identity permissions
├── run-sql.py                      # Schema import script
├── run-sql-dbrole.py              # Role assignment script
├── run-sql-stored-procs.py        # Stored procedure deployment
├── deploy.sh                       # Basic deployment script
├── deploy-with-chat.sh            # Full deployment with AI
└── ARCHITECTURE.md                 # Architecture documentation
```

## 🏗️ Architecture

### Basic Deployment (deploy.sh)
```
┌─────────────────────────────────────────┐
│  App Service (.NET 8)                   │
│  - Razor Pages UI                       │
│  - REST APIs                            │
│  - Swagger                              │
└────────┬────────────────────────────────┘
         │ Managed Identity
         ▼
┌─────────────────────────────────────────┐
│  Azure SQL Database                     │
│  - Azure AD auth only                   │
│  - Stored procedures                    │
└─────────────────────────────────────────┘
```

### Full Deployment (deploy-with-chat.sh)
```
┌─────────────────────────────────────────┐
│  App Service (.NET 8)                   │
│  - Razor Pages UI                       │
│  - REST APIs                            │
│  - Chat UI                              │
│  - Swagger                              │
└────┬────────────────┬───────────────────┘
     │                │ Managed Identity
     ▼                ▼
┌──────────────┐  ┌──────────────────────┐
│  Azure SQL   │  │  Azure OpenAI        │
│  Database    │  │  - GPT-4o model      │
└──────────────┘  │  - Function calling  │
                  └──────────────────────┘
                           │
                           ▼
                  ┌──────────────────────┐
                  │  Cognitive Search    │
                  │  - RAG pattern       │
                  └──────────────────────┘
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture documentation.

## 💰 Cost Estimate

### Basic Deployment (~£20-30/month)
- App Service (S1): ~£50/month
- Azure SQL (Basic): ~£4/month
- Total: ~£54/month (~$70/month)

### Full Deployment with AI (~£80-120/month)
- Basic deployment costs
- Azure OpenAI (S0): ~£30-50/month depending on usage
- Cognitive Search (Basic): ~£60/month
- Total: ~£144-164/month (~$180-210/month)

*Prices are estimates and may vary by region and usage*

## 🔧 Local Development

1. **Run the application locally**:
   ```bash
   cd app
   az login  # Authenticate with Azure
   dotnet run
   ```

2. **Update `appsettings.json`** for local development:
   ```json
   {
     "SqlConnection": {
       "Server": "your-server.database.windows.net",
       "Database": "expensedb",
       "ManagedIdentityClientId": ""
     }
   }
   ```
   
   Note: Leave `ManagedIdentityClientId` empty for local development. The app will use `DefaultAzureCredential` which prompts you to login with `az login`.

3. **Access the application**:
   - Main UI: http://localhost:5000/Index
   - API: http://localhost:5000/api/expenses
   - Swagger: http://localhost:5000/swagger

## 📚 API Documentation

The application includes interactive API documentation via Swagger. After deployment, access it at:
```
https://your-app-name.azurewebsites.net/swagger
```

### Available Endpoints

**Expenses**
- `GET /api/expenses` - Get all expenses
- `GET /api/expenses/{id}` - Get expense by ID
- `GET /api/expenses/user/{userId}` - Get expenses by user
- `GET /api/expenses/status/{status}` - Get expenses by status
- `POST /api/expenses` - Create new expense
- `PUT /api/expenses/{id}` - Update expense
- `POST /api/expenses/{id}/submit` - Submit expense
- `POST /api/expenses/{id}/approve` - Approve expense
- `POST /api/expenses/{id}/reject` - Reject expense
- `DELETE /api/expenses/{id}` - Delete expense

**Users**
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID

**Categories**
- `GET /api/categories` - Get all expense categories

**Chat** (if GenAI deployed)
- `POST /api/chat` - Send message to AI assistant

## 🤖 AI Chat Features

When deployed with `deploy-with-chat.sh`, the application includes an AI-powered chat assistant that can:

- Answer questions about expenses and users
- Filter and search expenses using natural language
- Retrieve specific expense details
- List expenses by status or user
- Explain expense categories and policies

**Example queries:**
- "Show me all submitted expenses"
- "List expenses for user Alice"
- "What are the expense categories?"
- "How many expenses are pending approval?"

## 🔒 Security

### Security Features Implemented
- ✅ Azure AD-only authentication (no SQL logins)
- ✅ User-assigned managed identity for all connections
- ✅ No secrets or connection strings in code
- ✅ HTTPS only enforced
- ✅ SQL firewall rules configured
- ✅ All database operations via stored procedures
- ✅ RBAC for Azure resources
- ✅ HTML escaping in chat UI to prevent XSS

### Security Scan Results
- **CodeQL**: No security vulnerabilities found
- **Code Review**: Minor recommendations addressed

## 🛠️ Troubleshooting

### Database Connection Errors
If you see database connection errors in the UI:
1. Check that the deployment script completed successfully
2. Verify the managed identity has database permissions: `run-sql-dbrole.py`
3. Ensure your IP is added to SQL firewall
4. Check App Service configuration has correct settings

### AI Chat Not Working
If chat returns "OpenAI is not configured":
1. Deploy using `deploy-with-chat.sh` instead of `deploy.sh`
2. Verify OpenAI resources were created in Azure portal
3. Check App Service configuration has OpenAI endpoint settings
4. Ensure managed identity has "Cognitive Services OpenAI User" role

### Build Errors
If the application fails to build:
```bash
cd app
dotnet restore
dotnet build
```

## 📖 Learn More

### Supporting Documentation
- [Azure App Service Best Practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/index-best-practices)
- [Azure SQL Managed Identity](https://learn.microsoft.com/en-us/azure/azure-sql/database/authentication-aad-configure)
- [Azure OpenAI Function Calling](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/function-calling)

### For Microsoft Employees
Supporting slides: [Here](https://microsofteur-my.sharepoint.com/:p:/g/personal/dchisholm_microsoft_com/IQAY41LQ12fjSIfFz3ha4hfFAZc7JQQuWaOrF7ObgxRK6f4?e=p6arJs)

## 🤝 Contributing

This is a demonstration project. Feel free to fork and adapt for your own use cases.

## 📝 License

See [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

Created to demonstrate AI-assisted application modernization using:
- GitHub Copilot
- Azure Services
- ASP.NET Core
- Bicep Infrastructure as Code
