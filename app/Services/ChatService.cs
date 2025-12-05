using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using System.Text.Json;
using OpenAI.Chat;

namespace ExpenseManagement.Services;

public class ChatService
{
    private readonly ILogger<ChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly DatabaseService _databaseService;
    private readonly ChatClient? _chatClient;
    private readonly bool _isConfigured;

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, DatabaseService databaseService)
    {
        _configuration = configuration;
        _logger = logger;
        _databaseService = databaseService;

        var endpoint = configuration["OpenAI:Endpoint"];
        var deploymentName = configuration["OpenAI:DeploymentName"];
        var managedIdentityClientId = configuration["ManagedIdentityClientId"];

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(deploymentName))
        {
            try
            {
                // Use ManagedIdentityCredential with explicit client ID or DefaultAzureCredential
                Azure.Core.TokenCredential credential;
                
                if (!string.IsNullOrEmpty(managedIdentityClientId))
                {
                    _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                    credential = new ManagedIdentityCredential(managedIdentityClientId);
                }
                else
                {
                    _logger.LogInformation("Using DefaultAzureCredential");
                    credential = new DefaultAzureCredential();
                }

                var azureOpenAIClient = new AzureOpenAIClient(new Uri(endpoint), credential);
                _chatClient = azureOpenAIClient.GetChatClient(deploymentName);
                _isConfigured = true;
                _logger.LogInformation("Chat service configured successfully with endpoint: {Endpoint}", endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to configure Azure OpenAI client");
                _isConfigured = false;
            }
        }
        else
        {
            _logger.LogInformation("OpenAI endpoint or deployment name not configured");
            _isConfigured = false;
        }
    }

    public async Task<string> GetChatResponseAsync(string userMessage)
    {
        if (!_isConfigured || _chatClient == null)
        {
            return "Azure OpenAI is not configured. Please deploy using deploy-with-chat.sh to enable AI chat functionality. " +
                   "The GenAI services need to be deployed for this feature to work.";
        }

        try
        {
            // Define function tools for expense operations
            var tools = new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    "get_all_expenses",
                    "Retrieves all expenses from the database"
                ),
                ChatTool.CreateFunctionTool(
                    "get_expenses_by_status",
                    "Retrieves expenses filtered by status",
                    BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "status": {
                                "type": "string",
                                "enum": ["Draft", "Submitted", "Approved", "Rejected"],
                                "description": "The status to filter expenses by"
                            }
                        },
                        "required": ["status"]
                    }
                    """)
                ),
                ChatTool.CreateFunctionTool(
                    "get_expenses_by_user",
                    "Retrieves expenses for a specific user",
                    BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "userId": {
                                "type": "integer",
                                "description": "The ID of the user"
                            }
                        },
                        "required": ["userId"]
                    }
                    """)
                ),
                ChatTool.CreateFunctionTool(
                    "get_all_users",
                    "Retrieves all users from the database"
                ),
                ChatTool.CreateFunctionTool(
                    "get_all_categories",
                    "Retrieves all expense categories"
                ),
                ChatTool.CreateFunctionTool(
                    "create_expense",
                    "Creates a new expense",
                    BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "userId": {
                                "type": "integer",
                                "description": "The ID of the user creating the expense"
                            },
                            "categoryId": {
                                "type": "integer",
                                "description": "The ID of the expense category"
                            },
                            "amountGBP": {
                                "type": "number",
                                "description": "The amount in GBP"
                            },
                            "expenseDate": {
                                "type": "string",
                                "description": "The date of the expense in YYYY-MM-DD format"
                            },
                            "description": {
                                "type": "string",
                                "description": "Description of the expense"
                            }
                        },
                        "required": ["userId", "categoryId", "amountGBP", "expenseDate"]
                    }
                    """)
                ),
                ChatTool.CreateFunctionTool(
                    "submit_expense",
                    "Submits an expense for approval",
                    BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "expenseId": {
                                "type": "integer",
                                "description": "The ID of the expense to submit"
                            }
                        },
                        "required": ["expenseId"]
                    }
                    """)
                ),
                ChatTool.CreateFunctionTool(
                    "approve_expense",
                    "Approves an expense (manager only)",
                    BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "expenseId": {
                                "type": "integer",
                                "description": "The ID of the expense to approve"
                            },
                            "reviewedBy": {
                                "type": "integer",
                                "description": "The ID of the manager approving the expense"
                            }
                        },
                        "required": ["expenseId", "reviewedBy"]
                    }
                    """)
                ),
                ChatTool.CreateFunctionTool(
                    "reject_expense",
                    "Rejects an expense (manager only)",
                    BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "expenseId": {
                                "type": "integer",
                                "description": "The ID of the expense to reject"
                            },
                            "reviewedBy": {
                                "type": "integer",
                                "description": "The ID of the manager rejecting the expense"
                            }
                        },
                        "required": ["expenseId", "reviewedBy"]
                    }
                    """)
                )
            };

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "You are an AI assistant for an Expense Management System. " +
                    "You have access to real functions to retrieve and manage expense data. " +
                    "When users ask about expenses, use the appropriate functions to get the actual data. " +
                    "When displaying lists, format them nicely with bullets or numbers. " +
                    "Available functions: get_all_expenses, get_expenses_by_status, get_expenses_by_user, " +
                    "get_all_users, get_all_categories, create_expense, submit_expense, approve_expense, reject_expense. " +
                    "Always be helpful and provide clear, formatted responses."
                ),
                new UserChatMessage(userMessage)
            };

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }

            // Orchestration loop for function calling
            bool requiresAction = true;
            int maxIterations = 5;
            int iteration = 0;

            while (requiresAction && iteration < maxIterations)
            {
                iteration++;
                
                var response = await _chatClient.CompleteChatAsync(messages, options);
                var choice = response.Value.Content[0];

                if (response.Value.FinishReason == ChatFinishReason.Stop)
                {
                    // Final response from the model
                    return choice.Text;
                }
                else if (response.Value.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // Model wants to call functions
                    messages.Add(new AssistantChatMessage(response.Value));

                    foreach (var toolCall in response.Value.ToolCalls)
                    {
                        if (toolCall is ChatToolCall functionToolCall)
                        {
                            var functionName = functionToolCall.FunctionName;
                            var functionArgs = functionToolCall.FunctionArguments;

                            _logger.LogInformation("Executing function: {FunctionName} with args: {Args}", 
                                functionName, functionArgs.ToString());

                            string functionResult = await ExecuteFunctionAsync(functionName, functionArgs.ToString());
                            
                            messages.Add(new ToolChatMessage(functionToolCall.Id, functionResult));
                        }
                    }
                }
                else
                {
                    requiresAction = false;
                }
            }

            return "I apologize, but I wasn't able to complete that request. Please try rephrasing your question.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetChatResponseAsync");
            return $"An error occurred while processing your request: {ex.Message}";
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string argumentsJson)
    {
        try
        {
            switch (functionName)
            {
                case "get_all_expenses":
                    var expenses = await _databaseService.GetAllExpensesAsync();
                    return JsonSerializer.Serialize(expenses);

                case "get_expenses_by_status":
                    var statusArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
                    var status = statusArgs!["status"].GetString();
                    var expensesByStatus = await _databaseService.GetExpensesByStatusAsync(status!);
                    return JsonSerializer.Serialize(expensesByStatus);

                case "get_expenses_by_user":
                    var userArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
                    var userId = userArgs!["userId"].GetInt32();
                    var expensesByUser = await _databaseService.GetExpensesByUserIdAsync(userId);
                    return JsonSerializer.Serialize(expensesByUser);

                case "get_all_users":
                    var users = await _databaseService.GetAllUsersAsync();
                    return JsonSerializer.Serialize(users);

                case "get_all_categories":
                    var categories = await _databaseService.GetAllCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

                case "create_expense":
                    var createArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
                    var request = new CreateExpenseRequest
                    {
                        UserId = createArgs!["userId"].GetInt32(),
                        CategoryId = createArgs["categoryId"].GetInt32(),
                        AmountGBP = createArgs["amountGBP"].GetDecimal(),
                        ExpenseDate = DateTime.Parse(createArgs["expenseDate"].GetString()!),
                        Description = createArgs.ContainsKey("description") ? createArgs["description"].GetString() : null
                    };
                    var newExpenseId = await _databaseService.CreateExpenseAsync(request);
                    return JsonSerializer.Serialize(new { expenseId = newExpenseId, message = "Expense created successfully" });

                case "submit_expense":
                    var submitArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
                    var expenseIdToSubmit = submitArgs!["expenseId"].GetInt32();
                    await _databaseService.SubmitExpenseAsync(expenseIdToSubmit);
                    return JsonSerializer.Serialize(new { message = "Expense submitted successfully" });

                case "approve_expense":
                    var approveArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
                    var expenseIdToApprove = approveArgs!["expenseId"].GetInt32();
                    var reviewerIdApprove = approveArgs["reviewedBy"].GetInt32();
                    await _databaseService.ApproveExpenseAsync(expenseIdToApprove, reviewerIdApprove);
                    return JsonSerializer.Serialize(new { message = "Expense approved successfully" });

                case "reject_expense":
                    var rejectArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
                    var expenseIdToReject = rejectArgs!["expenseId"].GetInt32();
                    var reviewerIdReject = rejectArgs["reviewedBy"].GetInt32();
                    await _databaseService.RejectExpenseAsync(expenseIdToReject, reviewerIdReject);
                    return JsonSerializer.Serialize(new { message = "Expense rejected successfully" });

                default:
                    return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
