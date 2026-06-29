/* =============================================================================
   StandNatura — FULL DATABASE SETUP SCRIPT
   Recreates the entire database from scratch on a fresh SQL Server instance.

   HOW TO RUN:
     1. Open SQL Server Management Studio (SSMS), connect to your local instance.
     2. File > New > Query, paste this whole script, press F5.

   *** IMPORTANT — CONNECTION STRING (read this!) *************************
   This only creates the DATABASE. The C# app still has its server name
   HARDCODED in:  StandNatura/Models/DatabaseConfig.cs
   It currently points at the original author's machine:
       Server=DESKTOP-4LDUQSS; Database=StandNatura; Integrated Security=True; ...
   On YOUR machine that server name will NOT match. After running this script,
   edit DatabaseConfig.cs and change the  Server=...  value to YOUR own SQL
   Server instance, e.g.:
       Server=.\SQLEXPRESS            (SQL Server Express)
       Server=(localdb)\MSSQLLocalDB  (LocalDB)
       Server=YOUR-PC-NAME            (default instance / your machine name)
   Otherwise the app will fail to connect even though the database exists.
   **********************************************************************

   TEST LOGINS created at the bottom (all password = 1234):
       admin / 1234         (Role: Admin)
       superadmin / 1234    (Role: SuperAdmin)
       contributor1 / 1234  (Role: Contributor)

   NOTE: The "drop" section makes this re-runnable, but it is DESTRUCTIVE —
   it deletes all StandNatura tables/data if the database already exists.
   On a fresh machine there is nothing to drop, so it is safe.
   ============================================================================= */


/* =============================================================================
   SECTION 1 — CREATE DATABASE
   ============================================================================= */
IF DB_ID('StandNatura') IS NULL
    CREATE DATABASE StandNatura;
GO

USE StandNatura;
GO


/* =============================================================================
   SECTION 2 — CLEAN SLATE (for re-runs; harmless on a fresh database)
   Drop programmable objects, then tables in reverse dependency order.
   (Dropping a table also drops its triggers automatically.)
   ============================================================================= */
DROP FUNCTION  IF EXISTS dbo.fn_TotalFundsForSighting;
DROP FUNCTION  IF EXISTS dbo.fn_CanUserComment;
DROP FUNCTION  IF EXISTS dbo.fn_GetUserSubmissionCountToday;
DROP PROCEDURE IF EXISTS dbo.usp_TogglePetitionSignature;
DROP PROCEDURE IF EXISTS dbo.usp_PostDonationComment;
DROP PROCEDURE IF EXISTS dbo.usp_DenySighting;

DROP TABLE IF EXISTS Comment;
DROP TABLE IF EXISTS PetitionSignature;
DROP TABLE IF EXISTS Petition;
DROP TABLE IF EXISTS Fund;
DROP TABLE IF EXISTS Donation;
DROP TABLE IF EXISTS Sighting;
DROP TABLE IF EXISTS Users;
GO


/* =============================================================================
   SECTION 3 — TABLES (created in dependency-safe order)
   ============================================================================= */

-- ---------- Users (no dependencies) ----------
CREATE TABLE Users (
    Id           INT IDENTITY(1,1) NOT NULL,
    Username     NVARCHAR(256) NOT NULL,
    Role         NVARCHAR(20)  NOT NULL CONSTRAINT DF_Users_Role DEFAULT ('Contributor'),
    PasswordHash NVARCHAR(200) NOT NULL,
    Salt         NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Username UNIQUE (Username)
);

-- ---------- Sighting (FK -> Users) ----------
CREATE TABLE Sighting (
    SightingId    INT IDENTITY(1,1) NOT NULL,
    UserId        INT NOT NULL,
    Title         NVARCHAR(100) NOT NULL,
    Description   NVARCHAR(500) NOT NULL,
    DatePosted    DATETIME NOT NULL CONSTRAINT DF_Sighting_DatePosted DEFAULT (GETDATE()),
    Photo         NVARCHAR(500) NULL,
    Location      NVARCHAR(200) NOT NULL,
    Province      NVARCHAR(100) NOT NULL,
    Region        NVARCHAR(100) NOT NULL,
    Longitude     DECIMAL(9,6) NOT NULL,
    Latitude      DECIMAL(9,6) NOT NULL,
    Status        NVARCHAR(20) NOT NULL CONSTRAINT DF_Sighting_Status DEFAULT ('Pending'),
    DenialReason  NVARCHAR(500) NULL,
    ArchiveReason NVARCHAR(500) NULL,
    CONSTRAINT PK_Sighting PRIMARY KEY (SightingId),
    CONSTRAINT FK_Sighting_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- ---------- Donation (FK -> Users, Sighting) ----------
CREATE TABLE Donation (
    DonationId INT IDENTITY(1,1) NOT NULL,
    UserId     INT NOT NULL,
    SightingId INT NOT NULL,
    Amount     DECIMAL(10,2) NOT NULL,
    CONSTRAINT PK_Donation PRIMARY KEY (DonationId),
    CONSTRAINT FK_Donation_Users    FOREIGN KEY (UserId)     REFERENCES Users(Id),
    CONSTRAINT FK_Donation_Sighting FOREIGN KEY (SightingId) REFERENCES Sighting(SightingId)
);

-- ---------- Fund (FK -> Sighting) ----------
CREATE TABLE Fund (
    FundId      INT IDENTITY(1,1) NOT NULL,
    SightingId  INT NOT NULL,
    TotalAmount DECIMAL(10,2) NOT NULL CONSTRAINT DF_Fund_TotalAmount DEFAULT ((0)),
    LastUpdated DATETIME NOT NULL CONSTRAINT DF_Fund_LastUpdated DEFAULT (GETDATE()),
    CONSTRAINT PK_Fund PRIMARY KEY (FundId),
    CONSTRAINT FK_Fund_Sighting FOREIGN KEY (SightingId) REFERENCES Sighting(SightingId)
);

-- ---------- Petition (FK -> Sighting) ----------
CREATE TABLE Petition (
    PetitionId  INT IDENTITY(1,1) NOT NULL,
    SightingId  INT NOT NULL,
    DateCreated DATETIME NOT NULL CONSTRAINT DF_Petition_DateCreated DEFAULT (GETDATE()),
    DemandCount INT NOT NULL CONSTRAINT DF_Petition_DemandCount DEFAULT ((0)),
    CONSTRAINT PK_Petition PRIMARY KEY (PetitionId),
    CONSTRAINT FK_Petition_Sighting FOREIGN KEY (SightingId) REFERENCES Sighting(SightingId)
);

-- ---------- PetitionSignature (FK -> Petition, Users) ----------
CREATE TABLE PetitionSignature (
    SignatureId INT IDENTITY(1,1) NOT NULL,
    PetitionId  INT NOT NULL,
    UserId      INT NOT NULL,
    DateSigned  DATETIME NOT NULL CONSTRAINT DF_PetitionSignature_DateSigned DEFAULT (GETDATE()),
    CONSTRAINT PK_PetitionSignature PRIMARY KEY (SignatureId),
    CONSTRAINT FK_PetitionSignature_Petition FOREIGN KEY (PetitionId) REFERENCES Petition(PetitionId),
    CONSTRAINT FK_PetitionSignature_Users    FOREIGN KEY (UserId)     REFERENCES Users(Id)
);

-- ---------- Comment (FK -> Sighting, Donation) ----------
CREATE TABLE Comment (
    CommentId   INT IDENTITY(1,1) NOT NULL,
    SightingId  INT NOT NULL,
    DonationId  INT NOT NULL,
    CommentText NVARCHAR(500) NOT NULL,
    CONSTRAINT PK_Comment PRIMARY KEY (CommentId),
    CONSTRAINT FK_Comment_Sighting FOREIGN KEY (SightingId) REFERENCES Sighting(SightingId),
    CONSTRAINT FK_Comment_Donation FOREIGN KEY (DonationId) REFERENCES Donation(DonationId)
);
GO


/* =============================================================================
   SECTION 4 — USER-DEFINED FUNCTIONS (3)
   ============================================================================= */

-- 1. Total donations for a sighting (replaces duplicated SUM in C#).
CREATE FUNCTION dbo.fn_TotalFundsForSighting (@SightingId INT)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @total DECIMAL(10,2);
    SELECT @total = ISNULL(SUM(Amount), 0)
    FROM Donation
    WHERE SightingId = @SightingId;
    RETURN @total;
END;
GO

-- 2. Can this user comment? True if they have a donation on this sighting
--    not yet used for a comment.
CREATE FUNCTION dbo.fn_CanUserComment (@UserId INT, @SightingId INT)
RETURNS BIT
AS
BEGIN
    DECLARE @canComment BIT = 0;
    IF EXISTS (
        SELECT 1
        FROM Donation d
        WHERE d.UserId = @UserId
          AND d.SightingId = @SightingId
          AND NOT EXISTS (SELECT 1 FROM Comment c WHERE c.DonationId = d.DonationId)
    )
        SET @canComment = 1;
    RETURN @canComment;
END;
GO


-- 3. Count of sightings a user submitted today (calendar day). Backs the app's
--    daily submission limit.
CREATE FUNCTION dbo.fn_GetUserSubmissionCountToday (@UserId INT)
RETURNS INT
AS
BEGIN
    DECLARE @count INT;
    SELECT @count = COUNT(*)
    FROM Sighting
    WHERE UserId = @UserId
      AND DatePosted >= CAST(GETDATE() AS DATE)
      AND DatePosted <  DATEADD(DAY, 1, CAST(GETDATE() AS DATE));
    RETURN @count;
END;
GO


/* =============================================================================
   SECTION 5 — STORED PROCEDURES (3)
   ============================================================================= */

-- 3. Atomic petition sign/unsign. Does NOT touch DemandCount — the trigger
--    (object 5) owns that. Returns the resulting state.
CREATE PROCEDURE dbo.usp_TogglePetitionSignature
    @PetitionId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM PetitionSignature
                   WHERE PetitionId = @PetitionId AND UserId = @UserId)
            DELETE FROM PetitionSignature
            WHERE PetitionId = @PetitionId AND UserId = @UserId;
        ELSE
            INSERT INTO PetitionSignature (PetitionId, UserId)
            VALUES (@PetitionId, @UserId);

        COMMIT TRANSACTION;

        SELECT
            CAST(CASE WHEN EXISTS (
                SELECT 1 FROM PetitionSignature
                WHERE PetitionId = @PetitionId AND UserId = @UserId
            ) THEN 1 ELSE 0 END AS BIT)                                   AS HasSigned,
            (SELECT DemandCount FROM Petition WHERE PetitionId = @PetitionId) AS DemandCount;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 4. Find the user's eligible (unused) donation and insert the comment
--    atomically; error 50001 if there is none.
CREATE PROCEDURE dbo.usp_PostDonationComment
    @UserId      INT,
    @SightingId  INT,
    @CommentText NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @DonationId INT;

        SELECT TOP 1 @DonationId = d.DonationId
        FROM Donation d
        WHERE d.UserId = @UserId
          AND d.SightingId = @SightingId
          AND NOT EXISTS (SELECT 1 FROM Comment c WHERE c.DonationId = d.DonationId)
        ORDER BY d.DonationId DESC;

        IF @DonationId IS NULL
            THROW 50001, 'No eligible donation to comment on. Make a donation first.', 1;

        INSERT INTO Comment (SightingId, DonationId, CommentText)
        VALUES (@SightingId, @DonationId, @CommentText);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO


-- 3. Deny a sighting: set Status='Denied' + reason. Wraps the admin Deny action.
CREATE PROCEDURE dbo.usp_DenySighting
    @SightingId   INT,
    @DenialReason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Sighting
    SET Status = 'Denied',
        DenialReason = @DenialReason
    WHERE SightingId = @SightingId;
END;
GO


/* =============================================================================
   SECTION 6 — TRIGGERS (3)
   ============================================================================= */

-- 5. Keep Petition.DemandCount in sync with the actual signature rows.
CREATE TRIGGER trg_PetitionSignature_SyncDemandCount
ON PetitionSignature
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    ;WITH Affected AS (
        SELECT PetitionId FROM inserted
        UNION
        SELECT PetitionId FROM deleted
    )
    UPDATE p
    SET DemandCount = (SELECT COUNT(*) FROM PetitionSignature ps
                       WHERE ps.PetitionId = p.PetitionId)
    FROM Petition p
    INNER JOIN Affected a ON a.PetitionId = p.PetitionId;
END;
GO

-- 6. Enforce one comment per donation (backstop; error 50002).
CREATE TRIGGER trg_Comment_EnforceOneCommentPerDonation
ON Comment
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM Comment c
        INNER JOIN inserted i ON i.DonationId = c.DonationId
        GROUP BY c.DonationId
        HAVING COUNT(*) > 1
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50002, 'That donation already has a comment (one comment per donation).', 1;
    END
END;
GO

-- 7. Block deleting the LAST remaining SuperAdmin (backstop; error 50003).
CREATE TRIGGER trg_Users_PreventLastSuperAdminDelete
ON Users
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE Role = 'SuperAdmin')
       AND NOT EXISTS (SELECT 1 FROM Users WHERE Role = 'SuperAdmin')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50003, 'Cannot delete the last remaining SuperAdmin account.', 1;
    END
END;
GO


/* =============================================================================
   SECTION 7 — SEED TEST ACCOUNTS (password '1234' for all three)
   PasswordHash/Salt are real PBKDF2-SHA256 values (100k iterations, 16-byte
   salt, 32-byte hash) generated by the app's PasswordHasher class. Each row
   has its own random salt, so the three hashes differ even with the same
   password. Accounts get Id 1, 2, 3.
   ============================================================================= */
INSERT INTO Users (Username, PasswordHash, Salt, Role) VALUES
 ('admin',        'hVnd3KZK63ndiODkyhkPVHNEcxL1nTN1CxZqFCqOzBI=', 'CnJE8+4iSc2h1/5ZC/QYrQ==', 'Admin'),
 ('superadmin',   'E3Ydu9f53iUuN6ZriAA9k+kPn45jhF1exqxS/tj5ZCI=', 'eixdt6E2xmZBq4pYJ3DS/g==', 'SuperAdmin'),
 ('contributor1', 'gRKWbQqHrviWhQq9vjSnm4SNryTsrBfQF5SVA358ihU=', 'CkuiwB/Ro72qdBo91qBnwA==', 'Contributor');
GO


/* =============================================================================
   SECTION 8 — VERIFY (optional)
   ============================================================================= */
PRINT '=== StandNatura setup complete ===';
SELECT Id, Username, Role FROM Users ORDER BY Id;
GO
