# Azure Services Architecture Diagram

## Expense Management System - Modernized Application

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Azure Cloud Platform                         │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                    Resource Group (UK South)                    │ │
│  │                                                                  │ │
│  │  ┌────────────────────┐                                         │ │
│  │  │  User-Assigned     │                                         │ │
│  │  │  Managed Identity  │─────┐                                   │ │
│  │  │  (Authentication)  │     │                                   │ │
│  │  └────────────────────┘     │                                   │ │
│  │            │                 │                                   │ │
│  │            │ Assigned to     │ Role: Cognitive Services User    │ │
│  │            │                 │ Role: Search Contributor         │ │
│  │            ▼                 │                                   │ │
│  │  ┌────────────────────┐     │                                   │ │
│  │  │   App Service      │     │                                   │ │
│  │  │   (Linux, .NET 8)  │     │                                   │ │
│  │  │                    │     │                                   │ │
│  │  │  • Razor Pages UI  │     │                                   │ │
│  │  │  • REST APIs       │     │                                   │ │
│  │  │  • Chat UI         │     │                                   │ │
│  │  │  • Swagger Docs    │     │                                   │ │
│  │  └────────┬───────────┘     │                                   │ │
│  │           │                  │                                   │ │
│  │           │ Connects via     │                                   │ │
│  │           │ Managed Identity │                                   │ │
│  │           ▼                  ▼                                   │ │
│  │  ┌────────────────────┐  ┌──────────────────┐                  │ │
│  │  │   Azure SQL DB     │  │  Azure OpenAI    │                  │ │
│  │  │                    │  │  (Sweden Central)│                  │ │
│  │  │  • Entra ID Auth   │  │                  │                  │ │
│  │  │  • No SQL Auth     │  │  • GPT-4o Model  │                  │ │
│  │  │  • Basic Tier      │  │  • S0 SKU        │                  │ │
│  │  │  • Firewall Rules  │  │  • Capacity: 8   │                  │ │
│  │  │                    │  └──────────────────┘                  │ │
│  │  │  Database:         │           │                             │ │
│  │  │  • Users           │           │ Used by Chat Service       │ │
│  │  │  • Expenses        │           │                             │ │
│  │  │  • Categories      │           ▼                             │ │
│  │  │  • Status          │  ┌──────────────────┐                  │ │
│  │  │  • Roles           │  │  AI Search       │                  │ │
│  │  │  • Stored Procs    │  │  (Basic Tier)    │                  │ │
│  │  └────────────────────┘  │                  │                  │ │
│  │                           │  • RAG Pattern   │                  │ │
│  └───────────────────────────│  • Indexing      │──────────────────┘ │
│                              └──────────────────┘                    │
│                                                                       │
│  Data Flow:                                                          │
│  1. User accesses App Service via HTTPS                             │
│  2. App Service uses Managed Identity for authentication             │
│  3. Stored Procedures used for all database operations              │
│  4. Chat UI calls Azure OpenAI with function calling                │
│  5. AI Search provides RAG capabilities for contextual answers      │
│                                                                       │
└───────────────────────────────────────────────────────────────────────┘

Key Security Features:
• Azure AD-only authentication for SQL (no SQL logins)
• Managed Identity for all Azure service connections
• HTTPS only for App Service
• Firewall rules for SQL Server
• Role-based access control (RBAC)
• No secrets in application code
```

## Deployment Options

### Option 1: Basic Deployment (deploy.sh)
- Resource Group
- App Service + App Service Plan
- User-Assigned Managed Identity
- Azure SQL Database
- Firewall Rules

**Cost**: ~£20-30/month (Basic SQL, S1 App Service)

### Option 2: Full Deployment with AI (deploy-with-chat.sh)
- All from Option 1, plus:
- Azure OpenAI (GPT-4o)
- Cognitive Search
- Enhanced chat capabilities with function calling

**Cost**: ~£50-80/month (includes AI services)

## Key Components

1. **App Service (Linux, .NET 8)**
   - Hosts the modernized ASP.NET Core application
   - Uses S1 SKU to avoid cold starts
   - Configured with managed identity

2. **Azure SQL Database**
   - Basic tier for development/POC
   - Azure AD-only authentication (MCAPS policy compliant)
   - Managed identity for app access
   - All operations via stored procedures

3. **User-Assigned Managed Identity**
   - Single identity for all services
   - Assigned to App Service
   - Granted permissions to SQL, OpenAI, and Search
   - Eliminates need for connection strings/keys

4. **Azure OpenAI (Optional)**
   - GPT-4o model in Sweden Central
   - Used for AI chat assistant
   - Function calling for database operations
   - S0 SKU with capacity 8

5. **Cognitive Search (Optional)**
   - Basic tier for RAG pattern
   - Indexes contextual information
   - Enhances chat responses

## Endpoints

- **Main App**: https://{app-name}.azurewebsites.net/Index
- **API Docs**: https://{app-name}.azurewebsites.net/swagger
- **Chat UI**: https://{app-name}.azurewebsites.net/chat
- **REST API**: https://{app-name}.azurewebsites.net/api/*
