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

internal static class Stack4
{
    public sealed class HeapItems<TItem>
    {
        public TItem[] Items;
        public HeapItems(int capacity) =>
            Items = new TItem[capacity];

        [MethodImpl((MethodImplOptions)256)]
        public void Put(int index, in TItem item)
        {
            if (index >= Items.Length)
                Array.Resize(ref Items, Items.Length << 1);
            Items[index] = item;
        }

        // todo: @improve Add explicit Remove method and think if count is decreased twice so the array may be resized
        [MethodImpl((MethodImplOptions)256)]
        public void PutDefault(int index)
        {
            if (index >= Items.Length)
                Array.Resize(ref Items, Items.Length << 1);
        }
    }
}

// here goes the LenseMan ;-)

/// <summary>Processes the `TItem` by-ref with the some (optional) state `A` returning result `R`.
/// The implementation of it supposesed to be struct so that the only method may be inlined</summary>
public interface IGetRef<TItem, A, R>
{
    /// <summary>Process `it` with state `a` returning `R`</summary>
    R Get(ref TItem it, in A a);
}

/// <summary>Processes the `TItem` by-ref with the some (optional) state `A`.
/// The implementation of it supposesed to be struct so that the only method may be inlined</summary>
public interface ISetRef<TItem, A>
{
    /// <summary>Process `it` with state `a`</summary>
    void Set(ref TItem it, in A a);
}

#pragma warning disable IDE1006
#pragma warning disable CS8981
/// <summary>Represents a no value</summary>
public readonly struct xo { }
#pragma warning restore IDE1006
#pragma warning restore CS8981

public struct Stack4<TItem> // todo: @wip rename to List4 to generalize the thing 
{
    public int Count;

    TItem _it0, _it1, _it2, _it3;

    Stack4.HeapItems<TItem> _deepItems;

    public TItem[] DebugDeepItems => _deepItems.Items; // todo: @note for debug/benchmarking only

    [MethodImpl((MethodImplOptions)256)]
    public void Put(int index, in TItem item)
    {
        switch (index)
        {
            case 0: _it0 = item; break;
            case 1: _it1 = item; break;
            case 2: _it2 = item; break;
            case 3: _it3 = item; break;
            default:
                _deepItems ??= new Stack4.HeapItems<TItem>(4);
                _deepItems.Put(index - 4, in item);
                break;
        }
    }

    [MethodImpl((MethodImplOptions)256)]
    public void PushLast(in TItem item) =>
        Put(Count++, item);

    [MethodImpl((MethodImplOptions)256)]
    public void PushLastDefault()
    {
        if (++Count >= 4)
        {
            if (_deepItems == null)
                _deepItems = new Stack4.HeapItems<TItem>(4);
            else
                _deepItems.PutDefault(Count - 4);
        }
    }

    [MethodImpl((MethodImplOptions)256)]
    public void PushLastDefault<TSetRef, A>(in A a, TSetRef setter = default)
        where TSetRef : struct, ISetRef<TItem, A>
    {
        var index = Count++;
        switch (index)
        {
            case 0: setter.Set(ref _it0, in a); break;
            case 1: setter.Set(ref _it1, in a); break;
            case 2: setter.Set(ref _it2, in a); break;
            case 3: setter.Set(ref _it3, in a); break;
            default:
                if (_deepItems == null)
                    _deepItems = new Stack4.HeapItems<TItem>(4);
                else
                    _deepItems.PutDefault(index - 4);
                setter.Set(ref _deepItems.Items[index - 4], in a);
                break;
        }
    }

    [MethodImpl((MethodImplOptions)256)]
    public TItem GetSurePresentItem(int index)
    {
        Debug.Assert(Count != 0);
        Debug.Assert(index < Count);
        switch (index)
        {
            case 0: return _it0;
            case 1: return _it1;
            case 2: return _it2;
            case 3: return _it3;
            default:
                Debug.Assert(_deepItems != null, $"Expecting a deeper parent stack created before accessing it here at level {index}");
                return _deepItems.Items[index - 4];
        }
    }

    [MethodImpl((MethodImplOptions)256)]
    public R GetSurePresentItem<TGetRef, A, R>(int index, in A a, TGetRef getter = default)
        where TGetRef : struct, IGetRef<TItem, A, R>
    {
        Debug.Assert(Count != 0);
        Debug.Assert(index < Count);
        switch (index)
        {
            case 0: return getter.Get(ref _it0, in a);
            case 1: return getter.Get(ref _it1, in a);
            case 2: return getter.Get(ref _it2, in a);
            case 3: return getter.Get(ref _it3, in a);
            default:
                Debug.Assert(_deepItems != null, $"Expecting a deeper parent stack created before accessing it here at level {index}");
                return getter.Get(ref _deepItems.Items[index - 4], in a);
        }
    }

    [MethodImpl((MethodImplOptions)256)]
    public TItem PeekLastSurePresentItem() =>
        GetSurePresentItem(Count - 1);

    [MethodImpl((MethodImplOptions)256)]
    public void PopLastSurePresentItem()
    {
        Debug.Assert(Count != 0);
        var index = --Count;
        switch (index)
        {
            case 0: _it0 = default; break;
            case 1: _it1 = default; break;
            case 2: _it2 = default; break;
            case 3: _it3 = default; break;
            default:
                Debug.Assert(_deepItems != null, $"Expecting a deeper parent stack created before accessing it here at level {index}");
                _deepItems.Put(index - 4, default);
                break;
        }
    }
}

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

        /// <summary>Returns the actual number of the stored entries</summary>
        int GetCount();

        /// <summary>Returns the reference to entry by its index, index should map to the present/non-removed entry</summary>
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
    private byte _capacityBitShift;

    // The _packedHashesAndIndexes elements are of `Int32` with the bits split as following:
    // 00010|000...110|01101
    // |     |         |- The index into the _entries structure, 0-based. The index bit count (indexMask) is the hashes capacity - 1.
    // |     |         | This part of the erased hash is used to get the ideal index into the hashes array, so later this part of hash may be restored from the hash index and its probes.
    // |     |- The remaining middle bits of the original hash
    // |- 5 (MaxProbeBits) high bits of the Probe count, with the minimal value of b00001 indicating the non-empty slot.
    private int[] _packedHashesAndIndexes;

#pragma warning disable IDE0044 // it tries to make entries readonly but they should stay modifyable to prevent its defensive struct copying  
    internal TEntries _entries;
#pragma warning restore IDE0044

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

    /// <summary>Find and return the index of the `key` in the `TEntries`. If not found return `-1`.
    /// Then you may use method `GetSurePresentValueRef` to access and modify entry value in-place!
    /// The approach differs from the `GetOrAddValueRef` because it does not add the new entry if the key is missing.</summary>
    [MethodImpl((MethodImplOptions)256)]
    public int GetEntryIndex(K key)
    {
        if (_packedHashesAndIndexes != null)
        {
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
                        return h & indexMask;
                }

                h = GetHash(ref hashesAndIndexes, ++hashIndex & indexMask);
                ++probes;
            }
        }
        return -1;
    }

    /// <summary>Allows to access and modify the present value in-place</summary>
    [MethodImpl((MethodImplOptions)256)]
    public ref V GetSurePresentValueRef(int index) =>
        ref _entries.GetSurePresentEntryRef(index).Value;

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
