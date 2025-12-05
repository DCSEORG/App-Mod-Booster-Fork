using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using System.Text.Json;

namespace ExpenseManagement.Services;

public class ChatService
{
    private readonly ILogger<ChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly DatabaseService _databaseService;
    private readonly OpenAIClient? _openAIClient;
    private readonly string? _deploymentName;
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

                _openAIClient = new OpenAIClient(new Uri(endpoint), credential);
                _deploymentName = deploymentName;
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
        if (!_isConfigured || _openAIClient == null || string.IsNullOrEmpty(_deploymentName))
        {
            return "Azure OpenAI is not configured. Please deploy using deploy-with-chat.sh to enable AI chat functionality. " +
                   "The GenAI services need to be deployed for this feature to work.";
        }

        try
        {
            // Create chat completion options with function definitions
            var chatCompletionsOptions = new ChatCompletionsOptions(_deploymentName, new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(
                    "You are an AI assistant for an Expense Management System. " +
                    "You have access to real functions to retrieve and manage expense data. " +
                    "When users ask about expenses, use the appropriate functions to get the actual data. " +
                    "When displaying lists, format them nicely with bullets or numbers. " +
                    "Always be helpful and provide clear, formatted responses."
                ),
                new ChatRequestUserMessage(userMessage)
            })
            {
                Temperature = 0.7f,
                MaxTokens = 1000
            };

            // Define function definitions for function calling
            var functions = new List<FunctionDefinition>
            {
                new FunctionDefinition
                {
                    Name = "get_all_expenses",
                    Description = "Retrieves all expenses from the database",
                    Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
                },
                new FunctionDefinition
                {
                    Name = "get_expenses_by_status",
                    Description = "Retrieves expenses filtered by status",
                    Parameters = BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            status = new
                            {
                                type = "string",
                                @enum = new[] { "Draft", "Submitted", "Approved", "Rejected" },
                                description = "The status to filter expenses by"
                            }
                        },
                        required = new[] { "status" }
                    })
                },
                new FunctionDefinition
                {
                    Name = "get_all_users",
                    Description = "Retrieves all users from the database",
                    Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
                },
                new FunctionDefinition
                {
                    Name = "get_all_categories",
                    Description = "Retrieves all expense categories",
                    Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
                }
            };

            foreach (var function in functions)
            {
                chatCompletionsOptions.Functions.Add(function);
            }

            // Get the response
            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var choice = response.Value.Choices[0];

            // Check if we need to call functions
            if (choice.FinishReason == CompletionsFinishReason.FunctionCall && choice.Message.FunctionCall != null)
            {
                var functionCall = choice.Message.FunctionCall;
                var functionResult = await ExecuteFunctionAsync(functionCall.Name, functionCall.Arguments);

                // Send function result back to get final response
                chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(choice.Message.Content) 
                { 
                    FunctionCall = choice.Message.FunctionCall 
                });
                chatCompletionsOptions.Messages.Add(new ChatRequestFunctionMessage(functionCall.Name, functionResult));

                var finalResponse = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                return finalResponse.Value.Choices[0].Message.Content;
            }

            return choice.Message.Content;
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
                    if (statusArgs == null || !statusArgs.ContainsKey("status"))
                    {
                        return JsonSerializer.Serialize(new { error = "Missing required parameter: status" });
                    }
                    var status = statusArgs["status"].GetString();
                    if (string.IsNullOrEmpty(status))
                    {
                        return JsonSerializer.Serialize(new { error = "Status parameter cannot be empty" });
                    }
                    var expensesByStatus = await _databaseService.GetExpensesByStatusAsync(status);
                    return JsonSerializer.Serialize(expensesByStatus);

                case "get_all_users":
                    var users = await _databaseService.GetAllUsersAsync();
                    return JsonSerializer.Serialize(users);

                case "get_all_categories":
                    var categories = await _databaseService.GetAllCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

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
