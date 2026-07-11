using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Ovk.Net.Core.Models;
using Ovk.Net.Infrastructure.Data;

namespace Ovk.Net.Web.Services;

public interface IAvatarStorage
{
    Task<Photo?> GetAvatarAsync(ulong profileId, CancellationToken cancellationToken = default);
    Task<Photo> SaveAvatarAsync(ulong profileId, Stream image, CancellationToken cancellationToken = default);
    Task DeleteAvatarAsync(ulong profileId, CancellationToken cancellationToken = default);
    string GetPath(string hash);
}

public sealed class AvatarStorage : IAvatarStorage
{
    private const byte AvatarAlbumType = 16;
    private readonly OvkDbContext _db;
    private readonly string _storageRoot;

    public AvatarStorage(OvkDbContext db, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _db = db;
        var configuredPath = configuration["OpenVk:StoragePath"] ?? "../../php/storage";
        _storageRoot = Path.GetFullPath(configuredPath, environment.ContentRootPath);
    }

    public async Task<Photo?> GetAvatarAsync(ulong profileId, CancellationToken cancellationToken = default)
    {
        return await (
            from album in _db.Albums.AsNoTracking()
            join relation in _db.AlbumRelations.AsNoTracking() on album.Id equals relation.CollectionId
            join photo in _db.Photos.AsNoTracking() on relation.MediaId equals photo.Id
            where album.OwnerId == (long)profileId && album.SpecialType == AvatarAlbumType && !album.Deleted && !photo.Deleted
            orderby relation.Index descending, photo.Id descending
            select photo
        ).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Photo> SaveAvatarAsync(ulong profileId, Stream image, CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await image.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var hash = Convert.ToHexString(SHA512.HashData(bytes)).ToLowerInvariant();
        var path = GetPath(hash);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var album = await _db.Albums.FirstOrDefaultAsync(
            x => x.OwnerId == (long)profileId && x.SpecialType == AvatarAlbumType && !x.Deleted,
            cancellationToken);

        if (album is null)
        {
            album = new Album
            {
                OwnerId = (long)profileId,
                Name = "[!!! internal album]",
                SpecialType = AvatarAlbumType,
                Created = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            _db.Albums.Add(album);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var nextVirtualId = (await _db.Photos
            .Where(x => x.OwnerId == (long)profileId)
            .MaxAsync(x => (long?)x.VirtualId, cancellationToken) ?? 0) + 1;
        var photo = new Photo
        {
            OwnerId = (long)profileId,
            VirtualId = nextVirtualId,
            Created = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Hash = hash
        };
        _db.Photos.Add(photo);
        await _db.SaveChangesAsync(cancellationToken);

        var nextIndex = (await _db.AlbumRelations
            .Where(x => x.CollectionId == album.Id)
            .MaxAsync(x => (ulong?)x.Index, cancellationToken) ?? 0) + 1;
        _db.AlbumRelations.Add(new AlbumRelation
        {
            CollectionId = album.Id,
            MediaId = photo.Id,
            Index = nextIndex
        });
        album.CoverPhotoId = photo.Id;
        album.Edited = photo.Created;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return photo;
    }

    public async Task DeleteAvatarAsync(ulong profileId, CancellationToken cancellationToken = default)
    {
        var avatar = await GetAvatarAsync(profileId, cancellationToken);
        if (avatar is null) return;

        var tracked = await _db.Photos.FindAsync(new object[] { avatar.Id }, cancellationToken);
        if (tracked is null) return;
        tracked.Deleted = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public string GetPath(string hash) => Path.Combine(_storageRoot, hash[..2], $"{hash}.jpeg");
}
