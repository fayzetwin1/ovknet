using Microsoft.EntityFrameworkCore;
using Ovk.Net.Core.Models;

namespace Ovk.Net.Infrastructure.Data;

public class OvkDbContext : DbContext
{
    public OvkDbContext(DbContextOptions<OvkDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Token> Tokens { get; set; } = null!;
    public DbSet<Profile> Profiles { get; set; } = null!;
    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<Club> Clubs { get; set; } = null!;
    public DbSet<Album> Albums { get; set; } = null!;
    public DbSet<AlbumRelation> AlbumRelations { get; set; } = null!;
    public DbSet<Photo> Photos { get; set; } = null!;
    public DbSet<ChandlerAclRelation> ChandlerAclRelations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map ChandlerUsers
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("ChandlerUsers");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("varchar(36)")
                .ValueGeneratedOnAdd();
                
            entity.Property(e => e.Login)
                .HasColumnName("login")
                .HasColumnType("varchar(64)")
                .IsRequired();
                
            entity.HasIndex(e => e.Login)
                .IsUnique();

            entity.Property(e => e.PasswordHash)
                .HasColumnName("passwordHash")
                .HasColumnType("varchar(136)")
                .IsRequired();

            entity.Property(e => e.Deleted)
                .HasColumnName("deleted")
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(false);
        });

        // Map ChandlerTokens
        modelBuilder.Entity<Token>(entity =>
        {
            entity.ToTable("ChandlerTokens");
            
            entity.HasKey(e => e.TokenId);
            entity.Property(e => e.TokenId)
                .HasColumnName("token")
                .HasColumnType("varchar(64)");

            entity.Property(e => e.UserId)
                .HasColumnName("user")
                .HasColumnType("varchar(36)");

            entity.Property(e => e.Ip)
                .HasColumnName("ip")
                .HasColumnType("varchar(255)");

            entity.Property(e => e.Ua)
                .HasColumnName("ua")
                .HasColumnType("varchar(1000)");
        });

        // Map OpenVK Profiles
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .HasColumnName("user")
                .HasColumnType("varchar(36)")
                .IsRequired();

            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.Pseudo).HasColumnName("pseudo");
            entity.Property(e => e.Info).HasColumnName("info");
            entity.Property(e => e.About).HasColumnName("about");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Privacy).HasColumnName("privacy");
            entity.Property(e => e.LeftMenu).HasColumnName("left_menu");
            entity.Property(e => e.Sex).HasColumnName("sex");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Email).HasColumnName("email");
            
            entity.HasIndex(e => e.Phone).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.Coins).HasColumnName("coins");
            entity.Property(e => e.Since).HasColumnName("since").HasColumnType("datetime");
            entity.Property(e => e.BlockReason).HasColumnName("block_reason");
            entity.Property(e => e.Verified).HasColumnName("verified");
            entity.Property(e => e.Reputation).HasColumnName("reputation");
            
            entity.Property(e => e.Shortcode).HasColumnName("shortcode");
            entity.HasIndex(e => e.Shortcode).IsUnique();

            entity.Property(e => e.RegisteringIp).HasColumnName("registering_ip");
            entity.Property(e => e.Online).HasColumnName("online");
            entity.Property(e => e.Birthday).HasColumnName("birthday");
            entity.Property(e => e.Hometown).HasColumnName("hometown");
            entity.Property(e => e.PolitViews).HasColumnName("polit_views");
            entity.Property(e => e.MaritalStatus).HasColumnName("marital_status");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Style).HasColumnName("style");
            entity.Property(e => e.StyleAvatar).HasColumnName("style_avatar");
            entity.Property(e => e.ShowRating).HasColumnName("show_rating");
            entity.Property(e => e.Milkshake).HasColumnName("milkshake");
            entity.Property(e => e.NsfwTolerance).HasColumnName("nsfw_tolerance");
            entity.Property(e => e.NotificationOffset).HasColumnName("notification_offset");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.Microblog).HasColumnName("microblog");
        });

        // Map OpenVK Posts
        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToTable("posts");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
                
            entity.Property(e => e.OwnerId).HasColumnName("owner");
            entity.Property(e => e.WallId).HasColumnName("wall");
            entity.Property(e => e.VirtualId).HasColumnName("virtual_id");
            entity.Property(e => e.Created).HasColumnName("created");
            entity.Property(e => e.Edited).HasColumnName("edited");
            entity.Property(e => e.Content).HasColumnName("content").HasColumnType("longtext");
            entity.Property(e => e.Flags).HasColumnName("flags");
            entity.Property(e => e.Nsfw).HasColumnName("nsfw");
            entity.Property(e => e.Ad).HasColumnName("ad");
            entity.Property(e => e.Deleted).HasColumnName("deleted");

            entity.HasIndex(e => e.WallId);
        });

        // Subscriptions mappings
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions");
            entity.HasKey(e => new { e.FollowerId, e.TargetId, e.TargetModel });
            entity.Property(e => e.FollowerId).HasColumnName("follower");
            entity.Property(e => e.TargetId).HasColumnName("target");
            entity.Property(e => e.TargetModel).HasColumnName("model").HasMaxLength(255);
            entity.Property(e => e.Flags).HasColumnName("flags").HasDefaultValue((byte)0);
        });

        // Map OpenVK Messages
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            
            entity.Property(e => e.SenderType).HasColumnName("sender_type");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.RecipientType).HasColumnName("recipient_type");
            entity.Property(e => e.RecipientId).HasColumnName("recipient_id");
            entity.Property(e => e.Content).HasColumnName("content").HasColumnType("longtext");
            entity.Property(e => e.Created).HasColumnName("created");
            entity.Property(e => e.Edited).HasColumnName("edited");
            entity.Property(e => e.Ad).HasColumnName("ad");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.Unread).HasColumnName("unread");
        });

        // Map OpenVK Groups/Clubs
        modelBuilder.Entity<Club>(entity =>
        {
            entity.ToTable("groups");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.About).HasColumnName("about").HasColumnType("mediumtext");
            entity.Property(e => e.OwnerId).HasColumnName("owner");
            entity.Property(e => e.Shortcode).HasColumnName("shortcode");
            entity.Property(e => e.Verified).HasColumnName("verified");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Closed).HasColumnName("closed");
            entity.Property(e => e.BlockReason).HasColumnName("block_reason").HasColumnType("text");
            entity.Property(e => e.Wall).HasColumnName("wall");
        });

        modelBuilder.Entity<Album>(entity =>
        {
            entity.ToTable("albums");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.OwnerId).HasColumnName("owner");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.AccessPragma).HasColumnName("access_pragma");
            entity.Property(e => e.CoverPhotoId).HasColumnName("cover_photo");
            entity.Property(e => e.SpecialType).HasColumnName("special_type");
            entity.Property(e => e.Created).HasColumnName("created");
            entity.Property(e => e.Edited).HasColumnName("edited");
            entity.Property(e => e.Deleted).HasColumnName("deleted");
        });

        modelBuilder.Entity<AlbumRelation>(entity =>
        {
            entity.ToTable("album_relations");
            entity.HasKey(e => new { e.CollectionId, e.MediaId });
            entity.Property(e => e.CollectionId).HasColumnName("collection");
            entity.Property(e => e.MediaId).HasColumnName("media");
            entity.Property(e => e.Index).HasColumnName("index");
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.ToTable("photos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.OwnerId).HasColumnName("owner");
            entity.Property(e => e.VirtualId).HasColumnName("virtual_id");
            entity.Property(e => e.Created).HasColumnName("created");
            entity.Property(e => e.Edited).HasColumnName("edited");
            entity.Property(e => e.Hash).HasColumnName("hash").HasMaxLength(128);
            entity.Property(e => e.Deleted).HasColumnName("deleted");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<ChandlerAclRelation>(entity =>
        {
            entity.ToTable("ChandlerACLRelations");
            entity.HasKey(e => new { e.UserId, e.GroupId });
            entity.Property(e => e.UserId).HasColumnName("user").HasColumnType("varchar(36)");
            entity.Property(e => e.GroupId).HasColumnName("group").HasColumnType("varchar(36)");
            entity.Property(e => e.Priority).HasColumnName("priority");
        });
    }
}
