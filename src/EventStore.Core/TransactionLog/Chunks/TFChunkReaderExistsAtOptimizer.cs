﻿using System;
using EventStore.Core.DataStructures;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.TransactionLog.Chunks
{
    public class TFChunkReaderExistsAtOptimizer
    {
        private static TFChunkReaderExistsAtOptimizer _instance;
        public static TFChunkReaderExistsAtOptimizer Instance
        {
            get
            {
                // we don't want two of these things so lock to make sure...
                if (_instance is null)
                    lock (typeof(TFChunkReaderExistsAtOptimizer))
                        if (_instance is null)
                            _instance = new TFChunkReaderExistsAtOptimizer(MaxBloomFiltersCached);

                return _instance;
            }
        }

        //Least-Recently-Used cache to keep track of scavenged TFChunks that have cached bloom filters
        private readonly ILRUCache<string, TFChunk.TFChunk> _cache;
        private const int MaxBloomFiltersCached = 10000; //around 5GB RAM max if we consider 200,000 log positions/chunk and 20 bits/log position
        private static readonly ILogger Log = TraceLogger.GetLogger<TFChunkReaderExistsAtOptimizer>();


        public TFChunkReaderExistsAtOptimizer(int maxCached)
        {
            Func<object, bool> onPut = (o) =>
            {
                var chunk = (TFChunk.TFChunk)o;
                if (chunk is null) { return false; }
#if DEBUG
                if (Log.IsDebugLevelEnabled()) Log.Optimizing_chunk_for_fast_merge(chunk.FileName);
#endif
                chunk.OptimizeExistsAt();
                return true;
            };

            Func<object, bool> onRemove = (o) =>
            {
                var chunk = (TFChunk.TFChunk)o;
                if (chunk is null) { return false; }
#if DEBUG
                if (Log.IsDebugLevelEnabled()) Log.Clearing_fast_merge_optimizations_from_chunk(chunk.FileName);
#endif
                chunk.DeOptimizeExistsAt();
                return true;
            };

            _cache = new LRUCache<string, TFChunk.TFChunk>(maxCached, onPut, onRemove);
        }

        public void Optimize(TFChunk.TFChunk chunk)
        {
            if (!chunk.ChunkHeader.IsScavenged) return;
            _cache.Put(chunk.FileName, chunk);
        }

        public bool IsOptimized(TFChunk.TFChunk chunk)
        {
            TFChunk.TFChunk value;
            return _cache.TryGet(chunk.FileName, out value);
        }

        public void DeOptimizeAll()
        {
            _cache.Clear();
        }
    }
}