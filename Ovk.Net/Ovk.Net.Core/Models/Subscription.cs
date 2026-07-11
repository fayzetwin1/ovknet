using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ovk.Net.Core.Models;

[Table("subscriptions")]
public class Subscription
{
    // The table does not have a single primary key in Chandler, but we will use composite key in EF Core.
    // It's mapped as:
    // `follower` bigint(20) UNSIGNED NOT NULL
    // `model` longtext COLLATE utf8mb4_unicode_520_ci NOT NULL
    // `target` bigint(20) UNSIGNED NOT NULL
    // `flags` tinyint unsigned NOT NULL DEFAULT 0

    [Column("follower")]
    public ulong FollowerId { get; set; }

    [Column("model")]
    public string TargetModel { get; set; } = "openvk\\Web\\Models\\Entities\\User";

    [Column("target")]
    public ulong TargetId { get; set; }

    [Column("flags")]
    public byte Flags { get; set; }
}
