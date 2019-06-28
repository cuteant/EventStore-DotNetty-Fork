using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace EventStore.Core.TransactionLog.FileNamingStrategy
{
    public class VersionedPatternFileNamingStrategy : IFileNamingStrategy
    {
        private readonly string _path;
        private readonly string _prefix;
        private readonly Regex _chunkNamePattern;

        public VersionedPatternFileNamingStrategy(string path, string prefix)
        {
            if (null == path) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.path); }
            if (null == prefix) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.prefix); }
            _path = path;
            _prefix = prefix;

            _chunkNamePattern = new Regex("^" + _prefix + @"\d{6}\.\w{6}$");
        }

        public string GetFilenameFor(int index, int version)
        {
            if ((uint)index > Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.index); }
            if ((uint)version > Consts.TooBigOrNegative) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.version); }

            return Path.Combine(_path, $"{_prefix}{index:000000}.{version:000000}");
        }

        public string DetermineBestVersionFilenameFor(int index)
        {
            var allVersions = GetAllVersionsFor(index);
            if (0u >= (uint)allVersions.Length)
                return GetFilenameFor(index, 0);
            int lastVersion; var firstVersion = allVersions[0];
            if (!int.TryParse(firstVersion.Substring(firstVersion.LastIndexOf('.') + 1), out lastVersion))
                ThrowHelper.ThrowException_CouldnotDetermineVersionFromFilename(firstVersion);
            return GetFilenameFor(index, lastVersion + 1);
        }

        public string[] GetAllVersionsFor(int index)
        {
            var versions = Directory.EnumerateFiles(_path, $"{_prefix}{index:000000}.*")
                                    .Where(x => _chunkNamePattern.IsMatch(Path.GetFileName(x)))
                                    .OrderByDescending(x => x, StringComparer.CurrentCultureIgnoreCase)
                                    .ToArray();
            return versions;
        }

        public string[] GetAllPresentFiles()
        {
            var versions = Directory.EnumerateFiles(_path, $"{_prefix}*.*")
                                    .Where(x => _chunkNamePattern.IsMatch(Path.GetFileName(x)))
                                    .ToArray();
            return versions;
        }

        public string GetTempFilename()
        {
            return Path.Combine(_path, $"{Guid.NewGuid()}.tmp");
        }

        public string[] GetAllTempFiles()
        {
            return Directory.GetFiles(_path, "*.tmp");
        }
    }
}
