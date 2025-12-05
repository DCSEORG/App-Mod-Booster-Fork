using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class UsersModel : PageModel
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<UsersModel> _logger;

    public List<User> Users { get; set; } = new();

    public UsersModel(DatabaseService databaseService, ILogger<UsersModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Users = await _databaseService.GetAllUsersAsync();
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Database error in Users page");
            ViewData["ErrorMessage"] = $"Failed to connect to database. {ex.Message} " +
                $"(Error in: DatabaseService.GetAllUsersAsync). " +
                "Please ensure the managed identity has proper database permissions.";
            
            // Return dummy data
            Users = GetDummyUsers();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Users page");
            ViewData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
            
            // Return dummy data
            Users = GetDummyUsers();
        }
    }

    private List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User
            {
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                RoleName = "Employee",
                RoleId = 1,
                ManagerId = 2,
                ManagerName = "Bob Manager",
                IsActive = true,
                CreatedAt = DateTime.Now.AddMonths(-6)
            },
            new User
            {
                UserId = 2,
                UserName = "Bob Manager",
                Email = "bob.manager@example.co.uk",
                RoleName = "Manager",
                RoleId = 2,
                ManagerId = null,
                ManagerName = null,
                IsActive = true,
                CreatedAt = DateTime.Now.AddYears(-1)
            }
        };
    }
}
