using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ovk.Net.Core.Models;

[Table("messages")]
public class Message
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [Column("sender_type")]
    public string SenderType { get; set; } = "openvk\\Web\\Models\\Entities\\User";

    [Column("sender_id")]
    public ulong SenderId { get; set; }

    [Column("recipient_type")]
    public string RecipientType { get; set; } = "openvk\\Web\\Models\\Entities\\User";

    [Column("recipient_id")]
    public ulong RecipientId { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("created")]
    public ulong Created { get; set; }

    [Column("edited")]
    public ulong? Edited { get; set; }

    [Column("ad")]
    public bool Ad { get; set; }

    [Column("deleted")]
    public bool Deleted { get; set; }

    [Column("unread")]
    public bool Unread { get; set; } = true;
}
