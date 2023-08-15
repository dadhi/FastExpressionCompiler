#define BENCHMARK

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.ImTools;
#else
namespace FastExpressionCompiler.ImTools;
#endif

using static FHashMap;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class Stack4
{
    internal sealed class HeapItems<TItem>
    {
        public TItem[] Items; // todo: @perf use MemoryMarshal.GetArrayDataReference and Unsafe.Add fo array navigation to avoid bounds check
        public HeapItems(int capacity) =>
            Items = new TItem[capacity];

        [MethodImpl((MethodImplOptions)256)]
        public void Add(int index, in TItem item)
        {
            if (index >= Items.Length)
                Array.Resize(ref Items, Items.Length << 1);
            Items[index] = item;
        }

        // todo: @improve Add explicit Remove method and think if count is decreased twice so the array may be resized
        [MethodImpl((MethodImplOptions)256)]
        public void AddDefault(int index)
        {
            if (index >= Items.Length)
                Array.Resize(ref Items, Items.Length << 1);
        }

        [MethodImpl((MethodImplOptions)256)]
        public ref TItem AddDefaultAndGetRef(int index)
        {
            if (index >= Items.Length)
                Array.Resize(ref Items, Items.Length << 1);
            return ref Items[index];
        }
    }

    [MethodImpl((MethodImplOptions)256)]
    public static ref TItem GetSurePresentItemRef<TItem>(this ref Stack4<TItem> source, int index)
    {
        Debug.Assert(source.Count != 0);
        Debug.Assert(index < source.Count);
        switch (index)
        {
            case 0: return ref source._it0;
            case 1: return ref source._it1;
            case 2: return ref source._it2;
            case 3: return ref source._it3;
            default:
                Debug.Assert(source._deepItems != null, $"Expecting deeper items are already existing on stack at index: {index}");
                return ref source._deepItems.Items[index - 4];
        }
    }

    [MethodImpl((MethodImplOptions)256)]
    public static ref TItem PeekLastSurePresentItem<TItem>(this ref Stack4<TItem> source) =>
        ref source.GetSurePresentItemRef(source._count - 1);

    [MethodImpl((MethodImplOptions)256)]
    public static ref TItem NotFound<TItem>(this ref Stack4<TItem> _) => ref Stack4<TItem>.Tombstone;

    [MethodImpl((MethodImplOptions)256)]
    public static ref TItem PushLastDefaultAndGetRef<TItem>(this ref Stack4<TItem> source)
    {
        var index = source._count++;
        switch (index)
        {
            case 0: return ref source._it0;
            case 1: return ref source._it1;
            case 2: return ref source._it2;
            case 3: return ref source._it3;
            default:
                if (source._deepItems != null)
                    return ref source._deepItems.AddDefaultAndGetRef(index - 4);
                source._deepItems = new HeapItems<TItem>(4);
                return ref source._deepItems.Items[0];
        }
    }
}

public struct Stack4<TItem>
{   // todo: @wip what if someone stores somthing in it, it would be a memory leak, but isn't it the same as using `out var` in the returning`false` Try...methods?
    public static TItem Tombstone; // return the ref to Tombstone when nothing found

    internal int _count;
    internal TItem _it0, _it1, _it2, _it3;
    internal Stack4.HeapItems<TItem> _deepItems;

    public int Count
    {
        [MethodImpl((MethodImplOptions)256)]
        get => _count;
    }

    [MethodImpl((MethodImplOptions)256)]
    public void PutOrAdd(int index, in TItem item)
    {
        switch (index)
        {
            case 0: _it0 = item; break;
            case 1: _it1 = item; break;
            case 2: _it2 = item; break;
            case 3: _it3 = item; break;
            default:
                if (_deepItems != null)
                    _deepItems.Add(index - 4, in item);
                else
                {
                    _deepItems = new Stack4.HeapItems<TItem>(4);
                    _deepItems.Items[index - 4] = item;
                }
                break;
        }
    }

    [MethodImpl((MethodImplOptions)256)]
    public void PushLast(in TItem item) =>
        PutOrAdd(_count++, item);

    [MethodImpl((MethodImplOptions)256)]
    public void PushLastDefault()
    {
        if (++_count >= 4)
        {
            if (_deepItems == null)
                _deepItems = new Stack4.HeapItems<TItem>(4);
            else
                _deepItems.AddDefault(_count - 4);
        }
    }

    [MethodImpl((MethodImplOptions)256)]
    public void PopLastSurePresentItem()
    {
        Debug.Assert(Count != 0);
        var index = --_count;
        switch (index)
        {
            case 0: _it0 = default; break;
            case 1: _it1 = default; break;
            case 2: _it2 = default; break;
            case 3: _it3 = default; break;
            default:
                Debug.Assert(_deepItems != null, $"Expecting a deeper parent stack created before accessing it here at level {index}");
                _deepItems.Items[index - 4] = default;
                break;
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

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

    /// <summary>Holds a single entry consisting of key and value. 
    /// Value may be set or changed but the key is set in stone (by construction).</summary>
    [DebuggerDisplay("{Key?.ToString()}->{Value}")]
    public struct Entry<K, V>
    {
        /// <summary>The readonly key</summary>
        public K Key;
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

    /// <summary>Converts the packed hashes and entries into the human readable info for debugging visualization</summary>
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
        public int GetHashCode(K key) => RuntimeHelpers.GetHashCode(key);
    }

    // todo: @improve can we move the Entry into the type parameter to configure and possibly save the memory e.g. for the sets? 
    /// <summary>Abstraction to configure your own entries data structure. Check the derivitives for the examples</summary>
    public interface IEntries<K, V, TEq> where TEq : IEq<K>
    {
        /// <summary>Initializes the entries storage to the specified capacity via the number of <paramref name="capacityBitShift"/> bits in the capacity</summary>
        void Init(byte capacityBitShift);

        /// <summary>Returns the reference to entry by its index, index should map to the present/non-removed entry</summary>
        ref Entry<K, V> GetSurePresentEntryRef(int index);

        /// <summary>Adds the key at the "end" of entriesc- so the order of addition is preserved.</summary>
        ref V AddKeyAndGetValueRef(K key, int count);

        /// <summary>@wip</summary>
        ref V AddWithInitial4(K key4, in Entry<K, V> e0, in Entry<K, V> e1, in Entry<K, V> e2, in Entry<K, V> e3);
    }

    internal const int MinEntriesCapacity = 2;

    /// <summary>Stores the entries in a single dynamically reallocated array</summary>
    public struct SingleArrayEntries<K, V, TEq> : IEntries<K, V, TEq> where TEq : struct, IEq<K>
    {
        internal Entry<K, V>[] _entries;

        /// <inheritdoc/>
        public void Init(byte capacityBitShift) =>
            _entries = new Entry<K, V>[1 << capacityBitShift];

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
        public ref V AddKeyAndGetValueRef(K key, int count)
        {
            if (count == _entries.Length)
            {
#if DEBUG
                Debug.WriteLine($"[AllocateEntries] Resize entries: {count} -> {count << 1}");
#endif
                Array.Resize(ref _entries, count << 1);
            }
#if NET7_0_OR_GREATER
            ref var e = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), count);
#else
            ref var e = ref _entries[count];
#endif
            e = new Entry<K, V>(key);
            return ref e.Value;
        }

        /// <inheritdoc/>
        public ref V AddWithInitial4(K key4, in Entry<K, V> e0, in Entry<K, V> e1, in Entry<K, V> e2, in Entry<K, V> e3)
        {
            _entries = new Entry<K, V>[8];
            _entries[0] = e0;
            _entries[1] = e1;
            _entries[2] = e2;
            _entries[3] = e3;
            ref var e4 = ref _entries[4];
            e4.Key = key4;
            return ref e4.Value;
        }
    }

    /// <summary>Finds the stored value by key. If found returns ref to the value it can be modified in place.</summary>
    [MethodImpl((MethodImplOptions)256)]
    public static ref V TryGetValueRef<K, V, TEq, TEntries>(this ref FHashMap<K, V, TEq, TEntries> map, K key, out bool found)
        where TEq : struct, IEq<K>
        where TEntries : struct, IEntries<K, V, TEq>
    {
        if (map._count > 4)
            return ref map.TryGetValueRefByHash(key, out found);
        switch (map._count)
        {
            case 1:
                if (found = default(TEq).Equals(key, map._e0.Key)) return ref map._e0.Value;
                break;
            case 2:
                if (found = default(TEq).Equals(key, map._e0.Key)) return ref map._e0.Value;
                if (found = default(TEq).Equals(key, map._e1.Key)) return ref map._e1.Value;
                break;
            case 3:
                if (found = default(TEq).Equals(key, map._e0.Key)) return ref map._e0.Value;
                if (found = default(TEq).Equals(key, map._e1.Key)) return ref map._e1.Value;
                if (found = default(TEq).Equals(key, map._e2.Key)) return ref map._e2.Value;
                break;
            case 4:
                if (found = default(TEq).Equals(key, map._e0.Key)) return ref map._e0.Value;
                if (found = default(TEq).Equals(key, map._e1.Key)) return ref map._e1.Value;
                if (found = default(TEq).Equals(key, map._e2.Key)) return ref map._e2.Value;
                if (found = default(TEq).Equals(key, map._e3.Key)) return ref map._e3.Value;
                break;
        }
        found = false;
        return ref FHashMap<K, V, TEq, TEntries>._missing.Value;
    }

    /// <summary>Gets the reference to the existing value of the provided key, or the default value to set for the newly added key.</summary>
    [MethodImpl((MethodImplOptions)256)]
    public static ref V GetOrAddValueRef<K, V, TEq, TEntries>(this ref FHashMap<K, V, TEq, TEntries> map, K key)
        where TEq : struct, IEq<K>
        where TEntries : struct, IEntries<K, V, TEq>
    {
        if (map._count > 4)
            return ref map.GetOrAddValueRefByHash(key);
        switch (map._count)
        {
            case 0:
                ++map._count;
                map._e0.Key = key;
                return ref map._e0.Value;

            case 1:
                if (default(TEq).Equals(key, map._e0.Key)) return ref map._e0.Value;
                ++map._count;
                map._e1.Key = key;
                return ref map._e1.Value;

            case 2:
                if (default(TEq).Equals(key, map._e0.Key)) return ref map._e0.Value;
                if (default(TEq).Equals(key, map._e1.Key)) return ref map._e1.Value;
                ++map._count;
                map._e2.Key = key;
                return ref map._e2.Value;

            case 3:
                if (default(TEq).Equals(key, map._e0.Key)) return ref map._e0.Value;
                if (default(TEq).Equals(key, map._e1.Key)) return ref map._e1.Value;
                if (default(TEq).Equals(key, map._e2.Key)) return ref map._e2.Value;
                ++map._count;
                map._e3.Key = key;
                return ref map._e3.Value;

            default:
                if (default(TEq).Equals(key, map._e0.Key)) return ref map._e0.Value;
                if (default(TEq).Equals(key, map._e1.Key)) return ref map._e1.Value;
                if (default(TEq).Equals(key, map._e2.Key)) return ref map._e2.Value;
                if (default(TEq).Equals(key, map._e3.Key)) return ref map._e3.Value;

                map._capacityBitShift = 4;
                map._packedHashesAndIndexes = new int[1 << map._capacityBitShift];

                map.AddInitialToHash(map._e0.Key, 0);
                map.AddInitialToHash(map._e1.Key, 1);
                map.AddInitialToHash(map._e2.Key, 2);
                map.AddInitialToHash(map._e3.Key, 3);
                map.AddInitialToHash(key, 4);

                ++map._count;
                ref var v5 = ref map._entries.AddWithInitial4(key, in map._e0, in map._e1, in map._e2, in map._e3);

                // clear the fields when they being moved to entries
                map._e0 = default;
                map._e1 = default;
                map._e2 = default;
                map._e3 = default;

                return ref v5;
        }
    }

    ///<summary>Get the value ref by the entry index. Also the index corresponds to entry adding order.
    ///Improtant: it does not checks the index bounds, so you need to check that the index is from 0 to map.Count-1</summary>
    [MethodImpl((MethodImplOptions)256)]
    public static ref Entry<K, V> GetSurePresentEntryRef<K, V, TEq, TEntries>(this ref FHashMap<K, V, TEq, TEntries> map, int index)
        where TEq : struct, IEq<K>
        where TEntries : struct, IEntries<K, V, TEq>
    {
        Debug.Assert(index >= 0);
        Debug.Assert(index < map._count);
        if (map._count > 4)
            return ref map._entries.GetSurePresentEntryRef(index);
        switch (index)
        {
            case 0: return ref map._e0;
            case 1: return ref map._e1;
            case 2: return ref map._e2;
            case 4: return ref map._e3;
        }
        return ref FHashMap<K, V, TEq, TEntries>._missing;
    }

#if DEBUG
    internal struct ProbesTracker
    {
        internal int MaxProbes;
        internal int[] Probes;

        // will output something like
        // [Add] Probes abs max = 10, curr max = 6, all = [1: 180, 2: 103, 3: 59, 4: 23, 5: 3, 6: 1]; first 4 probes are 365 out of 369
        internal void DebugOutputProbes(string label)
        {
            Probes ??= new int[1];
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
            Probes ??= new int[1];
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
            Probes ??= new int[1];
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
/// It is configurable in regard of hash calculation/equality via `TEq` type paremeter and 
/// in regard of key-value storage via `TEntries` type parameter.
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
public struct FHashMap<K, V, TEq, TEntries>
    where TEq : struct, IEq<K>
    where TEntries : struct, IEntries<K, V, TEq>
{
#if DEBUG
    ProbesTracker _dbg;
#endif
    internal static Entry<K, V> _missing;

    internal byte _capacityBitShift;
    internal int _count;

    // The _packedHashesAndIndexes elements are of `Int32` with the bits split as following:
    // 00010|000...110|01101
    // |     |         |- The index into the _entries structure, 0-based. The index bit count (indexMask) is the hashes capacity - 1.
    // |     |         | This part of the erased hash is used to get the ideal index into the hashes array, so later this part of hash may be restored from the hash index and its probes.
    // |     |- The remaining middle bits of the original hash
    // |- 5 (MaxProbeBits) high bits of the Probe count, with the minimal value of b00001 indicating the non-empty slot.
    internal int[] _packedHashesAndIndexes;

#pragma warning disable IDE0044 // it tries to make entries readonly but they should stay modifyable to prevent its defensive struct copying  
    internal TEntries _entries;
#pragma warning restore IDE0044

    // todo: @wip first 4 on stack
    internal Entry<K, V> _e0, _e1, _e2, _e3;

    /// <summary>Capacity bits</summary>
    public int CapacityBitShift => _capacityBitShift;

    /// <summary>Access to the hashes and indexes</summary>
    public int[] PackedHashesAndIndexes => _packedHashesAndIndexes;

    /// <summary>Number of entries in the map</summary>
    public int Count => _count;

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

    [MethodImpl((MethodImplOptions)256)]
    internal ref V TryGetValueRefByHash(K key, out bool found)
    {
        Debug.Assert(_packedHashesAndIndexes != null);

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
                    found = true;
                    return ref e.Value;
                }
            }

            h = GetHash(ref hashesAndIndexes, ++hashIndex & indexMask);
            ++probes;
        }

        found = false;
        return ref _missing.Value;
    }

    /// <summary>Gets the reference to the existing value of the provided key, or the default value to set for the newly added key.</summary>
    [MethodImpl((MethodImplOptions)256)]
    public ref V GetOrAddValueRefByHash(K key)
    {
        // if the free space is less than 1/8 of capacity (12.5%) then Resize
        var indexMask = (1 << _capacityBitShift) - 1;
        if (indexMask - _count <= (indexMask >>> MinFreeCapacityShift))
            indexMask = ResizeHashes(indexMask);

        var hash = default(TEq).GetHashCode(key);
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
        h = (probes << ProbeCountShift) | hashMiddle | _count;
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

        return ref _entries.AddKeyAndGetValueRef(key, _count++);
    }

    /// <summary>Gets the reference to the existing value of the provided key, or the default value to set for the newly added key.</summary>
    public void AddInitialToHash(K key, int index)
    {
        var indexMask = (1 << _capacityBitShift) - 1;
#if NET7_0_OR_GREATER
        ref var hashesAndIndexes = ref MemoryMarshal.GetArrayDataReference(_packedHashesAndIndexes);
#else
        var hashesAndIndexes = _packedHashesAndIndexes;
#endif
        var hash = default(TEq).GetHashCode(key);
        var hashIndex = hash & indexMask;

        // 1. Skip over hashes with the bigger and equal probes. The hashes with bigger probes overlapping from the earlier ideal positions
        ref var h = ref GetHashRef(ref hashesAndIndexes, hashIndex);
        var probes = 1;
        while ((h >>> ProbeCountShift) >= probes)
        {
            h = ref GetHashRef(ref hashesAndIndexes, ++hashIndex & indexMask);
            ++probes;
        }

        // 3. We did not find the hash and therefore the key, so insert the new entry
        var hRobinHooded = h;
        h = (probes << ProbeCountShift) | (hash & HashAndIndexMask & ~indexMask) | index;
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
}
