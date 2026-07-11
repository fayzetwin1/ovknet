namespace Ovk.Net.Core.Models;

public class Album
{
    public ulong Id { get; set; }
    public long OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public byte AccessPragma { get; set; } = 255;
    public ulong? CoverPhotoId { get; set; }
    public byte SpecialType { get; set; }
    public ulong Created { get; set; }
    public ulong? Edited { get; set; }
    public bool Deleted { get; set; }
}
