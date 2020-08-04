using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.DataStructures;
using EventStore.Core.Exceptions;
using EventStore.Core.Settings;
using EventStore.Core.TransactionLog.Unbuffered;

namespace EventStore.Core.Index
{
    public enum FileType : byte
    {
        PTableFile = 1,
        ChunkFile = 2
    }

    public class PTableVersions
    {
        public const byte IndexV1 = 1;
        public const byte IndexV2 = 2;
        public const byte IndexV3 = 3;
        public const byte IndexV4 = 4;
    }

    public partial class PTable : ISearchTable, IDisposable
    {
        public const int IndexEntryV1Size = sizeof(int) + sizeof(int) + sizeof(long);
        public const int IndexEntryV2Size = sizeof(int) + sizeof(long) + sizeof(long);
        public const int IndexEntryV3Size = sizeof(long) + sizeof(long) + sizeof(long);
        public const int IndexEntryV4Size = IndexEntryV3Size;

        public const int IndexKeyV1Size = sizeof(int) + sizeof(int);
        public const int IndexKeyV2Size = sizeof(int) + sizeof(long);
        public const int IndexKeyV3Size = sizeof(long) + sizeof(long);
        public const int IndexKeyV4Size = IndexKeyV3Size;
        public const int MD5Size = 16;
        public const int DefaultBufferSize = 8192;
        public const int DefaultSequentialBufferSize = 65536;

        private static readonly ILogger Log = TraceLogger.GetLogger<PTable>();

        public Guid Id { get { return _id; } }
        public long Count { get { return _count; } }
        public string Filename { get { return _filename; } }
        public byte Version { get { return _version; } }

        private readonly Guid _id;
        private readonly string _filename;
        private readonly long _count;
        private readonly long _size;
        private readonly Midpoint[] _midpoints;
        private readonly uint _midpointsCached = 0;
        private readonly long _midpointsCacheSize = 0;

        private readonly IndexEntryKey _minEntry, _maxEntry;
        private readonly ObjectPool<WorkItem> _workItems;
        private readonly byte _version;
        private readonly int _indexEntrySize;
        private readonly int _indexKeySize;

        private readonly ManualResetEventSlim _destroyEvent = new ManualResetEventSlim(false);
        private volatile bool _deleteFile;

        internal Midpoint[] GetMidPoints() { return _midpoints; }

        private PTable(string filename,
                       Guid id,
                       int initialReaders = ESConsts.PTableInitialReaderCount,
                       int maxReaders = ESConsts.PTableMaxReaderCount,
                       int depth = 16,
                       bool skipIndexVerify = false)
        {
            if (string.IsNullOrEmpty(filename)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.filename); }
            if (Guid.Empty == id) { ThrowHelper.ThrowArgumentException_NotEmptyGuid(ExceptionArgument.id); }
            if ((uint)(maxReaders - 1) >= Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.maxReaders); }
            if ((uint)depth > Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.depth); }

            if (!File.Exists(filename))
                ThrowHelper.ThrowCorruptIndexException_PTableNotFound(filename);

            _id = id;
            _filename = filename;

#if DEBUG
            if (Log.IsTraceLevelEnabled()) Log.LoadingPTabl1eStarted(skipIndexVerify, Filename);
#endif
            var sw = Stopwatch.StartNew();
            _size = new FileInfo(_filename).Length;

            File.SetAttributes(_filename, FileAttributes.ReadOnly | FileAttributes.NotContentIndexed);

            _workItems = new ObjectPool<WorkItem>(string.Format("PTable {0} work items", _id),
                                                  initialReaders,
                                                  maxReaders,
                                                  () => new WorkItem(filename, DefaultBufferSize),
                                                  workItem => workItem.Dispose(),
                                                  pool => OnAllWorkItemsDisposed());

            var readerWorkItem = GetWorkItem();
            try
            {
                readerWorkItem.Stream.Seek(0, SeekOrigin.Begin);
                var header = PTableHeader.FromStream(readerWorkItem.Stream);
                if ((header.Version != PTableVersions.IndexV1) &&
                    (header.Version != PTableVersions.IndexV2) &&
                    (header.Version != PTableVersions.IndexV3) &&
                    (header.Version != PTableVersions.IndexV4))
                    ThrowHelper.ThrowCorruptIndexException_WrongFileVersion(_filename, header.Version, Version);
                _version = header.Version;

                if (_version == PTableVersions.IndexV1)
                {
                    _indexEntrySize = IndexEntryV1Size;
                    _indexKeySize = IndexKeyV1Size;
                }
                if (_version == PTableVersions.IndexV2)
                {
                    _indexEntrySize = IndexEntryV2Size;
                    _indexKeySize = IndexKeyV2Size;
                }
                if (_version == PTableVersions.IndexV3)
                {
                    _indexEntrySize = IndexEntryV3Size;
                    _indexKeySize = IndexKeyV3Size;
                }

                if (_version >= PTableVersions.IndexV4)
                {
                    //read the PTable footer
                    var previousPosition = readerWorkItem.Stream.Position;
                    readerWorkItem.Stream.Seek(readerWorkItem.Stream.Length - MD5Size - PTableFooter.GetSize(_version), SeekOrigin.Begin);
                    var footer = PTableFooter.FromStream(readerWorkItem.Stream);
                    if (footer.Version != header.Version)
                        ThrowHelper.ThrowCorruptIndexException_PTableHeaderAndFooterVersionMismatch(header.Version, footer.Version);

                    if (_version == PTableVersions.IndexV4)
                    {
                        _indexEntrySize = IndexEntryV4Size;
                        _indexKeySize = IndexKeyV4Size;
                    }
                    else
                        ThrowHelper.ThrowInvalidOperationException_UnknownPTableVersion(_version);

                    _midpointsCached = footer.NumMidpointsCached;
                    _midpointsCacheSize = _midpointsCached * _indexEntrySize;
                    readerWorkItem.Stream.Seek(previousPosition, SeekOrigin.Begin);
                }

                long indexEntriesTotalSize = (_size - PTableHeader.Size - _midpointsCacheSize - PTableFooter.GetSize(_version) - MD5Size);

                if ((ulong)indexEntriesTotalSize > Consts.TooBigOrNegativeUL)
                {
                    ThrowHelper.ThrowCorruptIndexException_TotalSizeOfIndexEntries(indexEntriesTotalSize, _size, _midpointsCacheSize, _version);
                }
                else if (indexEntriesTotalSize % _indexEntrySize != 0)
                {
                    ThrowHelper.ThrowCorruptIndexException_TotalSizeOfIndexEntries(indexEntriesTotalSize, _indexEntrySize);
                }

                _count = indexEntriesTotalSize / _indexEntrySize;

                if (_version >= PTableVersions.IndexV4 && _count > 0 && _midpointsCached > 0 && _midpointsCached < 2)
                {
                    //if there is at least 1 index entry with version>=4 and there are cached midpoints, there should always be at least 2 midpoints cached
                    ThrowHelper.ThrowCorruptIndexException_LessThan2MidpointsCachedInPTable(_count, _midpointsCached);
                }
                else if (_count >= 2 && _midpointsCached > _count)
                {
                    //if there are at least 2 index entries, midpoints count should be at most the number of index entries
                    ThrowHelper.ThrowCorruptIndexException_MoreMidpointsCachedInPTableThanIndexEntries(_midpointsCached, _count);
                }

                if (0ul >= (ulong)Count)
                {
                    _minEntry = new IndexEntryKey(ulong.MaxValue, long.MaxValue);
                    _maxEntry = new IndexEntryKey(ulong.MinValue, long.MinValue);
                }
                else
                {
                    var minEntry = ReadEntry(_indexEntrySize, Count - 1, readerWorkItem, _version);
                    _minEntry = new IndexEntryKey(minEntry.Stream, minEntry.Version);
                    var maxEntry = ReadEntry(_indexEntrySize, 0, readerWorkItem, _version);
                    _maxEntry = new IndexEntryKey(maxEntry.Stream, maxEntry.Version);
                }
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
            finally
            {
                ReturnWorkItem(readerWorkItem);
            }
            int calcdepth = 0;
            try
            {
                calcdepth = GetDepth(_count * _indexEntrySize, depth);
                _midpoints = CacheMidpointsAndVerifyHash(calcdepth, skipIndexVerify);
            }
            catch (PossibleToHandleOutOfMemoryException)
            {
                Log.UnableToCreateMidpointsForPtable(Filename, Count, depth);
            }
#if DEBUG
            if (Log.IsTraceLevelEnabled()) Log.LoadingPTable(_version, Filename, Count, calcdepth, sw.Elapsed);
#endif
        }

        internal Midpoint[] CacheMidpointsAndVerifyHash(int depth, bool skipIndexVerify)
        {
            var buffer = new byte[4096];
            if (!Helper.IsInRangeInclusive(depth, 0, 30) /*depth < 0 || depth > 30*/)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.depth);
            var count = Count;
            if (0ul >= (ulong)count || 0u >= (uint)depth) { return null; }
#if DEBUG
            var debugEnabled = Log.IsDebugLevelEnabled();
            if (skipIndexVerify && debugEnabled)
            {
                Log.Disabling_Verification_of_PTable();
            }
#endif

            Stream stream = null;
            WorkItem workItem = null;
            if (Runtime.IsUnixOrMac)
            {
                workItem = GetWorkItem();
                stream = workItem.Stream;
            }
            else
            {
                stream = UnbufferedFileStream.Create(_filename, FileMode.Open, FileAccess.Read, FileShare.Read, false, 4096, 4096, false, 4096);
            }

            try
            {
                int midpointsCount = 0;
                Midpoint[] midpoints = null;
                using (MD5 md5 = MD5.Create())
                {
                    try
                    {
                        midpointsCount = (int)Math.Max(2L, Math.Min((long)1 << depth, count));
                        midpoints = new Midpoint[midpointsCount];
                    }
                    catch (OutOfMemoryException exc)
                    {
                        ThrowHelper.ThrowPossibleToHandleOutOfMemoryException_FailedToAllocateMemoryForMidpointCache(exc);
                    }

                    if (skipIndexVerify && (_version >= PTableVersions.IndexV4))
                    {
                        if (_midpointsCached == midpointsCount)
                        {
                            //index verification is disabled and cached midpoints with the same depth requested are available
                            //so, we can load them directly from the PTable file
#if DEBUG
                            if (debugEnabled) Log.LoadingCachedMidpointsFromPTable(_midpointsCached);
#endif
                            long startOffset = stream.Length - MD5Size - PTableFooter.GetSize(_version) - _midpointsCacheSize;
                            stream.Seek(startOffset, SeekOrigin.Begin);
                            for (uint k = 0; k < _midpointsCached; k++)
                            {
                                stream.Read(buffer, 0, _indexEntrySize);
                                if (_version != PTableVersions.IndexV4)
                                {
                                    ThrowHelper.ThrowInvalidOperationException_UnknownPTableVersion(_version);
                                }
                                var key = new IndexEntryKey(BitConverter.ToUInt64(buffer, 8), BitConverter.ToInt64(buffer, 0));
                                var index = BitConverter.ToInt64(buffer, 8 + 8);
                                midpoints[k] = new Midpoint(key, index);

                                if (k > 0)
                                {
                                    if (midpoints[k].Key.GreaterThan(midpoints[k - 1].Key))
                                    {
                                        ThrowHelper.ThrowCorruptIndexException_ItemIndexForMidpointLessThan(k, midpoints);
                                    }
                                    else if (midpoints[k - 1].ItemIndex > midpoints[k].ItemIndex)
                                    {
                                        ThrowHelper.ThrowCorruptIndexException_ItemIndexForMidpointGreaterThan(k, midpoints);
                                    }
                                }
                            }

                            return midpoints;
                        }
#if DEBUG
                        else
                        {
                            if (debugEnabled) Log.SkippingLoadingOfCachedmidpointsFromPTable(_midpointsCached, midpointsCount);
                        }
#endif
                    }

                    if (!skipIndexVerify)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.Read(buffer, 0, PTableHeader.Size);
                        md5.TransformBlock(buffer, 0, PTableHeader.Size, null, 0);
                    }

                    long previousNextIndex = long.MinValue;
                    var previousKey = new IndexEntryKey(long.MaxValue, long.MaxValue);
                    for (long k = 0; k < midpointsCount; ++k)
                    {
                        long nextIndex = GetMidpointIndex(k, count, midpointsCount);
                        if (previousNextIndex != nextIndex)
                        {
                            if (!skipIndexVerify)
                            {
                                ReadUntilWithMd5(PTableHeader.Size + _indexEntrySize * nextIndex, stream, md5);
                                stream.Read(buffer, 0, _indexKeySize);
                                md5.TransformBlock(buffer, 0, _indexKeySize, null, 0);
                            }
                            else
                            {
                                stream.Seek(PTableHeader.Size + _indexEntrySize * nextIndex, SeekOrigin.Begin);
                                stream.Read(buffer, 0, _indexKeySize);
                            }

                            IndexEntryKey key;
                            if (_version == PTableVersions.IndexV1)
                            {
                                key = new IndexEntryKey(BitConverter.ToUInt32(buffer, 4), BitConverter.ToInt32(buffer, 0));
                            }
                            else if (_version == PTableVersions.IndexV2)
                            {
                                key = new IndexEntryKey(BitConverter.ToUInt64(buffer, 4), BitConverter.ToInt32(buffer, 0));
                            }
                            else
                            {
                                key = new IndexEntryKey(BitConverter.ToUInt64(buffer, 8), BitConverter.ToInt64(buffer, 0));
                            }
                            midpoints[k] = new Midpoint(key, nextIndex);
                            previousNextIndex = nextIndex;
                            previousKey = key;
                        }
                        else
                        {
                            midpoints[k] = new Midpoint(previousKey, previousNextIndex);
                        }

                        if (k > 0)
                        {
                            if (midpoints[k].Key.GreaterThan(midpoints[k - 1].Key))
                            {
                                ThrowHelper.ThrowCorruptIndexException_ItemIndexForMidpointLessThan(k, midpoints);
                            }
                            else if (midpoints[k - 1].ItemIndex > midpoints[k].ItemIndex)
                            {
                                ThrowHelper.ThrowCorruptIndexException_ItemIndexForMidpointGreaterThan(k, midpoints);
                            }
                        }
                    }

                    if (!skipIndexVerify)
                    {
                        ReadUntilWithMd5(stream.Length - MD5Size, stream, md5);
                        //verify hash (should be at stream.length - MD5Size)
                        md5.TransformFinalBlock(Empty.ByteArray, 0, 0);
                        var fileHash = new byte[MD5Size];
                        stream.Read(fileHash, 0, MD5Size);
                        ValidateHash(md5.Hash, fileHash);
                    }

                    return midpoints;
                }
            }
            catch
            {
                Dispose();
                throw;
            }
            finally
            {
                if (Runtime.IsUnixOrMac)
                {
                    if (workItem is object)
                        ReturnWorkItem(workItem);
                }
                else
                {
                    if (stream is object)
                        stream.Dispose();
                }
            }
        }


        private readonly byte[] TmpReadBuf = new byte[DefaultBufferSize];
        private void ReadUntilWithMd5(long nextPos, Stream fileStream, MD5 md5)
        {
            long toRead = nextPos - fileStream.Position;
            if ((ulong)toRead > Consts.TooBigOrNegativeUL) ThrowHelper.ThrowException_ShouldNotDoNegativeReads();
            while (toRead > 0L)
            {
                var localReadCount = Math.Min(toRead, TmpReadBuf.Length);
                int read = fileStream.Read(TmpReadBuf, 0, (int)localReadCount);
                md5.TransformBlock(TmpReadBuf, 0, read, null, 0);
                toRead -= read;
            }
        }
        void ValidateHash(byte[] fromFile, byte[] computed)
        {
            if (computed is null)
                ThrowHelper.ThrowCorruptIndexException_CalculatedMD5HashIsNull();
            if (fromFile is null)
                ThrowHelper.ThrowCorruptIndexException_ReadFromFileMD5HashIsNull();

            if ((uint)computed.Length != (uint)fromFile.Length)
                ThrowHelper.ThrowCorruptIndexException_HashSizesDiffer(fromFile, computed);

            for (int i = 0; i < fromFile.Length; i++)
            {
                if (fromFile[i] != computed[i])
                    ThrowHelper.ThrowCorruptIndexException_HashesAreDifferent(fromFile, computed);
            }
        }

        public IEnumerable<IndexEntry> IterateAllInOrder()
        {
            var workItem = GetWorkItem();
            try
            {
                workItem.Stream.Position = PTableHeader.Size;
                for (long i = 0, n = Count; i < n; i++)
                {
                    yield return ReadNextNoSeek(workItem, _version);
                }
            }
            finally
            {
                ReturnWorkItem(workItem);
            }
        }

        public bool TryGetOneValue(ulong stream, long number, out long position)
        {
            IndexEntry entry;
            var hash = GetHash(stream);
            if (TryGetLargestEntry(hash, number, number, out entry))
            {
                position = entry.Position;
                return true;
            }
            position = -1;
            return false;
        }

        public bool TryGetLatestEntry(ulong stream, out IndexEntry entry)
        {
            ulong hash = GetHash(stream);
            return TryGetLargestEntry(hash, 0, long.MaxValue, out entry);
        }

        private bool TryGetLargestEntry(ulong stream, long startNumber, long endNumber, out IndexEntry entry)
        {
            if ((ulong)startNumber > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.startNumber); }
            if ((ulong)endNumber > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.endNumber); }

            entry = TableIndex.InvalidIndexEntry;

            var startKey = BuildKey(stream, startNumber);
            var endKey = BuildKey(stream, endNumber);

            if (startKey.GreaterThan(_maxEntry) || endKey.SmallerThan(_minEntry))
                return false;

            var workItem = GetWorkItem();
            try
            {
                IndexEntryKey lowBoundsCheck, highBoundsCheck;
                var recordRange = LocateRecordRange(endKey, out lowBoundsCheck, out highBoundsCheck);

                long low = recordRange.Lower;
                long high = recordRange.Upper;
                while (low < high)
                {
                    var mid = low + (high - low) / 2;
                    IndexEntry midpoint = ReadEntry(_indexEntrySize, mid, workItem, _version);
                    var midpointKey = new IndexEntryKey(midpoint.Stream, midpoint.Version);

                    if (midpointKey.GreaterThan(lowBoundsCheck))
                    {
                        ThrowHelper.ThrowMaybeCorruptIndexException_LowBounds(midpointKey.Stream, midpointKey.Version, lowBoundsCheck.Stream, lowBoundsCheck.Version);
                    }
                    else if (!midpointKey.GreaterEqualsThan(highBoundsCheck))
                    {
                        ThrowHelper.ThrowMaybeCorruptIndexException_HighBounds(midpointKey.Stream, midpointKey.Version, highBoundsCheck.Stream, highBoundsCheck.Version);
                    }

                    if (midpointKey.GreaterThan(endKey))
                    {
                        low = mid + 1;
                        lowBoundsCheck = midpointKey;
                    }
                    else
                    {
                        high = mid;
                        highBoundsCheck = midpointKey;
                    }
                }

                var candEntry = ReadEntry(_indexEntrySize, high, workItem, _version);
                var candKey = new IndexEntryKey(candEntry.Stream, candEntry.Version);
                if (candKey.GreaterThan(endKey))
                    ThrowHelper.ThrowMaybeCorruptIndexException_CandEntryGreater(candEntry.Stream, candEntry.Version, startKey, stream, startNumber, endNumber, Filename);
                if (candKey.SmallerThan(startKey))
                    return false;
                entry = candEntry;
                return true;
            }
            finally
            {
                ReturnWorkItem(workItem);
            }
        }

        public bool TryGetOldestEntry(ulong stream, out IndexEntry entry)
        {
            ulong hash = GetHash(stream);
            return TryGetSmallestEntry(hash, 0, long.MaxValue, out entry);
        }

        private bool TryGetSmallestEntry(ulong stream, long startNumber, long endNumber, out IndexEntry entry)
        {
            if ((ulong)startNumber > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.startNumber); }
            if ((ulong)endNumber > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.endNumber); }

            entry = TableIndex.InvalidIndexEntry;

            var startKey = BuildKey(stream, startNumber);
            var endKey = BuildKey(stream, endNumber);

            if (startKey.GreaterThan(_maxEntry) || endKey.SmallerThan(_minEntry))
                return false;

            var workItem = GetWorkItem();
            try
            {
                IndexEntryKey lowBoundsCheck, highBoundsCheck;
                var recordRange = LocateRecordRange(startKey, out lowBoundsCheck, out highBoundsCheck);

                long low = recordRange.Lower;
                long high = recordRange.Upper;
                while (low < high)
                {
                    var mid = low + (high - low + 1) / 2;
                    var midpoint = ReadEntry(_indexEntrySize, mid, workItem, _version);
                    var midpointKey = new IndexEntryKey(midpoint.Stream, midpoint.Version);

                    if (midpointKey.GreaterThan(lowBoundsCheck))
                    {
                        ThrowHelper.ThrowMaybeCorruptIndexException_LowBounds(midpointKey.Stream, midpointKey.Version, lowBoundsCheck.Stream, lowBoundsCheck.Version);
                    }
                    else if (!midpointKey.GreaterEqualsThan(highBoundsCheck))
                    {
                        ThrowHelper.ThrowMaybeCorruptIndexException_HighBounds(midpointKey.Stream, midpointKey.Version, highBoundsCheck.Stream, highBoundsCheck.Version);
                    }

                    if (midpointKey.SmallerThan(startKey))
                    {
                        high = mid - 1;
                        highBoundsCheck = midpointKey;
                    }
                    else
                    {
                        low = mid;
                        lowBoundsCheck = midpointKey;
                    }
                }

                var candEntry = ReadEntry(_indexEntrySize, high, workItem, _version);
                var candidateKey = new IndexEntryKey(candEntry.Stream, candEntry.Version);
                if (candidateKey.SmallerThan(startKey))
                    ThrowHelper.ThrowMaybeCorruptIndexException_CandEntryLess(candEntry.Stream, candEntry.Version, startKey, stream, startNumber, endNumber, Filename);
                if (candidateKey.GreaterThan(endKey))
                    return false;
                entry = candEntry;
                return true;
            }
            finally
            {
                ReturnWorkItem(workItem);
            }
        }

        public IEnumerable<IndexEntry> GetRange(ulong stream, long startNumber, long endNumber, int? limit = null)
        {
            if ((ulong)startNumber > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.startNumber); }
            if ((ulong)endNumber > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.endNumber); }

            ulong hash = GetHash(stream);

            var result = new List<IndexEntry>();
            var startKey = BuildKey(hash, startNumber);
            var endKey = BuildKey(hash, endNumber);

            if (startKey.GreaterThan(_maxEntry) || endKey.SmallerThan(_minEntry))
                return result;

            var workItem = GetWorkItem();
            try
            {
                IndexEntryKey lowBoundsCheck, highBoundsCheck;
                var recordRange = LocateRecordRange(endKey, out lowBoundsCheck, out highBoundsCheck);
                long low = recordRange.Lower;
                long high = recordRange.Upper;
                while (low < high)
                {
                    var mid = low + (high - low) / 2;
                    IndexEntry midpoint = ReadEntry(_indexEntrySize, mid, workItem, _version);
                    var midpointKey = new IndexEntryKey(midpoint.Stream, midpoint.Version);

                    if (midpointKey.GreaterThan(lowBoundsCheck))
                    {
                        ThrowHelper.ThrowMaybeCorruptIndexException_LowBounds(midpointKey.Stream, midpointKey.Version, lowBoundsCheck.Stream, lowBoundsCheck.Version);
                    }
                    else if (!midpointKey.GreaterEqualsThan(highBoundsCheck))
                    {
                        ThrowHelper.ThrowMaybeCorruptIndexException_HighBounds(midpointKey.Stream, midpointKey.Version, highBoundsCheck.Stream, highBoundsCheck.Version);
                    }

                    if (midpointKey.SmallerEqualsThan(endKey))
                    {
                        high = mid;
                        highBoundsCheck = midpointKey;
                    }
                    else
                    {
                        low = mid + 1;
                        lowBoundsCheck = midpointKey;
                    }
                }

                PositionAtEntry(_indexEntrySize, high, workItem);
                for (long i = high, n = Count; i < n; ++i)
                {
                    IndexEntry entry = ReadNextNoSeek(workItem, _version);
                    var candidateKey = new IndexEntryKey(entry.Stream, entry.Version);
                    if (candidateKey.GreaterThan(endKey))
                        ThrowHelper.ThrowMaybeCorruptIndexException_Entry(entry.Stream, entry.Version, startKey, stream, startNumber, endNumber, Filename);
                    if (candidateKey.SmallerThan(startKey))
                        return result;
                    result.Add(entry);
                    if (result.Count == limit) break;
                }
                return result;
            }
            finally
            {
                ReturnWorkItem(workItem);
            }
        }

        private ulong GetHash(ulong hash)
        {
            return _version == PTableVersions.IndexV1 ? hash >> 32 : hash;
        }

        private static IndexEntryKey BuildKey(ulong stream, long version)
        {
            return new IndexEntryKey(stream, version);
        }

        private EventStore.Core.Data.Range LocateRecordRange(in IndexEntryKey key, out IndexEntryKey lowKey, out IndexEntryKey highKey)
        {
            lowKey = new IndexEntryKey(ulong.MaxValue, long.MaxValue);
            highKey = new IndexEntryKey(ulong.MinValue, long.MinValue);

            var midpoints = _midpoints;
            if (midpoints is null)
                return new EventStore.Core.Data.Range(0, Count - 1);
            long lowerMidpoint = LowerMidpointBound(midpoints, key);
            long upperMidpoint = UpperMidpointBound(midpoints, key);

            lowKey = midpoints[lowerMidpoint].Key;
            highKey = midpoints[upperMidpoint].Key;

            return new EventStore.Core.Data.Range(midpoints[lowerMidpoint].ItemIndex, midpoints[upperMidpoint].ItemIndex);
        }

        private long LowerMidpointBound(Midpoint[] midpoints, in IndexEntryKey key)
        {
            long l = 0;
            long r = midpoints.Length - 1;
            while (l < r)
            {
                long m = l + (r - l + 1) / 2;
                if (midpoints[m].Key.GreaterThan(key))
                    l = m;
                else
                    r = m - 1;
            }
            return l;
        }

        private long UpperMidpointBound(Midpoint[] midpoints, in IndexEntryKey key)
        {
            long l = 0;
            long r = midpoints.Length - 1;
            while (l < r)
            {
                long m = l + (r - l) / 2;
                if (midpoints[m].Key.SmallerThan(key))
                    r = m;
                else
                    l = m + 1;
            }
            return r;
        }

        private static void PositionAtEntry(int indexEntrySize, long indexNum, WorkItem workItem)
        {
            workItem.Stream.Seek(indexEntrySize * indexNum + PTableHeader.Size, SeekOrigin.Begin);
        }

        private static IndexEntry ReadEntry(int indexEntrySize, long indexNum, WorkItem workItem, int ptableVersion)
        {
            long seekTo = indexEntrySize * indexNum + PTableHeader.Size;
            workItem.Stream.Seek(seekTo, SeekOrigin.Begin);
            return ReadNextNoSeek(workItem, ptableVersion);
        }

        private static IndexEntry ReadNextNoSeek(WorkItem workItem, int ptableVersion)
        {
            long version = (ptableVersion >= PTableVersions.IndexV3) ? workItem.Reader.ReadInt64() : workItem.Reader.ReadInt32();
            ulong stream = ptableVersion == PTableVersions.IndexV1 ? workItem.Reader.ReadUInt32() : workItem.Reader.ReadUInt64();
            long position = workItem.Reader.ReadInt64();
            return new IndexEntry(stream, version, position);
        }

        private WorkItem GetWorkItem()
        {
            try
            {
                return _workItems.Get();
            }
            catch (ObjectPoolDisposingException)
            {
                ThrowHelper.ThrowFileBeingDeletedException(); return null;
            }
            catch (ObjectPoolMaxLimitReachedException)
            {
                ThrowHelper.ThrowException(ExceptionResource.Unable_to_acquire_work_item); return null;
            }
        }

        private void ReturnWorkItem(WorkItem workItem)
        {
            _workItems.Return(workItem);
        }

        public void MarkForDestruction()
        {
            _deleteFile = true;
            _workItems.MarkForDisposal();
        }

        public void Dispose()
        {
            _deleteFile = false;
            _workItems.MarkForDisposal();
        }

        private void OnAllWorkItemsDisposed()
        {
            File.SetAttributes(_filename, FileAttributes.Normal);
            if (_deleteFile)
                File.Delete(_filename);
            _destroyEvent.Set();
        }

        public void WaitForDisposal(int timeout)
        {
            if (!_destroyEvent.Wait(timeout))
                ThrowHelper.ThrowTimeoutException();
        }

        public void WaitForDisposal(TimeSpan timeout)
        {
            if (!_destroyEvent.Wait(timeout))
                ThrowHelper.ThrowTimeoutException();
        }

        internal readonly struct Midpoint
        {
            public readonly IndexEntryKey Key;
            public readonly long ItemIndex;

            public Midpoint(in IndexEntryKey key, long itemIndex)
            {
                Key = key;
                ItemIndex = itemIndex;
            }
        }

        internal readonly struct IndexEntryKey
        {
            public readonly ulong Stream;
            public readonly long Version;
            public IndexEntryKey(ulong stream, long version)
            {
                Stream = stream;
                Version = version;
            }

            public bool GreaterThan(in IndexEntryKey other)
            {
                if (Stream == other.Stream)
                {
                    return Version > other.Version;
                }
                return Stream > other.Stream;
            }

            public bool SmallerThan(in IndexEntryKey other)
            {
                if (Stream == other.Stream)
                {
                    return Version < other.Version;
                }
                return Stream < other.Stream;
            }

            public bool GreaterEqualsThan(in IndexEntryKey other)
            {
                if (Stream == other.Stream)
                {
                    return Version >= other.Version;
                }
                return Stream >= other.Stream;
            }

            public bool SmallerEqualsThan(in IndexEntryKey other)
            {
                if (Stream == other.Stream)
                {
                    return Version <= other.Version;
                }
                return Stream <= other.Stream;
            }

            public override string ToString()
            {
                return string.Format("Stream: {0}, Version: {1}", Stream, Version);
            }
        }

        private class WorkItem : IDisposable
        {
            public readonly FileStream Stream;
            public readonly BinaryReader Reader;

            public WorkItem(string filename, int bufferSize)
            {
                Stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.RandomAccess);
                Reader = new BinaryReader(Stream);
            }

            ~WorkItem()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Stream.Dispose();
                    Reader.Dispose();
                }
            }
        }
    }
}
