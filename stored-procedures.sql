-- Stored Procedures for Expense Management System
-- All app code should use these stored procedures instead of direct SQL

-- =============================================
-- User Management Procedures
-- =============================================

CREATE OR ALTER PROCEDURE dbo.GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        r.RoleName,
        u.RoleId,
        u.ManagerId,
        m.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users m ON u.ManagerId = m.UserId
    ORDER BY u.UserName;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        r.RoleName,
        u.RoleId,
        u.ManagerId,
        m.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users m ON u.ManagerId = m.UserId
    WHERE u.UserId = @UserId;
END
GO

-- =============================================
-- Expense Management Procedures
-- =============================================

CREATE OR ALTER PROCEDURE dbo.GetAllExpenses
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor/100.0 AS DECIMAL(10,2)) AS AmountGBP,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        r.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
    ORDER BY e.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor/100.0 AS DECIMAL(10,2)) AS AmountGBP,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        r.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetExpensesByUserId
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor/100.0 AS DECIMAL(10,2)) AS AmountGBP,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        r.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
    WHERE e.UserId = @UserId
    ORDER BY e.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetExpensesByStatus
    @StatusName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor/100.0 AS DECIMAL(10,2)) AS AmountGBP,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        r.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
    WHERE s.StatusName = @StatusName
    ORDER BY e.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.CreateExpense
    @UserId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @Currency NVARCHAR(3),
    @ExpenseDate DATE,
    @Description NVARCHAR(1000),
    @ReceiptFile NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft';
    
    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, ReceiptFile)
    VALUES (@UserId, @CategoryId, @StatusId, @AmountMinor, @Currency, @ExpenseDate, @Description, @ReceiptFile);
    
    SELECT SCOPE_IDENTITY() AS ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.UpdateExpense
    @ExpenseId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @Currency NVARCHAR(3),
    @ExpenseDate DATE,
    @Description NVARCHAR(1000),
    @ReceiptFile NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Expenses
    SET 
        CategoryId = @CategoryId,
        AmountMinor = @AmountMinor,
        Currency = @Currency,
        ExpenseDate = @ExpenseDate,
        Description = @Description,
        ReceiptFile = @ReceiptFile
    WHERE ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.SubmitExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted';
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @StatusId,
        SubmittedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.ApproveExpense
    @ExpenseId INT,
    @ReviewedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Approved';
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @StatusId,
        ReviewedBy = @ReviewedBy,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.RejectExpense
    @ExpenseId INT,
    @ReviewedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Rejected';
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @StatusId,
        ReviewedBy = @ReviewedBy,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.DeleteExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM dbo.Expenses
    WHERE ExpenseId = @ExpenseId;
END
GO

-- =============================================
-- Category Management Procedures
-- =============================================

CREATE OR ALTER PROCEDURE dbo.GetAllCategories
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT CategoryId, CategoryName, IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

-- =============================================
-- Status Management Procedures
-- =============================================

CREATE OR ALTER PROCEDURE dbo.GetAllStatuses
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT StatusId, StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END
GO

-- =============================================
-- Roles Management Procedures
-- =============================================

CREATE OR ALTER PROCEDURE dbo.GetAllRoles
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT RoleId, RoleName, Description
    FROM dbo.Roles
    ORDER BY RoleName;
END
GO
