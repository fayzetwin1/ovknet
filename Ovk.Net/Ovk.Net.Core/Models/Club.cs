using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ovk.Net.Core.Models;

[Table("groups")]
public class Club
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("about")]
    public string? About { get; set; }

    [Column("owner")]
    public ulong? OwnerId { get; set; }

    [Column("shortcode")]
    public string? Shortcode { get; set; }

    [Column("verified")]
    public bool Verified { get; set; } = false;

    [Column("type")]
    public int Type { get; set; } = 1;

    [Column("closed")]
    public byte Closed { get; set; } = 0;

    [Column("block_reason")]
    public string? BlockReason { get; set; }

    [Column("wall")]
    public int Wall { get; set; } = 1;
}
