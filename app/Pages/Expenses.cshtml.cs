using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class ExpensesModel : PageModel
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<ExpensesModel> _logger;

    public List<Expense> Expenses { get; set; } = new();

    public ExpensesModel(DatabaseService databaseService, ILogger<ExpensesModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task OnGetAsync(string? status)
    {
        try
        {
            if (!string.IsNullOrEmpty(status))
            {
                Expenses = await _databaseService.GetExpensesByStatusAsync(status);
            }
            else
            {
                Expenses = await _databaseService.GetAllExpensesAsync();
            }
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Database error in Expenses page");
            ViewData["ErrorMessage"] = $"Failed to connect to database. {ex.Message} " +
                $"(Error in: DatabaseService.GetAllExpensesAsync). " +
                "Please ensure the managed identity has proper database permissions.";
            
            // Return dummy data
            Expenses = GetDummyExpenses();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Expenses page");
            ViewData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
            
            // Return dummy data
            Expenses = GetDummyExpenses();
        }
    }

    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserName = "Alice Example",
                CategoryName = "Travel",
                AmountGBP = 25.40m,
                ExpenseDate = DateTime.Now.AddDays(-5),
                StatusName = "Submitted",
                Description = "Taxi from airport to client site"
            },
            new Expense
            {
                ExpenseId = 2,
                UserName = "Alice Example",
                CategoryName = "Meals",
                AmountGBP = 14.25m,
                ExpenseDate = DateTime.Now.AddDays(-10),
                StatusName = "Approved",
                Description = "Client lunch meeting"
            },
            new Expense
            {
                ExpenseId = 3,
                UserName = "Alice Example",
                CategoryName = "Supplies",
                AmountGBP = 7.99m,
                ExpenseDate = DateTime.Now.AddDays(-2),
                StatusName = "Draft",
                Description = "Office stationery"
            },
            new Expense
            {
                ExpenseId = 4,
                UserName = "Alice Example",
                CategoryName = "Accommodation",
                AmountGBP = 123.00m,
                ExpenseDate = DateTime.Now.AddDays(-15),
                StatusName = "Approved",
                Description = "Hotel during client visit"
            }
        };
    }
}
