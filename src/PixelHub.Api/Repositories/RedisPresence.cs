using StackExchange.Redis;

namespace PixelHub.Api.Repositories;

public class RedisPresence
{
    private const string Key = "online:joueurs";

    // Fenêtre « en ligne » : 60 s selon le sujet. Passer à 10 s pour tester sans attendre une minute.
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(60);

    private readonly IDatabase _db;

    public RedisPresence(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    // Signe de vie : ZADD avec le timestamp comme score (rafraîchit si le pseudo existe déjà).
    public async Task TouchAsync(string pseudo)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _db.SortedSetAddAsync(Key, pseudo, now);
    }

    // Purge les scores trop vieux puis compte : ZREMRANGEBYSCORE + ZCARD.
    public async Task<long> CountOnlineAsync()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var cutoff = now - (long)Window.TotalSeconds;
        await _db.SortedSetRemoveRangeByScoreAsync(Key, double.NegativeInfinity, cutoff);
        return await _db.SortedSetLengthAsync(Key);
    }
}
