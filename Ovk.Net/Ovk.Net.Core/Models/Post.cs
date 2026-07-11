using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ovk.Net.Core.Models;

[Table("posts")]
public class Post
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("owner")]
    public ulong OwnerId { get; set; }

    [Column("wall")]
    public ulong WallId { get; set; }

    [Column("virtual_id")]
    public ulong VirtualId { get; set; }

    [Column("created")]
    public ulong Created { get; set; }

    [Column("edited")]
    public ulong? Edited { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("flags")]
    public byte? Flags { get; set; }

    [Column("nsfw")]
    public bool Nsfw { get; set; }

    [Column("ad")]
    public bool Ad { get; set; }

    [Column("deleted")]
    public bool Deleted { get; set; }
}
