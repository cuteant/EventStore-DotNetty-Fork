using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EventStore.Core.Exceptions;

namespace EventStore.Core.Index
{
    public class HashListMemTable : IMemTable, ISearchTable
    {
        private static readonly IComparer<Entry> MemTableComparer = new EntryComparer();

        public long Count { get { return _count; } }
        public Guid Id { get { return _id; } }
        public byte Version { get { return _version; } }

        private readonly ConcurrentDictionary<ulong, SortedList<Entry, byte>> _hash;
        private readonly Guid _id = Guid.NewGuid();
        private readonly byte _version;
        private int _count;

        private int _isConverting;

        public HashListMemTable(byte version, int maxSize)
        {
            _version = version;
            _hash = new ConcurrentDictionary<ulong, SortedList<Entry, byte>>();
        }

        public bool MarkForConversion()
        {
            return 0u >= (uint)Interlocked.CompareExchange(ref _isConverting, 1, 0);
        }

        public void Add(ulong stream, long version, long position)
        {
            AddEntries(new[] { new IndexEntry(stream, version, position) });
        }

        public void AddEntries(IList<IndexEntry> entries)
        {
            if (entries is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.entries); }
            if ((uint)(entries.Count - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.entries_Count); }

            var collection = entries.Select(x => new IndexEntry(GetHash(x.Stream), x.Version, x.Position)).ToList();

            // only one thread at a time can write
            Interlocked.Add(ref _count, collection.Count);

            var stream = collection[0].Stream; // NOTE: all entries should have the same stream
            SortedList<Entry, byte> list = null;
            try
            {
                if (!_hash.TryGetValue(stream, out list))
                {
                    list = new SortedList<Entry, byte>(MemTableComparer);
                    if (!Monitor.TryEnter(list, 10000)) { ThrowHelper.ThrowUnableToAcquireLockInReasonableTimeException(); }
                    _hash.AddOrUpdate(stream, list, (x, y) => { throw new Exception("This should never happen as MemTable updates are single-threaded."); });
                }
                else
                {
                    if (!Monitor.TryEnter(list, 10000)) { ThrowHelper.ThrowUnableToAcquireLockInReasonableTimeException(); }
                }

                for (int i = 0, n = collection.Count; i < n; ++i)
                {
                    var entry = collection[i];
                    if (entry.Stream != stream) { ThrowHelper.ThrowException_NotAllIndexEntriesInABulkHaveTheSameStreamHash(); }
                    if ((ulong)entry.Version > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.entry_Version); }
                    if ((ulong)entry.Position > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.entry_Position); }
                    list.Add(new Entry(entry.Version, entry.Position), 0);
                }
            }
            finally
            {
                if (list is object) { Monitor.Exit(list); }
            }
        }

        public bool TryGetOneValue(ulong stream, long number, out long position)
        {
            if ((ulong)number > Consts.TooBigOrNegativeUL)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.number);
            ulong hash = GetHash(stream);

            position = 0;

            SortedList<Entry, byte> list;
            if (_hash.TryGetValue(hash, out list))
            {
                if (!Monitor.TryEnter(list, 10000)) ThrowHelper.ThrowUnableToAcquireLockInReasonableTimeException();
                try
                {
                    int endIdx = list.UpperBound(new Entry(number, long.MaxValue));
                    if (endIdx == -1)
                        return false;

                    var key = list.Keys[endIdx];
                    if (key.EvNum == number)
                    {
                        position = key.LogPos;
                        return true;
                    }
                }
                finally
                {
                    Monitor.Exit(list);
                }
            }
            return false;
        }

        public bool TryGetLatestEntry(ulong stream, out IndexEntry entry)
        {
            ulong hash = GetHash(stream);
            entry = TableIndex.InvalidIndexEntry;

            SortedList<Entry, byte> list;
            if (_hash.TryGetValue(hash, out list))
            {
                if (!Monitor.TryEnter(list, 10000))
                    ThrowHelper.ThrowUnableToAcquireLockInReasonableTimeException();
                try
                {
                    var latest = list.Keys[list.Count - 1];
                    entry = new IndexEntry(hash, latest.EvNum, latest.LogPos);
                    return true;
                }
                finally
                {
                    Monitor.Exit(list);
                }
            }
            return false;
        }

        public bool TryGetOldestEntry(ulong stream, out IndexEntry entry)
        {
            ulong hash = GetHash(stream);
            entry = TableIndex.InvalidIndexEntry;

            SortedList<Entry, byte> list;
            if (_hash.TryGetValue(hash, out list))
            {
                if (!Monitor.TryEnter(list, 10000))
                    ThrowHelper.ThrowUnableToAcquireLockInReasonableTimeException();
                try
                {
                    var oldest = list.Keys[0];
                    entry = new IndexEntry(hash, oldest.EvNum, oldest.LogPos);
                    return true;
                }
                finally
                {
                    Monitor.Exit(list);
                }
            }
            return false;
        }

        public IEnumerable<IndexEntry> IterateAllInOrder()
        {
            //Log.Log1Trace("Sorting array in HashListMemTable.IterateAllInOrder...");

            var keys = _hash.Keys.ToArray();
            Array.Sort(keys, new ReverseComparer<ulong>());

            foreach (var key in keys)
            {
                var list = _hash[key];
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    var x = list.Keys[i];
                    yield return new IndexEntry(key, x.EvNum, x.LogPos);
                }
            }
            //Log.Log1Trace("Sorting array in HashListMemTable.IterateAllInOrder... DONE!");
        }

        public void Clear()
        {
            _hash.Clear();
        }

        public IEnumerable<IndexEntry> GetRange(ulong stream, long startNumber, long endNumber, int? limit = null)
        {
            if ((ulong)startNumber > Consts.TooBigOrNegativeUL)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startNumber);
            if ((ulong)endNumber > Consts.TooBigOrNegativeUL)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.endNumber);

            ulong hash = GetHash(stream);
            var ret = new List<IndexEntry>();

            SortedList<Entry, byte> list;
            if (_hash.TryGetValue(hash, out list))
            {
                if (!Monitor.TryEnter(list, 10000)) ThrowHelper.ThrowUnableToAcquireLockInReasonableTimeException();
                try
                {
                    var endIdx = list.UpperBound(new Entry(endNumber, long.MaxValue));
                    for (int i = endIdx; i >= 0; i--)
                    {
                        var key = list.Keys[i];
                        if (key.EvNum < startNumber || ret.Count == limit)
                            break;
                        ret.Add(new IndexEntry(hash, version: key.EvNum, position: key.LogPos));
                    }
                }
                finally
                {
                    Monitor.Exit(list);
                }
            }
            return ret;
        }

        private ulong GetHash(ulong hash)
        {
            return _version == PTableVersions.IndexV1 ? hash >> 32 : hash;
        }

        private readonly struct Entry
        {
            public readonly long EvNum;
            public readonly long LogPos;

            public Entry(long evNum, long logPos)
            {
                EvNum = evNum;
                LogPos = logPos;
            }
        }

        private class EntryComparer : IComparer<Entry>
        {
            public int Compare(Entry x, Entry y)
            {
                if (x.EvNum < y.EvNum) return -1;
                if (x.EvNum > y.EvNum) return 1;
                if (x.LogPos < y.LogPos) return -1;
                if (x.LogPos > y.LogPos) return 1;
                return 0;
            }
        }
    }

    public class ReverseComparer<T> : IComparer<T> where T : IComparable
    {
        public int Compare(T x, T y)
        {
            return -x.CompareTo(y);
        }
    }
}