-- DataBision — database init script
-- Generated to match EF Core Fluent API configurations in DataBision.Infrastructure
-- SQLite dialect (dev). SQL Server equivalent follows after the SQLite block.
-- Use this as a reference / fallback if EF migrations are not available.
-- Run:  sqlite3 databision_dev.db < sql/schema.sql

-- ============================================================
-- SQLite (development)
-- ============================================================

PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS companies (
    Id           TEXT    NOT NULL PRIMARY KEY,
    Name         TEXT    NOT NULL,
    Slug         TEXT    NOT NULL,
    Status       TEXT    NOT NULL DEFAULT 'Active',
    CreatedAt    TEXT    NOT NULL,
    UpdatedAt    TEXT    NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_companies_Slug ON companies (Slug);

CREATE TABLE IF NOT EXISTS company_branding (
    Id                  TEXT NOT NULL PRIMARY KEY,
    CompanyId           TEXT NOT NULL,
    PrimaryColor        TEXT NOT NULL,
    SecondaryColor      TEXT NOT NULL,
    AccentColor         TEXT NOT NULL,
    BackgroundColor     TEXT NOT NULL,
    SidebarColor        TEXT NOT NULL,
    LogoUrl             TEXT,
    FaviconUrl          TEXT,
    CompanyDisplayName  TEXT,
    FOREIGN KEY (CompanyId) REFERENCES companies (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_company_branding_CompanyId ON company_branding (CompanyId);

CREATE TABLE IF NOT EXISTS users (
    Id           TEXT NOT NULL PRIMARY KEY,
    Email        TEXT NOT NULL,
    PasswordHash TEXT NOT NULL,
    FirstName    TEXT NOT NULL,
    LastName     TEXT NOT NULL,
    Role         TEXT NOT NULL DEFAULT 'Viewer',
    IsActive     INTEGER NOT NULL DEFAULT 1,
    CreatedAt    TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_users_Email ON users (Email);

CREATE TABLE IF NOT EXISTS user_companies (
    Id        TEXT NOT NULL PRIMARY KEY,
    UserId    TEXT NOT NULL,
    CompanyId TEXT NOT NULL,
    FOREIGN KEY (UserId)    REFERENCES users     (Id) ON DELETE CASCADE,
    FOREIGN KEY (CompanyId) REFERENCES companies (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_user_companies_UserId_CompanyId ON user_companies (UserId, CompanyId);

CREATE TABLE IF NOT EXISTS modules (
    Id        TEXT    NOT NULL PRIMARY KEY,
    Name      TEXT    NOT NULL,
    Slug      TEXT    NOT NULL,
    Icon      TEXT,
    SortOrder INTEGER NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_modules_Slug ON modules (Slug);

CREATE TABLE IF NOT EXISTS reports (
    Id                  TEXT    NOT NULL PRIMARY KEY,
    ModuleId            TEXT    NOT NULL,
    CompanyId           TEXT    NOT NULL,
    Name                TEXT    NOT NULL,
    Description         TEXT,
    PowerBiWorkspaceId  TEXT    NOT NULL,
    PowerBiReportId     TEXT    NOT NULL,
    PowerBiDatasetId    TEXT    NOT NULL,
    IsActive            INTEGER NOT NULL DEFAULT 1,
    SortOrder           INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (ModuleId)   REFERENCES modules   (Id) ON DELETE NO ACTION,
    FOREIGN KEY (CompanyId)  REFERENCES companies (Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS user_permissions (
    Id        TEXT NOT NULL PRIMARY KEY,
    UserId    TEXT NOT NULL,
    CompanyId TEXT NOT NULL,
    ModuleId  TEXT NOT NULL,
    ReportId  TEXT,
    FOREIGN KEY (UserId)    REFERENCES users    (Id) ON DELETE CASCADE,
    FOREIGN KEY (CompanyId) REFERENCES companies(Id) ON DELETE NO ACTION,
    FOREIGN KEY (ModuleId)  REFERENCES modules  (Id) ON DELETE NO ACTION,
    FOREIGN KEY (ReportId)  REFERENCES reports  (Id) ON DELETE NO ACTION
);
CREATE INDEX IF NOT EXISTS IX_user_permissions_UserId_CompanyId ON user_permissions (UserId, CompanyId);

CREATE TABLE IF NOT EXISTS refresh_tokens (
    Id        TEXT NOT NULL PRIMARY KEY,
    TokenHash TEXT NOT NULL,
    UserId    TEXT NOT NULL,
    CompanyId TEXT,
    ExpiresAt TEXT NOT NULL,
    RevokedAt TEXT,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (UserId)    REFERENCES users    (Id) ON DELETE CASCADE,
    FOREIGN KEY (CompanyId) REFERENCES companies(Id) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_refresh_tokens_TokenHash ON refresh_tokens (TokenHash);

CREATE TABLE IF NOT EXISTS audit_logs (
    Id           INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    UserId       TEXT,
    CompanyId    TEXT,
    Action       TEXT    NOT NULL,
    ResourceType TEXT,
    ResourceId   TEXT,
    Metadata     TEXT,
    IpAddress    TEXT,
    UserAgent    TEXT,
    CreatedAt    TEXT    NOT NULL,
    FOREIGN KEY (UserId)    REFERENCES users    (Id) ON DELETE NO ACTION,
    FOREIGN KEY (CompanyId) REFERENCES companies(Id) ON DELETE NO ACTION
);
CREATE INDEX IF NOT EXISTS IX_audit_logs_CompanyId_CreatedAt ON audit_logs (CompanyId, CreatedAt);
CREATE INDEX IF NOT EXISTS IX_audit_logs_UserId_CreatedAt    ON audit_logs (UserId,    CreatedAt);


-- ============================================================
-- SQL Server (production) — run these instead of the block above
-- ============================================================
/*

CREATE TABLE companies (
    Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name      NVARCHAR(200)    NOT NULL,
    Slug      NVARCHAR(100)    NOT NULL,
    Status    NVARCHAR(20)     NOT NULL CONSTRAINT DF_companies_Status DEFAULT 'Active',
    CreatedAt DATETIME2        NOT NULL,
    UpdatedAt DATETIME2        NOT NULL
);
CREATE UNIQUE INDEX IX_companies_Slug ON companies (Slug);

CREATE TABLE company_branding (
    Id                 UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    CompanyId          UNIQUEIDENTIFIER NOT NULL,
    PrimaryColor       NVARCHAR(7)      NOT NULL,
    SecondaryColor     NVARCHAR(7)      NOT NULL,
    AccentColor        NVARCHAR(7)      NOT NULL,
    BackgroundColor    NVARCHAR(7)      NOT NULL,
    SidebarColor       NVARCHAR(7)      NOT NULL,
    LogoUrl            NVARCHAR(500),
    FaviconUrl         NVARCHAR(500),
    CompanyDisplayName NVARCHAR(200),
    CONSTRAINT FK_company_branding_companies FOREIGN KEY (CompanyId)
        REFERENCES companies (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_company_branding_CompanyId ON company_branding (CompanyId);

CREATE TABLE users (
    Id           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Email        NVARCHAR(200)    NOT NULL,
    PasswordHash NVARCHAR(500)    NOT NULL,
    FirstName    NVARCHAR(100)    NOT NULL,
    LastName     NVARCHAR(100)    NOT NULL,
    Role         NVARCHAR(20)     NOT NULL CONSTRAINT DF_users_Role DEFAULT 'Viewer',
    IsActive     BIT              NOT NULL CONSTRAINT DF_users_IsActive DEFAULT 1,
    CreatedAt    DATETIME2        NOT NULL
);
CREATE UNIQUE INDEX IX_users_Email ON users (Email);

CREATE TABLE user_companies (
    Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    UserId    UNIQUEIDENTIFIER NOT NULL,
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT FK_user_companies_users     FOREIGN KEY (UserId)    REFERENCES users     (Id) ON DELETE CASCADE,
    CONSTRAINT FK_user_companies_companies FOREIGN KEY (CompanyId) REFERENCES companies (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_user_companies_UserId_CompanyId ON user_companies (UserId, CompanyId);

CREATE TABLE modules (
    Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name      NVARCHAR(100)    NOT NULL,
    Slug      NVARCHAR(50)     NOT NULL,
    Icon      NVARCHAR(50),
    SortOrder INT              NOT NULL CONSTRAINT DF_modules_SortOrder DEFAULT 0
);
CREATE UNIQUE INDEX IX_modules_Slug ON modules (Slug);

CREATE TABLE reports (
    Id                 UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ModuleId           UNIQUEIDENTIFIER NOT NULL,
    CompanyId          UNIQUEIDENTIFIER NOT NULL,
    Name               NVARCHAR(200)    NOT NULL,
    Description        NVARCHAR(500),
    PowerBiWorkspaceId NVARCHAR(100)    NOT NULL,
    PowerBiReportId    NVARCHAR(100)    NOT NULL,
    PowerBiDatasetId   NVARCHAR(100)    NOT NULL,
    IsActive           BIT              NOT NULL CONSTRAINT DF_reports_IsActive DEFAULT 1,
    SortOrder          INT              NOT NULL CONSTRAINT DF_reports_SortOrder DEFAULT 0,
    CONSTRAINT FK_reports_modules   FOREIGN KEY (ModuleId)  REFERENCES modules   (Id),
    CONSTRAINT FK_reports_companies FOREIGN KEY (CompanyId) REFERENCES companies (Id) ON DELETE CASCADE
);

CREATE TABLE user_permissions (
    Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    UserId    UNIQUEIDENTIFIER NOT NULL,
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    ModuleId  UNIQUEIDENTIFIER NOT NULL,
    ReportId  UNIQUEIDENTIFIER,
    CONSTRAINT FK_user_permissions_users    FOREIGN KEY (UserId)    REFERENCES users    (Id) ON DELETE CASCADE,
    CONSTRAINT FK_user_permissions_companies FOREIGN KEY (CompanyId) REFERENCES companies(Id),
    CONSTRAINT FK_user_permissions_modules  FOREIGN KEY (ModuleId)  REFERENCES modules  (Id),
    CONSTRAINT FK_user_permissions_reports  FOREIGN KEY (ReportId)  REFERENCES reports  (Id)
);
CREATE INDEX IX_user_permissions_UserId_CompanyId ON user_permissions (UserId, CompanyId);

CREATE TABLE refresh_tokens (
    Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    TokenHash NVARCHAR(500)    NOT NULL,
    UserId    UNIQUEIDENTIFIER NOT NULL,
    CompanyId UNIQUEIDENTIFIER,
    ExpiresAt DATETIME2        NOT NULL,
    RevokedAt DATETIME2,
    CreatedAt DATETIME2        NOT NULL,
    CONSTRAINT FK_refresh_tokens_users     FOREIGN KEY (UserId)    REFERENCES users    (Id) ON DELETE CASCADE,
    CONSTRAINT FK_refresh_tokens_companies FOREIGN KEY (CompanyId) REFERENCES companies(Id)
);
CREATE UNIQUE INDEX IX_refresh_tokens_TokenHash ON refresh_tokens (TokenHash);

CREATE TABLE audit_logs (
    Id           BIGINT        NOT NULL PRIMARY KEY IDENTITY(1,1),
    UserId       UNIQUEIDENTIFIER,
    CompanyId    UNIQUEIDENTIFIER,
    Action       NVARCHAR(100) NOT NULL,
    ResourceType NVARCHAR(100),
    ResourceId   NVARCHAR(100),
    Metadata     NVARCHAR(MAX),
    IpAddress    NVARCHAR(45),
    UserAgent    NVARCHAR(500),
    CreatedAt    DATETIME2     NOT NULL,
    CONSTRAINT FK_audit_logs_users     FOREIGN KEY (UserId)    REFERENCES users    (Id),
    CONSTRAINT FK_audit_logs_companies FOREIGN KEY (CompanyId) REFERENCES companies(Id)
);
CREATE INDEX IX_audit_logs_CompanyId_CreatedAt ON audit_logs (CompanyId, CreatedAt);
CREATE INDEX IX_audit_logs_UserId_CreatedAt    ON audit_logs (UserId,    CreatedAt);

*/
