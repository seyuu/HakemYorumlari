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

CREATE TABLE [Maclar] (
    [Id] int NOT NULL IDENTITY,
    [EvSahibi] nvarchar(100) NOT NULL,
    [Deplasman] nvarchar(100) NOT NULL,
    [MacTarihi] datetime2 NOT NULL,
    [Skor] nvarchar(20) NOT NULL,
    [Liga] nvarchar(50) NOT NULL,
    [Hafta] int NOT NULL,
    CONSTRAINT [PK_Maclar] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Pozisyonlar] (
    [Id] int NOT NULL IDENTITY,
    [MacId] int NOT NULL,
    [Aciklama] nvarchar(200) NOT NULL,
    [Dakika] int NOT NULL,
    [PozisyonTuru] nvarchar(50) NOT NULL,
    [VideoUrl] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_Pozisyonlar] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Pozisyonlar_Maclar_MacId] FOREIGN KEY ([MacId]) REFERENCES [Maclar] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [HakemYorumlari] (
    [Id] int NOT NULL IDENTITY,
    [PozisyonId] int NOT NULL,
    [YorumcuAdi] nvarchar(100) NOT NULL,
    [Yorum] nvarchar(1000) NOT NULL,
    [DogruKarar] bit NOT NULL,
    [YorumTarihi] datetime2 NOT NULL,
    [Kanal] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_HakemYorumlari] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HakemYorumlari_Pozisyonlar_PozisyonId] FOREIGN KEY ([PozisyonId]) REFERENCES [Pozisyonlar] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [KullaniciAnketleri] (
    [Id] int NOT NULL IDENTITY,
    [PozisyonId] int NOT NULL,
    [KullaniciIp] nvarchar(50) NOT NULL,
    [DogruKarar] bit NOT NULL,
    [OyTarihi] datetime2 NOT NULL,
    CONSTRAINT [PK_KullaniciAnketleri] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KullaniciAnketleri_Pozisyonlar_PozisyonId] FOREIGN KEY ([PozisyonId]) REFERENCES [Pozisyonlar] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_HakemYorumlari_PozisyonId] ON [HakemYorumlari] ([PozisyonId]);
GO

CREATE UNIQUE INDEX [IX_KullaniciAnketleri_PozisyonId_KullaniciIp] ON [KullaniciAnketleri] ([PozisyonId], [KullaniciIp]);
GO

CREATE INDEX [IX_Pozisyonlar_MacId] ON [Pozisyonlar] ([MacId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250817110814_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Pozisyonlar]') AND [c].[name] = N'VideoUrl');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Pozisyonlar] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Pozisyonlar] ALTER COLUMN [VideoUrl] nvarchar(max) NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250817121416_UpdateModels', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Pozisyonlar] ADD [EmbedVideoUrl] nvarchar(500) NULL;
GO

ALTER TABLE [Pozisyonlar] ADD [VideoKaynagi] nvarchar(100) NULL;
GO

ALTER TABLE [Maclar] ADD [Durum] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Maclar] ADD [OtomatikYorumToplamaAktif] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Maclar] ADD [YorumToplamaNotlari] nvarchar(max) NULL;
GO

ALTER TABLE [Maclar] ADD [YorumToplamaZamani] datetime2 NULL;
GO

ALTER TABLE [Maclar] ADD [YorumlarToplandi] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [HakemYorumlari] ADD [KaynakLink] nvarchar(500) NULL;
GO

ALTER TABLE [HakemYorumlari] ADD [KaynakTuru] nvarchar(100) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250817132414_UpdateMacModel', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Pozisyonlar] ADD [HakemKarari] nvarchar(100) NULL;
GO

ALTER TABLE [Pozisyonlar] ADD [TartismaDerecesi] int NOT NULL DEFAULT 0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250817161835_AddPozisyonProperties', N'8.0.0');
GO

COMMIT;
GO

