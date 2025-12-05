using Azure.Core;
using Azure.Identity;
using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;

namespace ExpenseManagement.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var server = configuration["SqlConnection:Server"];
        var database = configuration["SqlConnection:Database"];
        var managedIdentityClientId = configuration["SqlConnection:ManagedIdentityClientId"];

        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            // Use User-Assigned Managed Identity for Azure
            _connectionString = $"Server=tcp:{server},1433;Database={database};Authentication=Active Directory Managed Identity;User Id={managedIdentityClientId};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        }
        else
        {
            // Use DefaultAzureCredential for local development
            _connectionString = $"Server=tcp:{server},1433;Database={database};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        }
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    // User methods
    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            var users = new List<User>();
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.GetAllUsers", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.GetString(2),
                    RoleName = reader.GetString(3),
                    RoleId = reader.GetInt32(4),
                    ManagerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    ManagerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    IsActive = reader.GetBoolean(7),
                    CreatedAt = reader.GetDateTime(8)
                });
            }
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllUsersAsync");
            throw new DatabaseException("Failed to retrieve users", ex);
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.GetUserById", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.GetString(2),
                    RoleName = reader.GetString(3),
                    RoleId = reader.GetInt32(4),
                    ManagerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    ManagerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    IsActive = reader.GetBoolean(7),
                    CreatedAt = reader.GetDateTime(8)
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUserByIdAsync for userId: {UserId}", userId);
            throw new DatabaseException($"Failed to retrieve user {userId}", ex);
        }
    }

    // Expense methods
    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        try
        {
            var expenses = new List<Expense>();
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.GetAllExpenses", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(ReadExpenseFromReader(reader));
            }
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllExpensesAsync");
            throw new DatabaseException("Failed to retrieve expenses", ex);
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.GetExpenseById", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadExpenseFromReader(reader);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetExpenseByIdAsync for expenseId: {ExpenseId}", expenseId);
            throw new DatabaseException($"Failed to retrieve expense {expenseId}", ex);
        }
    }

    public async Task<List<Expense>> GetExpensesByUserIdAsync(int userId)
    {
        try
        {
            var expenses = new List<Expense>();
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.GetExpensesByUserId", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(ReadExpenseFromReader(reader));
            }
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetExpensesByUserIdAsync for userId: {UserId}", userId);
            throw new DatabaseException($"Failed to retrieve expenses for user {userId}", ex);
        }
    }

    public async Task<List<Expense>> GetExpensesByStatusAsync(string statusName)
    {
        try
        {
            var expenses = new List<Expense>();
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.GetExpensesByStatus", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@StatusName", statusName);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(ReadExpenseFromReader(reader));
            }
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetExpensesByStatusAsync for status: {StatusName}", statusName);
            throw new DatabaseException($"Failed to retrieve expenses with status {statusName}", ex);
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.CreateExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@UserId", request.UserId);
            cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            cmd.Parameters.AddWithValue("@AmountMinor", (int)(request.AmountGBP * 100));
            cmd.Parameters.AddWithValue("@Currency", request.Currency);
            cmd.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateExpenseAsync");
            throw new DatabaseException("Failed to create expense", ex);
        }
    }

    public async Task UpdateExpenseAsync(int expenseId, CreateExpenseRequest request)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.UpdateExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
            cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            cmd.Parameters.AddWithValue("@AmountMinor", (int)(request.AmountGBP * 100));
            cmd.Parameters.AddWithValue("@Currency", request.Currency);
            cmd.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateExpenseAsync for expenseId: {ExpenseId}", expenseId);
            throw new DatabaseException($"Failed to update expense {expenseId}", ex);
        }
    }

    public async Task SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.SubmitExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SubmitExpenseAsync for expenseId: {ExpenseId}", expenseId);
            throw new DatabaseException($"Failed to submit expense {expenseId}", ex);
        }
    }

    public async Task ApproveExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.ApproveExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
            cmd.Parameters.AddWithValue("@ReviewedBy", reviewedBy);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApproveExpenseAsync for expenseId: {ExpenseId}", expenseId);
            throw new DatabaseException($"Failed to approve expense {expenseId}", ex);
        }
    }

    public async Task RejectExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.RejectExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
            cmd.Parameters.AddWithValue("@ReviewedBy", reviewedBy);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RejectExpenseAsync for expenseId: {ExpenseId}", expenseId);
            throw new DatabaseException($"Failed to reject expense {expenseId}", ex);
        }
    }

    public async Task DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.DeleteExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteExpenseAsync for expenseId: {ExpenseId}", expenseId);
            throw new DatabaseException($"Failed to delete expense {expenseId}", ex);
        }
    }

    // Category methods
    public async Task<List<ExpenseCategory>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = new List<ExpenseCategory>();
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.GetAllCategories", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllCategoriesAsync");
            throw new DatabaseException("Failed to retrieve categories", ex);
        }
    }

    // Status methods
    public async Task<List<ExpenseStatus>> GetAllStatusesAsync()
    {
        try
        {
            var statuses = new List<ExpenseStatus>();
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.GetAllStatuses", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1)
                });
            }
            return statuses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllStatusesAsync");
            throw new DatabaseException("Failed to retrieve statuses", ex);
        }
    }

    // Roles methods
    public async Task<List<Role>> GetAllRolesAsync()
    {
        try
        {
            var roles = new List<Role>();
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.GetAllRoles", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                roles.Add(new Role
                {
                    RoleId = reader.GetInt32(0),
                    RoleName = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllRolesAsync");
            throw new DatabaseException("Failed to retrieve roles", ex);
        }
    }

    private Expense ReadExpenseFromReader(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            UserName = reader.GetString(2),
            CategoryId = reader.GetInt32(3),
            CategoryName = reader.GetString(4),
            StatusId = reader.GetInt32(5),
            StatusName = reader.GetString(6),
            AmountMinor = reader.GetInt32(7),
            AmountGBP = reader.GetDecimal(8),
            Currency = reader.GetString(9),
            ExpenseDate = reader.GetDateTime(10),
            Description = reader.IsDBNull(11) ? null : reader.GetString(11),
            ReceiptFile = reader.IsDBNull(12) ? null : reader.GetString(12),
            SubmittedAt = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
            ReviewedBy = reader.IsDBNull(14) ? null : reader.GetInt32(14),
            ReviewedByName = reader.IsDBNull(15) ? null : reader.GetString(15),
            ReviewedAt = reader.IsDBNull(16) ? null : reader.GetDateTime(16),
            CreatedAt = reader.GetDateTime(17)
        };
    }
}

public class DatabaseException : Exception
{
    public DatabaseException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
