using System;

namespace Ovk.Net.Core.Models;

/// <summary>
/// Represents the `profiles` table from the OpenVK application.
/// Manages social and profile data for a user.
/// </summary>
public class Profile
{
    public ulong Id { get; set; }
    
    // Foreign key to ChandlerUsers.Id, but stored as string in legacy db
    public required string UserId { get; set; } 
    
    public string FirstName { get; set; } = "Jane";
    public string LastName { get; set; } = "Doe";
    public string? Pseudo { get; set; }
    
    public string? Info { get; set; }
    public string? About { get; set; }
    public string? Status { get; set; }
    
    public ulong Privacy { get; set; } = 1099511627775;
    public ulong LeftMenu { get; set; } = 1099511627775;
    
    public bool Sex { get; set; } = true;
    public sbyte Type { get; set; } = 0;
    
    public string? Phone { get; set; }
    public string? Email { get; set; }
    
    public ulong Coins { get; set; } = 0;
    public DateTime Since { get; set; }
    
    public string? BlockReason { get; set; }
    public bool Verified { get; set; } = false;
    
    public long Reputation { get; set; } = 1000;
    public string? Shortcode { get; set; }
    
    public string RegisteringIp { get; set; } = "127.0.0.1";
    public ulong Online { get; set; } = 0;
    
    public long? Birthday { get; set; } = 0;
    public string? Hometown { get; set; }
    
    public int? PolitViews { get; set; } = 0;
    public int? MaritalStatus { get; set; } = 0;
    
    public string? City { get; set; }
    public string? Address { get; set; }
    
    public string Style { get; set; } = "ovk";
    public int? StyleAvatar { get; set; } = 0;
    public bool? ShowRating { get; set; } = true;
    public bool Milkshake { get; set; } = false;
    
    public byte NsfwTolerance { get; set; } = 0;
    public ulong? NotificationOffset { get; set; } = 0;
    
    public byte Deleted { get; set; } = 0;
    public byte Microblog { get; set; } = 0;
}
