using System;

namespace Ovk.Net.Core.Models;

/// <summary>
/// Represents the `ChandlerUsers` table from the legacy Chandler framework.
/// Manages basic authentication and credentials.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    
    public required string Login { get; set; }
    
    public required string PasswordHash { get; set; }
    
    public bool Deleted { get; set; }
}
