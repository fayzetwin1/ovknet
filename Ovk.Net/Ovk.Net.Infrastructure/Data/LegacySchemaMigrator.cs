using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Ovk.Net.Infrastructure.Data;

/// <summary>
/// Applies additive, idempotent migrations to databases originally created by
/// Chandler/OpenVK or by early Ovk.Net builds that used EnsureCreated().
/// </summary>
public sealed class LegacySchemaMigrator
{
    private readonly OvkDbContext _db;

    public LegacySchemaMigrator(OvkDbContext db)
    {
        _db = db;
    }

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational()) return;

        await _db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS `OvkNetSchemaMigrations` (
                    `version` varchar(100) NOT NULL,
                    `applied_at` bigint unsigned NOT NULL,
                    PRIMARY KEY (`version`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
                """, cancellationToken);

            await ApplyMigrationAsync("20260711_01_media", MediaCommands, cancellationToken);
            await ApplyMigrationAsync("20260711_02_chandler", ChandlerCommands, cancellationToken);
            await ApplyMigrationAsync("20260711_04_chandler_membership", ChandlerMembershipCommands, cancellationToken);

            if (!await ColumnExistsAsync("subscriptions", "flags", cancellationToken))
            {
                await ExecuteAsync(
                    "ALTER TABLE `subscriptions` ADD COLUMN `flags` tinyint unsigned NOT NULL DEFAULT 0",
                    cancellationToken);
            }
            await RecordAsync("20260711_03_subscription_flags", cancellationToken);
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private async Task ApplyMigrationAsync(
        string version,
        IReadOnlyList<string> commands,
        CancellationToken cancellationToken)
    {
        if (await IsAppliedAsync(version, cancellationToken)) return;
        foreach (var command in commands)
        {
            await ExecuteAsync(command, cancellationToken);
        }
        await RecordAsync(version, cancellationToken);
    }

    private async Task<bool> IsAppliedAsync(string version, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            "SELECT COUNT(*) FROM `OvkNetSchemaMigrations` WHERE `version` = @version");
        AddParameter(command, "@version", version);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private async Task RecordAsync(string version, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand("""
            INSERT IGNORE INTO `OvkNetSchemaMigrations` (`version`, `applied_at`)
            VALUES (@version, @appliedAt)
            """);
        AddParameter(command, "@version", version);
        AddParameter(command, "@appliedAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<bool> ColumnExistsAsync(
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand("""
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @column
            """);
        AddParameter(command, "@table", table);
        AddParameter(command, "@column", column);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateCommand(string sql)
    {
        var command = _db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static readonly string[] MediaCommands =
    [
        """
        CREATE TABLE IF NOT EXISTS `albums` (
            `id` bigint unsigned NOT NULL AUTO_INCREMENT,
            `owner` bigint NOT NULL,
            `name` varchar(36) NOT NULL,
            `description` longtext NULL,
            `access_pragma` tinyint unsigned NOT NULL DEFAULT 255,
            `cover_photo` bigint unsigned NULL,
            `special_type` tinyint unsigned NOT NULL DEFAULT 0,
            `created` bigint unsigned NOT NULL,
            `edited` bigint unsigned NULL,
            `deleted` tinyint(1) NOT NULL DEFAULT 0,
            PRIMARY KEY (`id`),
            KEY `owner` (`owner`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `photos` (
            `id` bigint unsigned NOT NULL AUTO_INCREMENT,
            `owner` bigint NOT NULL,
            `virtual_id` bigint NOT NULL,
            `created` bigint unsigned NOT NULL,
            `edited` bigint unsigned NULL,
            `hash` char(128) NOT NULL,
            `sizes` varbinary(486) NULL,
            `width` smallint unsigned NULL,
            `height` smallint unsigned NULL,
            `anonymous` tinyint(1) NOT NULL DEFAULT 0,
            `system` tinyint unsigned NOT NULL DEFAULT 0,
            `private` tinyint unsigned NOT NULL DEFAULT 0,
            `deleted` tinyint(1) NOT NULL DEFAULT 0,
            `description` longtext NULL,
            PRIMARY KEY (`id`),
            KEY `owner` (`owner`),
            KEY `owner_virtual` (`owner`, `virtual_id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `album_relations` (
            `collection` bigint unsigned NOT NULL,
            `media` bigint unsigned NOT NULL,
            `index` bigint unsigned NOT NULL,
            PRIMARY KEY (`collection`, `media`),
            KEY `media` (`media`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """
    ];

    private static readonly string[] ChandlerCommands =
    [
        """
        CREATE TABLE IF NOT EXISTS `ChandlerUsers` (
            `id` varchar(36) NOT NULL,
            `login` varchar(64) NOT NULL,
            `passwordHash` varchar(136) NOT NULL,
            `deleted` tinyint(1) NOT NULL DEFAULT 0,
            PRIMARY KEY (`id`), UNIQUE KEY `login` (`login`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `ChandlerTokens` (
            `token` varchar(64) NOT NULL,
            `user` varchar(36) NOT NULL,
            `ip` varchar(255) NOT NULL,
            `ua` varchar(1000) NOT NULL,
            PRIMARY KEY (`token`), KEY `user` (`user`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `ChandlerGroups` (
            `id` varchar(36) NOT NULL,
            `name` varchar(100) NOT NULL,
            `color` mediumint unsigned NULL,
            PRIMARY KEY (`id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `ChandlerACLRelations` (
            `user` varchar(36) NOT NULL,
            `group` varchar(36) NOT NULL,
            `priority` bigint unsigned NOT NULL DEFAULT 0,
            PRIMARY KEY (`user`, `group`), KEY `group` (`group`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `ChandlerACLGroupsPermissions` (
            `group` varchar(36) NOT NULL,
            `model` varchar(1000) NOT NULL,
            `context` int unsigned NULL,
            `permission` varchar(36) NOT NULL,
            `status` tinyint(1) NOT NULL DEFAULT 1,
            KEY `group` (`group`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `ChandlerACLPermissionAliases` (
            `alias` varchar(190) NOT NULL,
            `model` varchar(255) NOT NULL,
            `context` varchar(255) NOT NULL,
            `permission` varchar(255) NOT NULL,
            PRIMARY KEY (`alias`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `ChandlerACLUsersPermissions` (
            `user` varchar(36) NOT NULL,
            `model` varchar(1000) NOT NULL,
            `context` int unsigned NOT NULL,
            `permission` int unsigned NOT NULL,
            `status` tinyint(1) NOT NULL DEFAULT 1,
            KEY `user` (`user`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `ChandlerLogs` (
            `id` bigint unsigned NOT NULL AUTO_INCREMENT,
            `user` varchar(36) NOT NULL,
            `type` int NOT NULL,
            `object_table` tinytext NOT NULL,
            `object_model` mediumtext NOT NULL,
            `object_id` bigint unsigned NOT NULL,
            `xdiff_old` longtext NOT NULL,
            `xdiff_new` longtext NOT NULL,
            `ts` bigint NOT NULL,
            `ip` tinytext NOT NULL,
            `useragent` longtext NOT NULL,
            PRIMARY KEY (`id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        INSERT IGNORE INTO `ChandlerGroups` (`id`, `name`, `color`) VALUES
            ('c75fe4de-1e62-11ea-904d-42010aac0003', 'Users', NULL),
            ('594e6cb4-2a3a-11ea-9e1e-42010aac0003', 'Administrators', NULL)
        """,
        """
        INSERT INTO `ChandlerACLGroupsPermissions` (`group`, `model`, `context`, `permission`, `status`)
        SELECT '594e6cb4-2a3a-11ea-9e1e-42010aac0003', 'admin', NULL, 'access', 1
        WHERE NOT EXISTS (
            SELECT 1 FROM `ChandlerACLGroupsPermissions`
            WHERE `group` = '594e6cb4-2a3a-11ea-9e1e-42010aac0003'
              AND `model` = 'admin' AND `permission` = 'access'
        )
        """
    ];

    private static readonly string[] ChandlerMembershipCommands =
    [
        """
        INSERT INTO `ChandlerACLRelations` (`user`, `group`, `priority`)
        SELECT `u`.`id`, 'c75fe4de-1e62-11ea-904d-42010aac0003', 32
        FROM `ChandlerUsers` AS `u`
        WHERE NOT EXISTS (
            SELECT 1 FROM `ChandlerACLRelations` AS `r`
            WHERE BINARY `r`.`user` = BINARY `u`.`id`
              AND BINARY `r`.`group` = BINARY 'c75fe4de-1e62-11ea-904d-42010aac0003'
        )
        """
    ];
}
