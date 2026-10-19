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
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE TABLE [Members] (
        [MemberId] int NOT NULL IDENTITY,
        [KodeMember] nvarchar(20) NOT NULL,
        [NamaLengkap] nvarchar(100) NOT NULL,
        [NoTelepon] nvarchar(15) NOT NULL,
        [Email] nvarchar(100) NULL,
        [Alamat] nvarchar(255) NULL,
        [TglLahir] date NULL,
        [JenisKelamin] char(1) NULL,
        [KontakDarurat] nvarchar(100) NULL,
        [CatatanKesehatan] nvarchar(1000) NULL,
        [TglBergabung] date NOT NULL DEFAULT (CAST(GETDATE() AS DATE)),
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Members] PRIMARY KEY ([MemberId]),
        CONSTRAINT [CK_Members_JenisKelamin] CHECK ([JenisKelamin] IN ('L','P'))
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE TABLE [MembershipPlans] (
        [PlanId] int NOT NULL IDENTITY,
        [NamaPaket] nvarchar(50) NOT NULL,
        [DurasiHari] int NOT NULL,
        [Harga] decimal(12,2) NOT NULL,
        [Deskripsi] nvarchar(255) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_MembershipPlans] PRIMARY KEY ([PlanId]),
        CONSTRAINT [CK_Plans_DurasiHari] CHECK ([DurasiHari] > 0),
        CONSTRAINT [CK_Plans_Harga] CHECK ([Harga] >= 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [UserId] int NOT NULL IDENTITY,
        [Username] nvarchar(50) NOT NULL,
        [PasswordHash] varbinary(32) NOT NULL,
        [PasswordSalt] varbinary(16) NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [NamaLengkap] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [Theme] nvarchar(10) NOT NULL DEFAULT N'Dark',
        [FailedLoginCount] int NOT NULL DEFAULT 0,
        [LockedUntil] datetime2 NULL,
        [MustChangePassword] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_Users] PRIMARY KEY ([UserId]),
        CONSTRAINT [CK_Users_Role] CHECK ([Role] IN ('Admin','Resepsionis','Instruktur'))
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE TABLE [Trainers] (
        [TrainerId] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [Nama] nvarchar(100) NOT NULL,
        [NoTelepon] nvarchar(15) NULL,
        [Keahlian] nvarchar(100) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Trainers] PRIMARY KEY ([TrainerId]),
        CONSTRAINT [FK_Trainers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE TABLE [Transactions] (
        [TransactionId] int NOT NULL IDENTITY,
        [NoKuitansi] nvarchar(20) NOT NULL,
        [MemberId] int NOT NULL,
        [PlanId] int NOT NULL,
        [UserIdKasir] int NOT NULL,
        [TglTransaksi] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
        [TglMulaiAktif] date NOT NULL,
        [TglBerakhir] date NOT NULL,
        [HargaPaket] decimal(12,2) NOT NULL,
        [Diskon] decimal(12,2) NOT NULL DEFAULT 0.0,
        [TotalBayar] decimal(12,2) NOT NULL,
        [MetodeBayar] nvarchar(10) NOT NULL,
        [Status] nvarchar(10) NOT NULL DEFAULT N'Lunas',
        [AlasanBatal] nvarchar(255) NULL,
        CONSTRAINT [PK_Transactions] PRIMARY KEY ([TransactionId]),
        CONSTRAINT [CK_Trx_Diskon] CHECK ([Diskon] <= [HargaPaket]),
        CONSTRAINT [CK_Trx_MetodeBayar] CHECK ([MetodeBayar] IN ('Tunai','Transfer')),
        CONSTRAINT [CK_Trx_Status] CHECK ([Status] IN ('Lunas','Batal')),
        CONSTRAINT [CK_Trx_Tanggal] CHECK ([TglBerakhir] >= [TglMulaiAktif]),
        CONSTRAINT [FK_Transactions_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([MemberId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Transactions_MembershipPlans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [MembershipPlans] ([PlanId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Transactions_Users_UserIdKasir] FOREIGN KEY ([UserIdKasir]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE TABLE [PTAssignments] (
        [AssignmentId] int NOT NULL IDENTITY,
        [MemberId] int NOT NULL,
        [TrainerId] int NOT NULL,
        [TglMulai] date NOT NULL,
        [TglSelesai] date NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_PTAssignments] PRIMARY KEY ([AssignmentId]),
        CONSTRAINT [FK_PTAssignments_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([MemberId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PTAssignments_Trainers_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Trainers] ([TrainerId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PlanId', N'Deskripsi', N'DurasiHari', N'Harga', N'IsActive', N'NamaPaket') AND [object_id] = OBJECT_ID(N'[MembershipPlans]'))
        SET IDENTITY_INSERT [MembershipPlans] ON;
    EXEC(N'INSERT INTO [MembershipPlans] ([PlanId], [Deskripsi], [DurasiHari], [Harga], [IsActive], [NamaPaket])
    VALUES (1, N''Akses gym 30 hari'', 30, 250000.0, CAST(1 AS bit), N''Bronze 1 Bulan''),
    (2, N''Akses gym 90 hari'', 90, 675000.0, CAST(1 AS bit), N''Silver 3 Bulan''),
    (3, N''Akses gym 365 hari'', 365, 2400000.0, CAST(1 AS bit), N''Gold 12 Bulan'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PlanId', N'Deskripsi', N'DurasiHari', N'Harga', N'IsActive', N'NamaPaket') AND [object_id] = OBJECT_ID(N'[MembershipPlans]'))
        SET IDENTITY_INSERT [MembershipPlans] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'IsActive', N'LockedUntil', N'MustChangePassword', N'NamaLengkap', N'PasswordHash', N'PasswordSalt', N'Role', N'Theme', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
        SET IDENTITY_INSERT [Users] ON;
    EXEC(N'INSERT INTO [Users] ([UserId], [IsActive], [LockedUntil], [MustChangePassword], [NamaLengkap], [PasswordHash], [PasswordSalt], [Role], [Theme], [Username])
    VALUES (1, CAST(1 AS bit), NULL, CAST(1 AS bit), N''Administrator'', 0x2004B430F4920BBC1B3350FF9DA1DD57C445706BE246860B7A4643E5759844DA, 0x47594D2D5359532D61646D696E2D7632, N''Admin'', N''Dark'', N''admin'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'IsActive', N'LockedUntil', N'MustChangePassword', N'NamaLengkap', N'PasswordHash', N'PasswordSalt', N'Role', N'Theme', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
        SET IDENTITY_INSERT [Users] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Members_KodeMember] ON [Members] ([KodeMember]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Members_Nama] ON [Members] ([NamaLengkap]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Members_NoTelepon] ON [Members] ([NoTelepon]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MembershipPlans_NamaPaket] ON [MembershipPlans] ([NamaPaket]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PTAssignments_TrainerId] ON [PTAssignments] ([TrainerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PT_MemberAktif] ON [PTAssignments] ([MemberId]) WHERE [IsActive] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Trainers_UserId] ON [Trainers] ([UserId]) WHERE [UserId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Transactions_NoKuitansi] ON [Transactions] ([NoKuitansi]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Transactions_PlanId] ON [Transactions] ([PlanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Transactions_UserIdKasir] ON [Transactions] ([UserIdKasir]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Trx_Member] ON [Transactions] ([MemberId], [Status], [TglBerakhir]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    EXEC(N'
    CREATE VIEW vw_MemberStatus AS
    SELECT m.MemberId, m.KodeMember, m.NamaLengkap, m.NoTelepon,
           t.TglBerakhirTerakhir,
           CASE
               WHEN m.IsActive = 0 THEN ''Nonaktif''
               WHEN t.TglBerakhirTerakhir IS NULL THEN ''Belum Berlangganan''
               WHEN CAST(GETDATE() AS DATE) > t.TglBerakhirTerakhir THEN ''Kedaluwarsa''
               WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), t.TglBerakhirTerakhir) < 7 THEN ''Akan Habis''
               ELSE ''Aktif''
           END AS Status
    FROM Members m
    OUTER APPLY (
        SELECT MAX(TglBerakhir) AS TglBerakhirTerakhir
        FROM Transactions
        WHERE MemberId = m.MemberId AND Status = ''Lunas''
    ) t;')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019065214_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019065214_InitialCreate', N'8.0.32');
END;
GO

COMMIT;
GO

