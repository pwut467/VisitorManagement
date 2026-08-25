IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorUserId] nvarchar(80) NULL,
        [Action] nvarchar(80) NOT NULL,
        [EntityType] nvarchar(80) NULL,
        [EntityId] nvarchar(40) NULL,
        [Detail] nvarchar(1000) NULL,
        [IpAddress] nvarchar(60) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [BlacklistEntries] (
        [Id] int NOT NULL IDENTITY,
        [NationalId] nvarchar(13) NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Reason] nvarchar(400) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [ExpiresAt] datetime2 NULL,
        CONSTRAINT [PK_BlacklistEntries] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [CompanyProfiles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Address] nvarchar(300) NULL,
        [LogoPath] nvarchar(260) NULL,
        [BadgeFooter] nvarchar(200) NOT NULL,
        [DefaultVisitHours] int NOT NULL,
        [OverstayGraceMinutes] int NOT NULL,
        CONSTRAINT [PK_CompanyProfiles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [Gates] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(80) NOT NULL,
        [Location] nvarchar(120) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Gates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [Visitors] (
        [Id] int NOT NULL IDENTITY,
        [NationalId] nvarchar(13) NOT NULL,
        [Title] nvarchar(20) NOT NULL,
        [FirstName] nvarchar(80) NOT NULL,
        [LastName] nvarchar(80) NOT NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(150) NULL,
        [CompanyName] nvarchar(200) NULL,
        [Address] nvarchar(300) NULL,
        [DateOfBirth] datetime2 NULL,
        [PhotoPath] nvarchar(260) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Visitors] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [VisitorTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(80) NOT NULL,
        [BadgeLabel] nvarchar(20) NOT NULL,
        [Color] nvarchar(20) NOT NULL,
        [RequiresEscortDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_VisitorTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [VisitPurposes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_VisitPurposes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeCode] nvarchar(30) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [DepartmentId] int NOT NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(150) NULL,
        [UserId] nvarchar(450) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [Visits] (
        [Id] int NOT NULL IDENTITY,
        [VisitNumber] nvarchar(20) NOT NULL,
        [VisitCode] nvarchar(32) NOT NULL,
        [VisitorId] int NOT NULL,
        [VisitorTypeId] int NOT NULL,
        [VisitPurposeId] int NOT NULL,
        [HostEmployeeId] int NOT NULL,
        [GateInId] int NULL,
        [GateOutId] int NULL,
        [CompanyName] nvarchar(200) NULL,
        [PurposeDetail] nvarchar(300) NULL,
        [VehiclePlate] nvarchar(20) NULL,
        [VehicleType] nvarchar(40) NULL,
        [ItemsBrought] nvarchar(500) NULL,
        [AccompanyingCount] int NOT NULL,
        [AccompanyingNames] nvarchar(300) NULL,
        [RequiresEscort] bit NOT NULL,
        [AccessArea] nvarchar(120) NULL,
        [Notes] nvarchar(500) NULL,
        [AppointmentAt] datetime2 NULL,
        [CheckInAt] datetime2 NULL,
        [CheckOutAt] datetime2 NULL,
        [ExpectedCheckoutAt] datetime2 NULL,
        [BadgePrintedAt] datetime2 NULL,
        [PdpaConsentAt] datetime2 NULL,
        [Status] int NOT NULL,
        [PhotoPath] nvarchar(260) NULL,
        [RegisteredByUserId] nvarchar(450) NULL,
        [CheckedOutByUserId] nvarchar(450) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Visits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Visits_AspNetUsers_CheckedOutByUserId] FOREIGN KEY ([CheckedOutByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_Visits_AspNetUsers_RegisteredByUserId] FOREIGN KEY ([RegisteredByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_Visits_Employees_HostEmployeeId] FOREIGN KEY ([HostEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Visits_Gates_GateInId] FOREIGN KEY ([GateInId]) REFERENCES [Gates] ([Id]),
        CONSTRAINT [FK_Visits_Gates_GateOutId] FOREIGN KEY ([GateOutId]) REFERENCES [Gates] ([Id]),
        CONSTRAINT [FK_Visits_VisitPurposes_VisitPurposeId] FOREIGN KEY ([VisitPurposeId]) REFERENCES [VisitPurposes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Visits_VisitorTypes_VisitorTypeId] FOREIGN KEY ([VisitorTypeId]) REFERENCES [VisitorTypes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Visits_Visitors_VisitorId] FOREIGN KEY ([VisitorId]) REFERENCES [Visitors] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE TABLE [VisitItems] (
        [Id] int NOT NULL IDENTITY,
        [VisitId] int NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [SerialNumber] nvarchar(80) NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_VisitItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VisitItems_Visits_VisitId] FOREIGN KEY ([VisitId]) REFERENCES [Visits] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BlacklistEntries_NationalId] ON [BlacklistEntries] ([NationalId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_Code] ON [Departments] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_EmployeeCode] ON [Employees] ([EmployeeCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_UserId] ON [Employees] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VisitItems_VisitId] ON [VisitItems] ([VisitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visitors_LastName_FirstName] ON [Visitors] ([LastName], [FirstName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Visitors_NationalId] ON [Visitors] ([NationalId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visits_CheckedOutByUserId] ON [Visits] ([CheckedOutByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visits_CheckInAt] ON [Visits] ([CheckInAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visits_GateInId] ON [Visits] ([GateInId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visits_GateOutId] ON [Visits] ([GateOutId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visits_HostEmployeeId] ON [Visits] ([HostEmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visits_RegisteredByUserId] ON [Visits] ([RegisteredByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visits_Status] ON [Visits] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Visits_VisitCode] ON [Visits] ([VisitCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Visits_VisitNumber] ON [Visits] ([VisitNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visits_VisitorId] ON [Visits] ([VisitorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visits_VisitorTypeId] ON [Visits] ([VisitorTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Visits_VisitPurposeId] ON [Visits] ([VisitPurposeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825021531_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825021531_InitialCreate', N'8.0.19');
END;
GO

COMMIT;
GO

