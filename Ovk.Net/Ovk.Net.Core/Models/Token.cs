using System;

namespace Ovk.Net.Core.Models;

/// <summary>
/// Represents the `ChandlerTokens` table from the legacy Chandler framework.
/// Manages user sessions.
/// </summary>
public class Token
{
    public required string TokenId { get; set; }
    
    public Guid UserId { get; set; }
    
    public required string Ip { get; set; }
    
    public required string Ua { get; set; }
}
