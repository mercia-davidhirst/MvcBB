namespace MvcBB.ModKit
{
    /// <summary>
    /// Per-request memoised bulk lookup for slot view components. Register it
    /// (or a class wrapping it) as Scoped. The first <see cref="GetAsync"/> on a
    /// page loads every key in the page set at once; later calls on the same
    /// page are served from memory. On a thread page where 12 members wrote 300
    /// posts, that's one load of 12 keys instead of 300 loads.
    /// </summary>
    /// <remarks>
    /// Razor renders sequentially, so concurrent first calls aren't expected,
    /// but they're still safe: overlapping calls share one in-flight load.
    /// Keys the loader doesn't return are remembered as "no value", so they
    /// aren't asked for again.
    /// </remarks>
    public sealed class SlotBatch<TKey, TValue> where TKey : notnull
    {
        private readonly Func<IReadOnlyCollection<TKey>, CancellationToken, Task<IReadOnlyDictionary<TKey, TValue>>> _loadMany;
        private readonly Dictionary<TKey, Task<IReadOnlyDictionary<TKey, TValue>>> _loads = new();
        private readonly object _lock = new();

        /// <param name="loadMany">
        /// Loads values for a set of keys in one go, returning only the keys that
        /// have a value. It should split very large key sets into chunks itself
        /// if its backing API caps how many keys one call may ask for.
        /// </param>
        public SlotBatch(Func<IReadOnlyCollection<TKey>, CancellationToken, Task<IReadOnlyDictionary<TKey, TValue>>> loadMany)
        {
            _loadMany = loadMany ?? throw new ArgumentNullException(nameof(loadMany));
        }

        /// <param name="key">The key wanted now.</param>
        /// <param name="pageKeys">
        /// Normally <c>context.Page.UserIds</c> or <c>context.Page.PostIds</c>.
        /// Keys already loaded (or loading) are skipped. If <paramref name="key"/>
        /// isn't in the set, it's loaded too.
        /// </param>
        public async Task<TValue?> GetAsync(TKey key, IReadOnlyCollection<TKey>? pageKeys, CancellationToken ct = default)
        {
            Task<IReadOnlyDictionary<TKey, TValue>> load;
            lock (_lock)
            {
                if (!_loads.TryGetValue(key, out load!))
                {
                    var missing = new HashSet<TKey> { key };
                    if (pageKeys != null)
                    {
                        foreach (var pageKey in pageKeys)
                        {
                            if (!_loads.ContainsKey(pageKey))
                                missing.Add(pageKey);
                        }
                    }

                    load = _loadMany(missing, ct);
                    foreach (var missingKey in missing)
                        _loads[missingKey] = load;
                }
            }

            var values = await load.ConfigureAwait(false);
            return values.TryGetValue(key, out var value) ? value : default;
        }
    }
}
