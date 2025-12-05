using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<IndexModel> _logger;

    public List<Expense> RecentExpenses { get; set; } = new();
    public int TotalExpenses { get; set; }
    public int PendingCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalUsers { get; set; }

    public IndexModel(DatabaseService databaseService, ILogger<IndexModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            // Get all expenses for statistics
            var allExpenses = await _databaseService.GetAllExpensesAsync();
            TotalExpenses = allExpenses.Count;
            PendingCount = allExpenses.Count(e => e.StatusName == "Submitted");
            TotalAmount = allExpenses.Sum(e => e.AmountGBP);

            // Get recent expenses (last 10)
            RecentExpenses = allExpenses.Take(10).ToList();

            // Get user count
            var users = await _databaseService.GetAllUsersAsync();
            TotalUsers = users.Count;
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Database error in Index page");
            ViewData["ErrorMessage"] = $"Failed to connect to database. {ex.Message} " +
                $"(Error in: DatabaseService.GetAllExpensesAsync). " +
                "Please ensure the managed identity has proper database permissions. " +
                "Run the deployment script to configure the database roles.";
            
            // Return dummy data
            TotalExpenses = 4;
            PendingCount = 1;
            TotalAmount = 165.64m;
            TotalUsers = 2;
            RecentExpenses = GetDummyExpenses();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Index page");
            ViewData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
            
            // Return dummy data
            TotalExpenses = 4;
            PendingCount = 1;
            TotalAmount = 165.64m;
            TotalUsers = 2;
            RecentExpenses = GetDummyExpenses();
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
