using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(DatabaseService databaseService, ILogger<UsersController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAllUsers()
    {
        try
        {
            var users = await _databaseService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return StatusCode(500, new { error = "Failed to retrieve users", details = ex.Message });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(int id)
    {
        try
        {
            var user = await _databaseService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { error = $"User {id} not found" });
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new { error = "Failed to retrieve user", details = ex.Message });
        }
    }
}
