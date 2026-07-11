namespace Ovk.Net.Core.Models;

public class Photo
{
    public ulong Id { get; set; }
    public long OwnerId { get; set; }
    public long VirtualId { get; set; }
    public ulong Created { get; set; }
    public ulong? Edited { get; set; }
    public required string Hash { get; set; }
    public bool Deleted { get; set; }
    public string? Description { get; set; }
}
