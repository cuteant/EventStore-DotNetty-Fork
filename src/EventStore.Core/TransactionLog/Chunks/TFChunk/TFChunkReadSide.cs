using System;
using System.Collections.Generic;
using System.IO;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.DataStructures;
using EventStore.Core.Exceptions;
using EventStore.Core.TransactionLog.LogRecords;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.TransactionLog.Chunks.TFChunk
{
    public partial class TFChunk
    {
        public interface IChunkReadSide
        {
            void Cache();
            void Uncache();

            bool ExistsAt(long logicalPosition);
            RecordReadResult TryReadAt(long logicalPosition);
            RecordReadResult TryReadFirst();
            RecordReadResult TryReadClosestForward(long logicalPosition);
            RecordReadResult TryReadLast();
            RecordReadResult TryReadClosestBackward(long logicalPosition);
        }

        private class TFChunkReadSideUnscavenged : TFChunkReadSide, IChunkReadSide
        {
            public TFChunkReadSideUnscavenged(TFChunk chunk) : base(chunk)
            {
                if (chunk.ChunkHeader.IsScavenged)
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Scavenged_TFChunk_passed_into_unscavenged_chunk_read_side);
            }

            public void Cache()
            {
                // do nothing
            }

            public void Uncache()
            {
                // do nothing
            }

            public bool ExistsAt(long logicalPosition)
            {
                return logicalPosition >= 0 && logicalPosition < Chunk.LogicalDataSize;
            }

            public RecordReadResult TryReadAt(long logicalPosition)
            {
                var workItem = Chunk.GetReaderWorkItem();
                try
                {
                    if (logicalPosition >= Chunk.LogicalDataSize)
                    {
                        return RecordReadResult.Failure;
                    }

                    var result = TryReadForwardInternal(workItem, logicalPosition, out int length, out LogRecord record);
                    return new RecordReadResult(result, -1, record, length);
                }
                finally
                {
                    Chunk.ReturnReaderWorkItem(workItem);
                }
            }

            public RecordReadResult TryReadFirst()
            {
                return TryReadClosestForward(0);
            }

            public RecordReadResult TryReadClosestForward(long logicalPosition)
            {
                var workItem = Chunk.GetReaderWorkItem();
                try
                {
                    if (logicalPosition >= Chunk.LogicalDataSize)
                    {
                        return RecordReadResult.Failure;
                    }

                    if (!TryReadForwardInternal(workItem, logicalPosition, out int length, out LogRecord record))
                    {
                        return RecordReadResult.Failure;
                    }

                    long nextLogicalPos = record.GetNextLogPosition(logicalPosition, length);
                    return new RecordReadResult(true, nextLogicalPos, record, length);
                }
                finally
                {
                    Chunk.ReturnReaderWorkItem(workItem);
                }
            }

            public RecordReadResult TryReadLast()
            {
                return TryReadClosestBackward(Chunk.LogicalDataSize);
            }

            public RecordReadResult TryReadClosestBackward(long logicalPosition)
            {
                var workItem = Chunk.GetReaderWorkItem();
                try
                {
                    // here we allow actualPosition == _logicalDataSize as we can read backward the very last record that way
                    if (logicalPosition > Chunk.LogicalDataSize)
                    {
                        return RecordReadResult.Failure;
                    }

                    if (!TryReadBackwardInternal(workItem, logicalPosition, out int length, out LogRecord record))
                    {
                        return RecordReadResult.Failure;
                    }

                    long nextLogicalPos = record.GetPrevLogPosition(logicalPosition, length);
                    return new RecordReadResult(true, nextLogicalPos, record, length);
                }
                finally
                {
                    Chunk.ReturnReaderWorkItem(workItem);
                }
            }
        }

        private class TFChunkReadSideScavenged : TFChunkReadSide, IChunkReadSide
        {
            private static readonly ILogger s_logger = TraceLogger.GetLogger<TFChunkReadSideScavenged>();
            private Midpoint[] _midpoints;
            private bool _optimizeCache;
            private BloomFilter _logPositionsBloomFilter;

            private bool CacheIsOptimized { get { return _optimizeCache && _logPositionsBloomFilter != null; }}

            public TFChunkReadSideScavenged(TFChunk chunk, bool optimizeCache)
              : base(chunk)
            {
                _optimizeCache = optimizeCache;
                if (!chunk.ChunkHeader.IsScavenged)
                {
                    ThrowHelper.ThrowArgumentException_ChunkProvidedIsNotScavenged(chunk);
                }
            }

            public void Uncache()
            {
                _midpoints = null;
            }

            public void Cache()
            {
                _midpoints = PopulateMidpoints(Chunk.MidpointsDepth);
            }

            public void OptimizeExistsAt(){
                if(_optimizeCache && _logPositionsBloomFilter == null)
                    _logPositionsBloomFilter = PopulateBloomFilter();
            }

            public void DeOptimizeExistsAt(){
                if(_logPositionsBloomFilter != null)
                    _logPositionsBloomFilter = null;
            }

            private BloomFilter PopulateBloomFilter(){
                var mapCount = Chunk.ChunkFooter.MapCount;
                if(mapCount <= 0) return null;

                BloomFilter bf = null;
                double p = 1e-4; //false positive probability

                while(p < 1.0){
                    try{
                        bf = new BloomFilter(mapCount, p);
                        //Log.LogDebug("Created bloom filter with {0} bits and {1} hash functions for chunk {2} with map count: {3}", bf.NumBits, bf.NumHashFunctions, Chunk.FileName, mapCount);
                        break;
                    }
                    catch(ArgumentOutOfRangeException){
                        p *= 10.0;
                    }
                }

                if (bf == null){
                    if (s_logger.IsWarningLevelEnabled()) s_logger.Could_not_create_bloom_filter_for_chunk(Chunk.FileName, mapCount);
                    return null;
                }

                ReaderWorkItem workItem = null;
                try
                {
                    workItem = Chunk.GetReaderWorkItem();

                    foreach(var posMap in ReadPosMap(workItem, 0, mapCount)){
                        bf.Add(posMap.LogPos);
                    }

                    //Log.LogDebug("{0} items added to bloom filter for chunk {1}", mapCount, Chunk.FileName);
                    return bf;
                }
                catch (FileBeingDeletedException)
                {
                    return null;
                }
                catch (OutOfMemoryException)
                {
                    return null;
                }
                finally
                {
                    if (workItem != null)
                        Chunk.ReturnReaderWorkItem(workItem);
                }
            }

            private Midpoint[] PopulateMidpoints(int depth)
            {
                if (depth > 31)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_DepthTooForMidpoints();
                }

                if (Chunk.ChunkFooter.MapCount == 0) // empty chunk
                {
                    return null;
                }

                ReaderWorkItem workItem = null;
                try
                {
                    workItem = Chunk.GetReaderWorkItem();

                    int midPointsCnt = 1 << depth;
                    int segmentSize;
                    Midpoint[] midpoints;
                    var mapCount = Chunk.ChunkFooter.MapCount;
                    if (mapCount < midPointsCnt)
                    {
                        segmentSize = 1; // we cache all items
                        midpoints = new Midpoint[mapCount];
                    }
                    else
                    {
                        segmentSize = mapCount / midPointsCnt;
                        midpoints = new Midpoint[1 + (mapCount + segmentSize - 1) / segmentSize];
                    }

                    for (int x = 0, i = 0, xN = mapCount - 1; x < xN; x += segmentSize, i += 1)
                    {
                        midpoints[i] = new Midpoint(x, ReadPosMap(workItem, x));
                    }

                    // add the very last item as the last midpoint (possibly it is done twice)
                    midpoints[midpoints.Length - 1] = new Midpoint(mapCount - 1, ReadPosMap(workItem, mapCount - 1));
                    return midpoints;
                }
                catch (FileBeingDeletedException)
                {
                    return null;
                }
                catch (OutOfMemoryException)
                {
                    return null;
                }
                finally
                {
                    if (workItem != null)
                    {
                        Chunk.ReturnReaderWorkItem(workItem);
                    }
                }
            }

            private PosMap ReadPosMap(ReaderWorkItem workItem, long index){
                foreach(var posMap in ReadPosMap(workItem, index, 1)){
                    return posMap;
                }
                return ThrowHelper.ThrowArgumentOutOfRangeException_CouldNotReadPosMapAtIndex(index);
            }

            private IEnumerable<PosMap> ReadPosMap(ReaderWorkItem workItem, long index, int count)
            {
                if (Chunk.ChunkFooter.IsMap12Bytes)
                {
                    var pos = ChunkHeader.Size + Chunk.ChunkFooter.PhysicalDataSize + index*PosMap.FullSize;
                    workItem.Stream.Seek(pos, SeekOrigin.Begin);
                    for(int i=0;i<count;i++)
                        yield return PosMap.FromNewFormat(workItem.Reader);
                }
                else
                {
                    var pos = ChunkHeader.Size + Chunk.ChunkFooter.PhysicalDataSize + index*PosMap.DeprecatedSize;
                    workItem.Stream.Seek(pos, SeekOrigin.Begin);
                    for(int i=0;i<count;i++)
                        yield return PosMap.FromOldFormat(workItem.Reader);
                }
            }

            public bool ExistsAt(long logicalPosition){
                if(CacheIsOptimized)
                    return MayExistAt(logicalPosition);

                var workItem = Chunk.GetReaderWorkItem();
                try
                {
                    var actualPosition = TranslateExactPosition(workItem, logicalPosition);
                    return actualPosition >= 0 && actualPosition < Chunk.PhysicalDataSize;
                }
                finally
                {
                    Chunk.ReturnReaderWorkItem(workItem);
                }
            }

            public bool MayExistAt(long logicalPosition)
            {
                /* This function is much faster than ExistsAt. However, it may return false positives (with a very low probability) but never false negatives */
                return _logPositionsBloomFilter.MayExist(logicalPosition);
            }

            public RecordReadResult TryReadAt(long logicalPosition)
            {
                var workItem = Chunk.GetReaderWorkItem();
                try
                {
                    var actualPosition = TranslateExactPosition(workItem, logicalPosition);
                    if (actualPosition == -1 || actualPosition >= Chunk.PhysicalDataSize)
                    {
                        return RecordReadResult.Failure;
                    }

                    var result = TryReadForwardInternal(workItem, actualPosition, out int length, out LogRecord record);
                    return new RecordReadResult(result, -1, record, length);
                }
                finally
                {
                    Chunk.ReturnReaderWorkItem(workItem);
                }
            }

            private int TranslateExactPosition(ReaderWorkItem workItem, long pos)
            {
                var midpoints = _midpoints;
                if (workItem.IsMemory || midpoints == null)
                {
                    return TranslateExactWithoutMidpoints(workItem, pos, 0, Chunk.ChunkFooter.MapCount - 1);
                }

                return TranslateExactWithMidpoints(workItem, midpoints, pos);
            }

            private int TranslateExactWithoutMidpoints(ReaderWorkItem workItem, long pos, long startIndex, long endIndex)
            {
                long low = startIndex;
                long high = endIndex;
                while (low <= high)
                {
                    var mid = low + (high - low) / 2;
                    var v = ReadPosMap(workItem, mid);

                    if (v.LogPos == pos)
                    {
                        return v.ActualPos;
                    }

                    if (v.LogPos < pos)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid - 1;
                    }
                }
                return -1;
            }

            private int TranslateExactWithMidpoints(ReaderWorkItem workItem, Midpoint[] midpoints, long pos)
            {
                if (pos < midpoints[0].LogPos || pos > midpoints[midpoints.Length - 1].LogPos)
                {
                    return -1;
                }

                var recordRange = LocatePosRange(midpoints, pos);
                return TranslateExactWithoutMidpoints(workItem, pos, recordRange.Lower, recordRange.Upper);
            }

            public RecordReadResult TryReadFirst()
            {
                return TryReadClosestForward(0);
            }

            public RecordReadResult TryReadClosestForward(long logicalPosition)
            {
                if (Chunk.ChunkFooter.MapCount == 0)
                {
                    return RecordReadResult.Failure;
                }

                var workItem = Chunk.GetReaderWorkItem();
                try
                {
                    var actualPosition = TranslateClosestForwardPosition(workItem, logicalPosition);
                    if (actualPosition == -1 || actualPosition >= Chunk.PhysicalDataSize)
                    {
                        return RecordReadResult.Failure;
                    }

                    if (!TryReadForwardInternal(workItem, actualPosition, out int length, out LogRecord record))
                    {
                        return RecordReadResult.Failure;
                    }

                    long nextLogicalPos = Chunk.ChunkHeader.GetLocalLogPosition(record.GetNextLogPosition(record.LogPosition, length));
                    return new RecordReadResult(true, nextLogicalPos, record, length);
                }
                finally
                {
                    Chunk.ReturnReaderWorkItem(workItem);
                }
            }

            public RecordReadResult TryReadLast()
            {
                return TryReadClosestBackward(Chunk.LogicalDataSize);
            }

            public RecordReadResult TryReadClosestBackward(long logicalPosition)
            {
                if (Chunk.ChunkFooter.MapCount == 0)
                {
                    return RecordReadResult.Failure;
                }

                var workItem = Chunk.GetReaderWorkItem();
                try
                {
                    var actualPosition = TranslateClosestForwardPosition(workItem, logicalPosition);
                    // here we allow actualPosition == _physicalDataSize as we can read backward the very last record that way
                    if (actualPosition == -1 || actualPosition > Chunk.PhysicalDataSize)
                    {
                        return RecordReadResult.Failure;
                    }

                    if (!TryReadBackwardInternal(workItem, actualPosition, out int length, out LogRecord record))
                    {
                        return RecordReadResult.Failure;
                    }

                    long nextLogicalPos = Chunk.ChunkHeader.GetLocalLogPosition(record.LogPosition);
                    return new RecordReadResult(true, nextLogicalPos, record, length);
                }
                finally
                {
                    Chunk.ReturnReaderWorkItem(workItem);
                }
            }

            private int TranslateClosestForwardPosition(ReaderWorkItem workItem, long logicalPosition)
            {
                var midpoints = _midpoints;
                if (workItem.IsMemory || midpoints == null)
                {
                    return TranslateClosestForwardWithoutMidpoints(workItem, logicalPosition, 0, Chunk.ChunkFooter.MapCount - 1);
                }

                return TranslateClosestForwardWithMidpoints(workItem, midpoints, logicalPosition);
            }

            private int TranslateClosestForwardWithMidpoints(ReaderWorkItem workItem, Midpoint[] midpoints, long pos)
            {
                // to allow backward reading of the last record, forward read will decline anyway
                if (pos > midpoints[midpoints.Length - 1].LogPos)
                {
                    return Chunk.PhysicalDataSize;
                }

                var recordRange = LocatePosRange(midpoints, pos);
                return TranslateClosestForwardWithoutMidpoints(workItem, pos, recordRange.Lower, recordRange.Upper);
            }

            private int TranslateClosestForwardWithoutMidpoints(ReaderWorkItem workItem, long pos, long startIndex, long endIndex)
            {
                PosMap res = ReadPosMap(workItem, endIndex);

                // to allow backward reading of the last record, forward read will decline anyway
                if (pos > res.LogPos)
                {
                    return Chunk.PhysicalDataSize;
                }

                long low = startIndex;
                long high = endIndex;
                while (low < high)
                {
                    var mid = low + (high - low) / 2;
                    var v = ReadPosMap(workItem, mid);

                    if (v.LogPos < pos)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid;
                        res = v;
                    }
                }
                return res.ActualPos;
            }

            private static Range LocatePosRange(Midpoint[] midpoints, long pos)
            {
                int lowerMidpoint = LowerMidpointBound(midpoints, pos);
                int upperMidpoint = UpperMidpointBound(midpoints, pos);
                return new Range(midpoints[lowerMidpoint].ItemIndex, midpoints[upperMidpoint].ItemIndex);
            }

            /// <summary>
            /// Returns the index of lower midpoint for given logical position.
            /// Assumes it always exist.
            /// </summary>
            private static int LowerMidpointBound(Midpoint[] midpoints, long pos)
            {
                int l = 0;
                int r = midpoints.Length - 1;
                while (l < r)
                {
                    int m = l + (r - l + 1) / 2;
                    if (midpoints[m].LogPos <= pos)
                    {
                        l = m;
                    }
                    else
                    {
                        r = m - 1;
                    }
                }

                return l;
            }

            /// <summary>
            /// Returns the index of upper midpoint for given logical position.
            /// Assumes it always exist.
            /// </summary>
            private static int UpperMidpointBound(Midpoint[] midpoints, long pos)
            {
                int l = 0;
                int r = midpoints.Length - 1;
                while (l < r)
                {
                    int m = l + (r - l) / 2;
                    if (midpoints[m].LogPos >= pos)
                    {
                        r = m;
                    }
                    else
                    {
                        l = m + 1;
                    }
                }
                return l;
            }
        }

        private abstract class TFChunkReadSide
        {
            protected readonly TFChunk Chunk;

            protected TFChunkReadSide(TFChunk chunk)
            {
                if (null == chunk) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chunk); }
                Chunk = chunk;
            }

            protected bool TryReadForwardInternal(ReaderWorkItem workItem, long actualPosition, out int length, out LogRecord record)
            {
                length = -1;
                record = null;

                workItem.Stream.Position = GetRawPosition(actualPosition);

                if (actualPosition + 2 * sizeof(int) > Chunk.PhysicalDataSize) // no space even for length prefix and suffix
                    return false;

                length = workItem.Reader.ReadInt32();
                if (length <= 0)
                {
                    ThrowHelper.ThrowInvalidReadException_LogRecordAtActualPosHasNonPositiveLength(actualPosition, length, Chunk);
                }
                if (length > TFConsts.MaxLogRecordSize)
                {
                    ThrowHelper.ThrowInvalidReadException_LogRecordAtActualPosHasTooLargeLength(actualPosition, length, Chunk);
                }
                if (actualPosition + length + 2 * sizeof(int) > Chunk.PhysicalDataSize)
                {
                    ThrowHelper.ThrowUnableToReadPastEndOfStreamException_ThereIsNotEnoughSpaceToReadFullRecordPrefix(length, actualPosition, Chunk);
                }

                record = LogRecord.ReadFrom(workItem.Reader);

                // verify suffix length == prefix length
                int suffixLength = workItem.Reader.ReadInt32();
                if (suffixLength != length)
                {
                    ThrowHelper.ThrowException_SuffixLengthInconsistency(length, suffixLength, actualPosition, Chunk);
                }

                return true;
            }

            protected bool TryReadBackwardInternal(ReaderWorkItem workItem, long actualPosition, out int length, out LogRecord record)
            {
                length = -1;
                record = null;

                if (actualPosition < 2 * sizeof(int)) // no space even for length prefix and suffix
                {
                    return false;
                }

                var realPos = GetRawPosition(actualPosition);
                workItem.Stream.Position = realPos - sizeof(int);

                length = workItem.Reader.ReadInt32();
                if (length <= 0)
                {
                    ThrowHelper.ThrowInvalidReadException_LogRecordThatEndsAtActualPosHasNonPositiveLength(actualPosition, length, Chunk);
                }
                if (length > TFConsts.MaxLogRecordSize)
                {
                    ThrowHelper.ThrowArgumentException_LogRecordThatEndsAtActualPosHasTooLargeLength(actualPosition, length, Chunk);
                }
                if (actualPosition < length + 2 * sizeof(int)) // no space for record + length prefix and suffix
                {
                    ThrowHelper.ThrowUnableToReadPastEndOfStreamException_ThereIsNotEnoughSpaceToReadFullRecordSuffix(length, actualPosition, Chunk);
                }

                workItem.Stream.Position = realPos - length - 2 * sizeof(int);

                // verify suffix length == prefix length
                int prefixLength = workItem.Reader.ReadInt32();
                if (prefixLength != length)
                {
                    ThrowHelper.ThrowException_PrefixLengthInconsistency(prefixLength, length, actualPosition, Chunk);
                }
                record = LogRecord.ReadFrom(workItem.Reader);

                return true;
            }
        }
    }

    public class InvalidReadException : Exception
    {
        public InvalidReadException(string message) : base(message) { }
    }
}