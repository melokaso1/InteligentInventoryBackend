using Microsoft.Extensions.Caching.Memory;

namespace Api.Caching;

public static class EndpointCache
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(45);

    public static async Task<T> GetOrCreateAsync<T>(
        IMemoryCache cache,
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default,
        TimeSpan? ttl = null)
    {
        return await cache.GetOrCreateAsync(
            key,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl;
                return await factory(cancellationToken);
            }) ?? await factory(cancellationToken);
    }
}
