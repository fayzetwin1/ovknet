namespace Ovk.Net.Core.Models;

public class ChandlerAclRelation
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
    public ulong Priority { get; set; }
}
