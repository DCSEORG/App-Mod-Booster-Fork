-- Drop and recreate the managed identity user with correct SID
-- This will be replaced with the actual managed identity name by the deployment script
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'MANAGED-IDENTITY-NAME')
BEGIN
    DROP USER [MANAGED-IDENTITY-NAME];
END
GO

CREATE USER [MANAGED-IDENTITY-NAME] FROM EXTERNAL PROVIDER;
GO

ALTER ROLE db_datareader ADD MEMBER [MANAGED-IDENTITY-NAME];
GO

ALTER ROLE db_datawriter ADD MEMBER [MANAGED-IDENTITY-NAME];
GO

GRANT EXECUTE TO [MANAGED-IDENTITY-NAME];
GO
