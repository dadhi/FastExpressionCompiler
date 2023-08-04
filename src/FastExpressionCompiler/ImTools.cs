using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices; // For [MethodImpl(AggressiveInlining)]

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace FastExpressionCompiler.ImTools;

using static FHashMap;

/// <summary>Configiration and the tools for the FHashMap map data structure</summary>
public static class FHashMap
{
    /// <summary>2^32 / phi for the Fibonacci hashing, where phi is the golden ratio ~1.61803</summary>
    public const uint GoldenRatio32 = 2654435769;

    internal const byte MinFreeCapacityShift = 3; // e.g. for the capacity 16: 16 >> 3 => 2, 12.5% of the free hash slots (it does not mean the entries free slot)
    internal const byte MinCapacityBits = 3; // 1 << 3 == 8

    /// <summary>Upper hash bits spent on storing the probes, e.g. 5 bits mean 31 probes max.</summary>
    public const byte MaxProbeBits = 5;
    internal const byte MaxProbeCount = (1 << MaxProbeBits) - 1;
    internal const byte ProbeCountShift = 32 - MaxProbeBits;
    internal const int HashAndIndexMask = ~(MaxProbeCount << ProbeCountShift);

    /// <summary>Creates the map with the <see cref="SingleArrayEntries{K, V, TEq}"/> storage</summary>
    [MethodImpl((MethodImplOptions)256)]
    public static FHashMap<K, V, TEq, SingleArrayEntries<K, V, TEq>> New<K, V, TEq>(byte capacityBitShift = 0)
        where TEq : struct, IEq<K> => new(capacityBitShift);

    // todo: @name a better name like NewMemEfficient or NewAddFocused?
    /// <summary>Creates the map with the <see cref="ChunkedArrayEntries{K, V, TEq}"/> storage</summary>
    [MethodImpl((MethodImplOptions)256)]
    public static FHashMap<K, V, TEq, ChunkedArrayEntries<K, V, TEq>> NewChunked<K, V, TEq>(byte capacityBitShift = 0)
        where TEq : struct, IEq<K> => new(capacityBitShift);

    /// <summary>Holds a single entry consisting of key and value. 
    /// Value may be set or changed but the key is set in stone (by construction).</summary>
    [DebuggerDisplay("{Key.ToString()}->{Value}")]
    public struct Entry<K, V>
    {
        /// <summary>The readonly key</summary>
        public readonly K Key;
        /// <summary>The mutable value</summary>
        public V Value;
        /// <summary>Construct with the key and default value</summary>
        public Entry(K key) => Key = key;
        /// <summary>Construct with the key and value</summary>
        public Entry(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>Converts the packed hashes and entries into the human readable info.
    /// This also used for the debugging view of the <paramref name="map"/> and by the Verify... methods in tests.</summary>
    public static DebugHashItem<K, V>[] Explain<K, V, TEq, TEntries>(this FHashMap<K, V, TEq, TEntries> map)
        where TEq : struct, IEq<K>
        where TEntries : struct, IEntries<K, V, TEq>
    {
        var hashes = map.PackedHashesAndIndexes;
        var capacity = 1 << map.CapacityBitShift;
        var indexMask = capacity - 1;

        var items = new DebugHashItem<K, V>[hashes.Length];
        for (var i = 0; i < hashes.Length; i++)
        {
            var h = hashes[i];
            if (h == 0)
                continue;

            var probe = h >>> ProbeCountShift;
            var hashIndex = ((capacity + i) - (probe - 1)) & indexMask;

            var hash = (h & HashAndIndexMask & ~indexMask) | hashIndex;
            var entryIndex = h & indexMask;

            ref var e = ref map.Entries.GetSurePresentEntryRef(entryIndex);
            var kh = default(TEq).GetHashCode(e.Key) & HashAndIndexMask;
            var heq = kh == hash;
            items[i] = new DebugHashItem<K, V> { Probe = probe, Hash = toB(hash), Index = entryIndex, HEq = heq };
        }
        return items;

        // binary reprsentation of the `int`
        static string toB(int x) => Convert.ToString(x, 2).PadLeft(32, '0');
    }

    /// <summary>Verifies that the hashes correspond to the keys stroed in the entries. May be called from the tests.</summary>
    public static void VerifyHashesAndKeysEq<K, V, TEq, TEntries>(this FHashMap<K, V, TEq, TEntries> map, Action<bool> assertEq)
        where TEq : struct, IEq<K>
        where TEntries : struct, IEntries<K, V, TEq>
    {
        var exp = map.Explain();
        foreach (var it in exp)
            if (!it.IsEmpty)
                assertEq(it.HEq);
    }

    /// <summary>Verifies that there is no duplicate keys stored in hashes -> entries. May be called from the tests.</summary>
    public static void VerifyNoDuplicateKeys<K, V, TEq, TEntries>(this FHashMap<K, V, TEq, TEntries> map, Action<K> assertKey)
        where TEq : struct, IEq<K>
        where TEntries : struct, IEntries<K, V, TEq>
    {
        // Verify the indexes do no contains duplicate keys
        var uniq = new Dictionary<K, int>(map.Count);
        var hashes = map.PackedHashesAndIndexes;
        var capacity = 1 << map.CapacityBitShift;
        var indexMask = capacity - 1;
        for (var i = 0; i < hashes.Length; i++)
        {
            var h = hashes[i];
            if (h == 0)
                continue;
            var key = map.Entries.GetSurePresentEntryRef(h & indexMask).Key;
            if (!uniq.TryGetValue(key, out _))
                uniq.Add(key, 1);
            else
                assertKey(key);
        }
    }

    /// <summary>Verifies that the probes are consistently increasing</summary>
    public static void VerifyProbesAreFitRobinHood<K, V, TEq, TEntries>(this FHashMap<K, V, TEq, TEntries> map, Action<string> reportFail)
        where TEq : struct, IEq<K>
        where TEntries : struct, IEntries<K, V, TEq>
    {
        var hashes = map.PackedHashesAndIndexes;
        var capacity = 1 << map.CapacityBitShift;
        var indexMask = capacity - 1;
        var prevProbes = -1;
        for (var i = 0; i < hashes.Length; i++)
        {
            var h = hashes[i];
            var probes = h >>> ProbeCountShift;
            if (prevProbes != -1 && probes - prevProbes > 1)
                reportFail($"Probes are not consequent: {prevProbes}, {probes} for {i}: p{probes}, {h & indexMask} -> {map.Entries.GetSurePresentEntryRef(h & indexMask).Key}");
            prevProbes = probes;
        }
    }

    /// <summary>Verifies that the map contains all passed keys. May be called from the tests.</summary>
    public static void VerifyContainAllKeys<K, V, TEq, TEntries>(this FHashMap<K, V, TEq, TEntries> map, IEnumerable<K> expectedKeys, Action<bool, K> assertContainKey)
        where TEq : struct, IEq<K>
        where TEntries : struct, IEntries<K, V, TEq>
    {
        foreach (var key in expectedKeys)
            assertContainKey(map.TryGetValue(key, out _), key);
    }

    [MethodImpl((MethodImplOptions)256)]
#if NET7_0_OR_GREATER
        internal static ref int GetHashRef(ref int start, int distance) => ref Unsafe.Add(ref start, distance);
#else
    internal static ref int GetHashRef(ref int[] start, int distance) => ref start[distance];
#endif

    [MethodImpl((MethodImplOptions)256)]
#if NET7_0_OR_GREATER
        internal static int GetHash(ref int start, int distance) => Unsafe.Add(ref start, distance);
#else
    internal static int GetHash(ref int[] start, int distance) => start[distance];
#endif

    /// <summary>Configures removed key tombstone, equality and hash function for the FHashMap</summary>
    public interface IEq<K>
    {
        /// <summary>Defines the value of the key indicating the removed entry</summary>
        K GetTombstone();

        /// <summary>Equals keys</summary>
        bool Equals(K x, K y);

        /// <summary>Calculates and returns the hash of the key</summary>
        int GetHashCode(K key);
    }

    /// <summary>Default comparer using the `object.GetHashCode` and `object.Equals` oveloads</summary>
    public struct DefaultEq<K> : IEq<K>
    {
        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public K GetTombstone() => default;

        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public bool Equals(K x, K y) => x.Equals(y);

        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public int GetHashCode(K key) => key.GetHashCode();
    }

    /// <summary>Uses the `object.GetHashCode` and `object.ReferenceEquals`</summary>
    public struct RefEq<K> : IEq<K> where K : class
    {
        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public K GetTombstone() => null;

        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public bool Equals(K x, K y) => ReferenceEquals(x, y);

        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public int GetHashCode(K key) => key.GetHashCode();
    }

    /// <summary>Uses the integer itself as hash code and `==` for equality</summary>
    public struct IntEq : IEq<int>
    {
        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public int GetTombstone() => int.MinValue;

        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public bool Equals(int x, int y) => x == y;

        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public int GetHashCode(int key) => key;
    }

    /// <summary>Uses Fibonacci hashing by multiplying the integer on the factor derived from the GoldenRatio</summary>
    public struct GoldenIntEq : IEq<int>
    {
        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public int GetTombstone() => int.MinValue;

        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public bool Equals(int x, int y) => x == y;

        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public int GetHashCode(int key) => (int)(key * GoldenRatio32) >>> MaxProbeBits;
    }

    /// <summary>Compares the types faster via `==` and gets the hash faster via `RuntimeHelpers.GetHashCode`</summary>
    public struct TypeEq : IEq<Type>
    {
        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public Type GetTombstone() => null;

        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public bool Equals(Type x, Type y) => x == y;

        /// <inheritdoc />
        [MethodImpl((MethodImplOptions)256)]
        public int GetHashCode(Type key) => RuntimeHelpers.GetHashCode(key);
    }

    // todo: @improve can we move the Entry into the type parameter to configure and possibly save the memory e.g. for the sets? 
    /// <summary>Abstraction to configure your own entries data structure. Check the derivitives for the examples</summary>
    public interface IEntries<K, V, TEq> where TEq : IEq<K>
    {
        /// <summary>Initializes the entries storage to the specified capacity via the number of <paramref name="capacityBitShift"/> bits in the capacity</summary>
        void Init(byte capacityBitShift);

        /// <summary>Returns the actual number of the stored entries</summary>
        int GetCount();

        /// <summary>Returns the reference to entry by its index, index should be valid</summary>
        ref Entry<K, V> GetSurePresentEntryRef(int index);

        /// <summary>Adds the key at the "end" of entriesc- so the order of addition is preserved.</summary>
        ref V AddKeyAndGetValueRef(K key);

        /// <summary>Marks the entry as removed `TEq.GetTombstone` or removes it and frees the memory if possible.</summary>
        void TombstoneOrRemoveSurePresentEntry(int index);
    }

    internal const int MinEntriesCapacity = 2;

    /// <summary>Stores the entries in a single dynamically reallocated array</summary>
    public struct SingleArrayEntries<K, V, TEq> : IEntries<K, V, TEq> where TEq : struct, IEq<K>
    {
        int _entryCount;
        internal Entry<K, V>[] _entries;

        /// <inheritdoc/>
        public void Init(byte capacityBitShift) =>
            _entries = new Entry<K, V>[1 << capacityBitShift];

        /// <inheritdoc/>
        [MethodImpl((MethodImplOptions)256)]
        public int GetCount() => _entryCount;

        /// <inheritdoc/>
        [MethodImpl((MethodImplOptions)256)]
        public ref Entry<K, V> GetSurePresentEntryRef(int index)
        {
#if NET7_0_OR_GREATER
                return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index);
#else
            return ref _entries[index];
#endif
        }

        /// <inheritdoc/>
        [MethodImpl((MethodImplOptions)256)]
        public ref V AddKeyAndGetValueRef(K key)
        {
            var index = _entryCount;
            if (index == 0)
                _entries = new Entry<K, V>[MinEntriesCapacity];
            else if (index == _entries.Length)
            {
#if DEBUG
                Debug.WriteLine($"[AllocateEntries] Resize entries: {index} -> {index << 1}");
#endif
                Array.Resize(ref _entries, index << 1);
            }
#if NET7_0_OR_GREATER
                ref var e = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index);
#else
            ref var e = ref _entries[index];
#endif
            ++_entryCount;
            e = new Entry<K, V>(key);
            return ref e.Value;
        }

        /// <summary>Tombstones the entry key</summary>
        [MethodImpl((MethodImplOptions)256)]
        public void TombstoneOrRemoveSurePresentEntry(int index)
        {
            GetSurePresentEntryRef(index) = new Entry<K, V>(default(TEq).GetTombstone());
            --_entryCount;
        }
    }

    // todo: @improve make it configurable
    /// <summary>The capacity of chunk in bits for <see cref="ChunkedArrayEntries{K, V, TEq}"/></summary>
    public const byte ChunkCapacityBitShift = 8; // 8 bits == 256
    internal const int ChunkCapacity = 1 << ChunkCapacityBitShift;
    internal const int ChunkCapacityMask = ChunkCapacity - 1;

    // todo: @perf research on the similar growable indexed collection with append-to-end semantics
    /// <summary>The array of array buckets, where bucket is the fixed size. 
    /// It enables adding the new bucket without for the new entries without reallocating the existing data.
    /// It may allow to drop the empty bucket as well, reclaiming the memory after remove.
    /// The structure is similar to Hashed Array Tree (HAT)</summary>
    public struct ChunkedArrayEntries<K, V, TEq> : IEntries<K, V, TEq> where TEq : struct, IEq<K>
    {
        int _entryCount;
        Entry<K, V>[][] _entries;
        /// <inheritdoc/>
        public void Init(byte capacityBitShift) =>
            _entries = new[] { new Entry<K, V>[(1 << capacityBitShift) & ChunkCapacityMask] };

        /// <inheritdoc/>
        [MethodImpl((MethodImplOptions)256)]
        public int GetCount() => _entryCount;

        /// <inheritdoc/>
        [MethodImpl((MethodImplOptions)256)]
        public ref Entry<K, V> GetSurePresentEntryRef(int index)
        {
#if NET7_0_OR_GREATER
                ref var entries = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index >>> ChunkCapacityBitShift);
                return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(entries), index & ChunkCapacityMask);
#else
            return ref _entries[index >>> ChunkCapacityBitShift][index & ChunkCapacityMask];
#endif
        }

        /// <inheritdoc/>
        public ref V AddKeyAndGetValueRef(K key)
        {
            var index = _entryCount++;
            var bucketIndex = index >>> ChunkCapacityBitShift;
            if (bucketIndex == 0) // small count of element fit into a single array
            {
                if (index != 0)
                {
#if NET7_0_OR_GREATER
                        ref var bucket = ref MemoryMarshal.GetArrayDataReference(_entries);
#else
                    ref var bucket = ref _entries[0];
#endif
                    if (index == bucket.Length)
                        Array.Resize(ref bucket, index << 1);

#if NET7_0_OR_GREATER
                        ref var e = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(bucket), index);
#else
                    ref var e = ref bucket[index];
#endif
                    e = new Entry<K, V>(key);
                    return ref e.Value;
                }
                {
                    var bucket = new Entry<K, V>[MinEntriesCapacity];
                    _entries = new[] { bucket };
#if NET7_0_OR_GREATER
                        ref var e = ref MemoryMarshal.GetArrayDataReference(bucket);
#else
                    ref var e = ref bucket[0];
#endif
                    e = new Entry<K, V>(key);
                    return ref e.Value;
                }
            }

            if ((index & ChunkCapacityMask) != 0)
            {
                ref var e = ref GetSurePresentEntryRef(index);
                e = new Entry<K, V>(key);
                return ref e.Value;
            }
            {
                if (bucketIndex == _entries.Length)
                    Array.Resize(ref _entries, bucketIndex << 1);
#if NET7_0_OR_GREATER
                    ref var bucket = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), bucketIndex);
#else
                ref var bucket = ref _entries[bucketIndex];
#endif
                bucket = new Entry<K, V>[ChunkCapacity];
#if NET7_0_OR_GREATER
                    ref var e = ref MemoryMarshal.GetArrayDataReference(bucket);
#else
                ref var e = ref bucket[0];
#endif
                e = new Entry<K, V>(key);
                return ref e.Value;
            }
        }

        /// <summary>Tombstones the entry key</summary>
        [MethodImpl((MethodImplOptions)256)]
        public void TombstoneOrRemoveSurePresentEntry(int index)
        {
            GetSurePresentEntryRef(index) = new Entry<K, V>(default(TEq).GetTombstone());
            --_entryCount;
            // todo: @perf we may try to free the chunk if it is empty
        }
    }

#if DEBUG
    internal struct ProbesTracker
    {
        internal int MaxProbes;
        internal int[] Probes;

        public ProbesTracker()
        {
            MaxProbes = 1;
            Probes = new int[1];
        }

        // will output something like
        // [Add] Probes abs max = 10, curr max = 6, all = [1: 180, 2: 103, 3: 59, 4: 23, 5: 3, 6: 1]; first 4 probes are 365 out of 369
        internal void DebugOutputProbes(string label)
        {
            Debug.Write($"[{label}] Probes abs max={MaxProbes}, curr max={Probes.Length}, all=[");
            var first4probes = 0;
            var allProbes = 0;
            for (var i = 0; i < Probes.Length; i++)
            {
                var p = Probes[i];
                Debug.Write($"{(i == 0 ? "" : ", ")}{i + 1}: {p}");
                if (i < 4)
                    first4probes += p;
                allProbes += p;
            }
            Debug.WriteLine($"]; first 4 probes are {first4probes} out of {allProbes}");
        }

        internal void DebugCollectAndOutputProbes(int probes, [CallerMemberName] string label = "")
        {
            if (probes > Probes.Length)
            {
                if (probes > MaxProbes)
                    MaxProbes = probes;
                Array.Resize(ref Probes, probes);
                Probes[probes - 1] = 1;
                DebugOutputProbes(label);
            }
            else
                ++Probes[probes - 1];
        }

        internal void DebugReCollectAndOutputProbes(int[] packedHashes, [CallerMemberName] string label = "")
        {
            var newProbes = new int[1];
            foreach (var h in packedHashes)
            {
                if (h == 0) continue;
                var p = h >>> ProbeCountShift;
                if (p > MaxProbes)
                    MaxProbes = p;
                if (p > newProbes.Length)
                    Array.Resize(ref newProbes, p);
                ++newProbes[p - 1];
            }
            Probes = newProbes;
            DebugOutputProbes(label);
        }

        internal void RemoveProbes(int probes)
        {
            ref var p = ref Probes[probes - 1];
            --p;
            if (p == 0 && probes == Probes.Length)
            {
                Array.Resize(ref Probes, probes - 1);
                if (MaxProbes == probes)
                    --MaxProbes;
            }
        }
    }
#endif
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public struct DebugHashItem<K, V>
    {
        public int Probe;
        public string Hash;
        public int Index;
        public bool IsEmpty => Probe == 0;
        public bool HEq;

        public override string ToString() => IsEmpty ? "empty" : $"{Probe}|{Hash}|{Index}";
    }

    public class DebugProxy<K, V, TEq, TEntries>
        where TEq : struct, IEq<K>
        where TEntries : struct, IEntries<K, V, TEq>
    {
        private readonly FHashMap<K, V, TEq, TEntries> _map;
        internal DebugProxy(FHashMap<K, V, TEq, TEntries> map) => _map = map;
        public DebugHashItem<K, V>[] PackedHashes => _map.Explain();
        public TEntries Entries => _map.Entries;
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

// todo: @improve ? how/where to add SIMD to improve CPU utilization but not losing perf for smaller sizes
/// <summary>
/// Fast and less-allocating hash map without thread safety nets. Please measure it in your own use case before use.
/// It is configurable in regard of hash calculation/equality via <typeparamref name="TEq"/> and 
/// in regard of key-value storage via <typeparamref name="TEntries"/>
/// 
/// Details:
/// - Implemented as a struct so that the empty/default map does not allocate on heap
/// - Hashes and key-values are the separate collections enabling better cash locality and faster performance (data-oriented design)
/// - No SIMD for now to avoid complexity and costs for the smaller maps, so the map is more fit for the smaller sizes.
/// - Provides the "stable" enumeration of the entries in the added order
/// - The TryRemove method removes the hash but replaces the key-value entry with the tombstone key and the default value.
/// For instance, for the `RefEq` the tombstone is <see langword="null"/>. You may redefine it in the `IEq{K}.GetTombstone()` implementation.
/// 
/// </summary>
[DebuggerTypeProxy(typeof(DebugProxy<,,,>))]
[DebuggerDisplay("Count={Count}")]
public struct FHashMap<K, V, TEq, TEntries> : IReadOnlyCollection<Entry<K, V>>
    where TEq : struct, IEq<K>
    where TEntries : struct, IEntries<K, V, TEq>
{
#if DEBUG
    ProbesTracker _dbg = new();
#endif
    private byte _capacityBitShift;

    // The _packedHashesAndIndexes elements are of `Int32` with the bits split as following:
    // 00010|000...110|01101
    // |     |         |- The index into the _entries structure, 0-based. The index bit count (indexMask) is the hashes capacity - 1.
    // |     |         | This part of the erased hash is used to get the ideal index into the hashes array, so later this part of hash may be restored from the hash index and its probes.
    // |     |- The remaining middle bits of the original hash
    // |- 5 (MaxProbeBits) high bits of the Probe count, with the minimal value of b00001 indicating the non-empty slot.
    private int[] _packedHashesAndIndexes;
    private readonly TEntries _entries;

    /// <summary>Capacity bits</summary>
    public int CapacityBitShift => _capacityBitShift;

    /// <summary>Access to the hashes and indexes</summary>
    public int[] PackedHashesAndIndexes => _packedHashesAndIndexes;

    /// <summary>Number of entries in the map</summary>
    public int Count => _entries.GetCount();

    /// <summary>Access to the key-value entries</summary>
    public TEntries Entries => _entries;

    /// <summary>Capacity calculates as `1 leftShift capacityBitShift`</summary>
    public FHashMap(byte capacityBitShift)
    {
        _capacityBitShift = capacityBitShift;

        // the overflow tail to the hashes is the size of log2N where N==capacityBitShift, 
        // it is probably fine to have the check for the overlow of capacity because it will be mispredicted only once at the end of loop (it even rarely for the lookup)
        _packedHashesAndIndexes = new int[1 << capacityBitShift];
        _entries = default;
        _entries.Init(capacityBitShift);
    }

    /// <summary>Lookup for the key and get the associated value if the key is found</summary>
    [MethodImpl((MethodImplOptions)256)]
    public bool TryGetValue(K key, out V value)
    {
        if (_packedHashesAndIndexes == null)
        {
            value = default;
            return false;
        }

        var hash = default(TEq).GetHashCode(key);

        var indexMask = (1 << _capacityBitShift) - 1;
        var hashMiddleMask = HashAndIndexMask & ~indexMask;
        var hashMiddle = hash & hashMiddleMask;
        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
            ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif

        var h = GetHash(ref hashesAndIndexes, hashIndex);

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        var probes = 1;
        while ((h >>> ProbeCountShift) >= probes)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if (((h >>> ProbeCountShift) == probes) & ((h & hashMiddleMask) == hashMiddle))
            {
                ref var e = ref _entries.GetSurePresentEntryRef(h & indexMask);
                if (default(TEq).Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }

            h = GetHash(ref hashesAndIndexes, ++hashIndex & indexMask);
            ++probes;
        }

        value = default;
        return false;
    }

    /// <summary>Lookup for the key and get the associated value or the default value if the key is not found</summary>
    [MethodImpl((MethodImplOptions)256)]
    public V GetValueOrDefault(K key, V defaultValue = default) =>
        TryGetValue(key, out var value) ? value : defaultValue;

    /// <summary>Gets the reference to the existing value of the provided key, or the default value to set for the newly added key.</summary>
    [MethodImpl((MethodImplOptions)256)]
    public ref V GetOrAddValueRef(K key)
    {
        var hash = default(TEq).GetHashCode(key);

        var indexMask = (1 << _capacityBitShift) - 1;
        var entryCount = _entries.GetCount();

        // if the free space is less than 1/8 of capacity (12.5%) then Resize
        if (indexMask - entryCount <= (indexMask >>> MinFreeCapacityShift))
            indexMask = ResizeHashes(indexMask);

        var hashMiddleMask = HashAndIndexMask & ~indexMask;
        var hashMiddle = hash & hashMiddleMask;
        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
            ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif
        ref var h = ref GetHashRef(ref hashesAndIndexes, hashIndex);

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        var probes = 1;
        while ((h >>> ProbeCountShift) >= probes)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if (((h >>> ProbeCountShift) == probes) & ((h & hashMiddleMask) == hashMiddle))
            {
                ref var e = ref _entries.GetSurePresentEntryRef(h & indexMask);
#if DEBUG
                Debug.WriteLine($"[Add] Probes and Hash parts are matching: probes {probes}, new key:`{key}` with matched hash of key:`{e.Key}`");
#endif
                if (default(TEq).Equals(e.Key, key))
                    return ref e.Value;
            }
            h = ref GetHashRef(ref hashesAndIndexes, ++hashIndex & indexMask);
            ++probes;
        }

        // 3. We did not find the hash and therefore the key, so insert the new entry
        var hRobinHooded = h;
        h = (probes << ProbeCountShift) | hashMiddle | entryCount;
#if DEBUG
        _dbg.DebugCollectAndOutputProbes(probes, "Add");
#endif
        // 4. If the robin hooded hash is empty then we stop
        // 5. Otherwise we steal the slot with the smaller probes
        probes = hRobinHooded >>> ProbeCountShift;
        while (hRobinHooded != 0)
        {
            h = ref GetHashRef(ref hashesAndIndexes, ++hashIndex & indexMask);
            if ((h >>> ProbeCountShift) < ++probes)
            {
#if DEBUG
                if (h != 0)
                    _dbg.RemoveProbes(h >>> ProbeCountShift);
                _dbg.DebugCollectAndOutputProbes(probes, "Add-RH");
#endif
                var tmp = h;
                h = (probes << ProbeCountShift) | (hRobinHooded & HashAndIndexMask);
                hRobinHooded = tmp;
                probes = hRobinHooded >>> ProbeCountShift;
            }
        }
        return ref _entries.AddKeyAndGetValueRef(key);
    }

    /// <summary>Same as `GetOrAddValueRef` but provides the value to add or override for the existing key</summary>
    [MethodImpl((MethodImplOptions)256)]
    public void AddOrUpdate(K key, in V value) =>
        GetOrAddValueRef(key) = value;

    /// <summary>Removes the hash and entry of the provided key or returns <see langword="false"/></summary>
    [MethodImpl((MethodImplOptions)256)]
    public bool TryRemove(K key)
    {
        var hash = default(TEq).GetHashCode(key);

        var indexMask = (1 << _capacityBitShift) - 1;
        var hashMiddleMask = ~indexMask & HashAndIndexMask;
        var hashMiddle = hash & hashMiddleMask;
        var hashIndex = hash & indexMask;

#if NET7_0_OR_GREATER
            ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif
        ref var h = ref GetHashRef(ref hashesAndIndexes, hashIndex);

        var removed = false;

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        var probes = 1;
        while ((h >>> ProbeCountShift) >= probes)
        {
            // 2. For the equal probes check for equality the hash middle part, and update the entry if the keys are equal too 
            if (((h >>> ProbeCountShift) == probes) & ((h & hashMiddleMask) == hashMiddle))
            {
                ref var e = ref _entries.GetSurePresentEntryRef(h & indexMask);
                if (default(TEq).Equals(e.Key, key))
                {
                    _entries.TombstoneOrRemoveSurePresentEntry(h & indexMask);
                    removed = true;
                    h = 0;
#if DEBUG
                    _dbg.RemoveProbes(probes);
#endif
                    break;
                }
            }
            h = ref GetHashRef(ref hashesAndIndexes, ++hashIndex & indexMask);
            ++probes;
        }

        if (!removed)
            return false;

        ref var emptied = ref h;
        h = ref GetHashRef(ref hashesAndIndexes, ++hashIndex & indexMask);

        // move the next hash into the emptied slot until the next hash is empty or ideally positioned (hash is 0 or probe is 1)
        while ((h >>> ProbeCountShift) > 1)
        {
            emptied = (((h >>> ProbeCountShift) - 1) << ProbeCountShift) | (h & HashAndIndexMask); // decrease the probe count by one cause we moving the hash closer to the ideal index
            h = 0;

            emptied = ref h;
            h = ref GetHashRef(ref hashesAndIndexes, ++hashIndex & indexMask);
        }
        return true;
    }

    internal int ResizeHashes(int indexMask)
    {
        if (indexMask == 0)
        {
            _capacityBitShift = MinCapacityBits;
            _packedHashesAndIndexes = new int[1 << MinCapacityBits];
#if DEBUG
            Debug.WriteLine($"[ResizeHashes] new empty hashes {1} -> {_packedHashesAndIndexes.Length}");
#endif
            return (1 << MinCapacityBits) - 1;
        }

        var oldCapacity = indexMask + 1;
        var newHashAndIndexMask = HashAndIndexMask & ~oldCapacity;
        var newIndexMask = (indexMask << 1) | 1;

        var newHashesAndIndexes = new int[oldCapacity << 1];

#if NET7_0_OR_GREATER
            ref var newHashes = ref MemoryMarshal.GetArrayDataReference(newHashesAndIndexes);
            ref var oldHashes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
            var oldHash = oldHashes;
#else
        var newHashes = newHashesAndIndexes;
        var oldHashes = _packedHashesAndIndexes;
        var oldHash = oldHashes[0];
#endif
        // Overflow segment is wrapped-around hashes and! the hashes at the beginning robin hooded by the wrapped-around hashes
        var i = 0;
        while ((oldHash >>> ProbeCountShift) > 1)
            oldHash = GetHash(ref oldHashes, ++i);

        var oldCapacityWithOverflowSegment = i + oldCapacity;
        while (true)
        {
            if (oldHash != 0)
            {
                // get the new hash index from the old one with the next bit equal to the `oldCapacity`
                var indexWithNextBit = (oldHash & oldCapacity) | (((i + 1) - (oldHash >>> ProbeCountShift)) & indexMask);

                // no need for robinhooding because we already did it for the old hashes and now just sparcing the hashes into the new array which are already in order
                var probes = 1;
                ref var newHash = ref GetHashRef(ref newHashes, indexWithNextBit);
                while (newHash != 0)
                {
                    newHash = ref GetHashRef(ref newHashes, ++indexWithNextBit & newIndexMask);
                    ++probes;
                }
                newHash = (probes << ProbeCountShift) | (oldHash & newHashAndIndexMask);
            }
            if (++i >= oldCapacityWithOverflowSegment)
                break;

            oldHash = GetHash(ref oldHashes, i & indexMask);
        }
#if DEBUG
        Debug.WriteLine($"[ResizeHashes] {oldCapacity} -> {newHashesAndIndexes.Length}");
        _dbg.DebugReCollectAndOutputProbes(newHashesAndIndexes);
#endif
        ++_capacityBitShift;
        _packedHashesAndIndexes = newHashesAndIndexes;
        return newIndexMask;
    }

    /// <inheritdoc />
    [MethodImpl((MethodImplOptions)256)]
    public Enumerator GetEnumerator() => new(_entries); // prevents the boxing of the enumerator struct

    /// <inheritdoc />
    IEnumerator<Entry<K, V>> IEnumerable<Entry<K, V>>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Enumerator of the entries in the order of their addition to the map</summary>
    public struct Enumerator : IEnumerator<Entry<K, V>>
    {
        private int _index;
        private Entry<K, V> _current;
        private readonly TEntries _entries;
        private int _countIncludingRemoved;
        internal Enumerator(TEntries entries)
        {
            _index = 0;
            _current = default;
            _entries = entries;
            _countIncludingRemoved = entries.GetCount();
        }

        /// <summary>Move to the next entry in the order of their addition to the map</summary>
        [MethodImpl((MethodImplOptions)256)]
        public bool MoveNext()
        {
        skipRemoved:
            if (_index < _countIncludingRemoved)
            {
                ref var e = ref _entries.GetSurePresentEntryRef(_index++);
                if (!default(TEq).Equals(e.Key, default(TEq).GetTombstone()))
                {
                    _current = e;
                    return true;
                }
                ++_countIncludingRemoved;
                goto skipRemoved;
            }
            _current = default;
            return false;
        }

        /// <inheritdoc />
        public Entry<K, V> Current => _current;
        object IEnumerator.Current => _current;

        void IEnumerator.Reset()
        {
            _index = 0;
            _countIncludingRemoved = _entries.GetCount();
            _current = default;
        }

        /// <inheritdoc />
        public void Dispose() { }
    }
}
