using Lumina.Excel;

namespace Ocelot.Services.Data.Cache;

public interface ICache<in TKey, TModel>
{
    ICachePolicy CachePolicy { get; }

    bool TryGet(TKey key, out TModel value);

    TModel GetOrAdd(TKey key, Func<TModel> factory);

    void Set(TKey key, TModel value);

    void Remove(TKey key);
}

public sealed class CacheEntry<TModel>(TModel value, CacheEntryMetadata metadata)
{
    public TModel Value { get; } = value;

    public CacheEntryMetadata Metadata { get; } = metadata;
}

public class CacheEntryMetadata
{
    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public int HitCount;
}

public interface ICachePolicy
{
    bool IsExpired(CacheEntryMetadata metadata);

    TimeSpan? GetTimeToLive(object value);
}

public class NoExpirationPolicy : ICachePolicy
{
    public bool IsExpired(CacheEntryMetadata metadata)
    {
        return false;
    }

    public TimeSpan? GetTimeToLive(object value)
    {
        return null;
    }
}

public sealed class ExcelCache<TKey, TModel>() : GenericCache<TKey, TModel>(null)
    where TKey : notnull
    where TModel : struct, IExcelRow<TModel>;
