using System;
using System.IO;
using System.Security.Cryptography;
using EventStore.Common.Utils;

namespace EventStore.Core.Util
{
    public class MD5Hash
    {
        public static byte[] GetHashFor(Stream s)
        {
            //when using this, it will calculate from this point to the END of the stream!
            using (MD5 md5 = MD5.Create())
                return md5.ComputeHash(s);
        }

        public static byte[] GetHashFor(Stream s, int startPosition, long count)
        {
            if ((ulong)count > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.count); }

            using (MD5 md5 = MD5.Create())
            {
                ContinuousHashFor(md5, s, startPosition, count);
                md5.TransformFinalBlock(Empty.ByteArray, 0, 0);
                return md5.Hash;
            }
        }

        public static void ContinuousHashFor(MD5 md5, Stream s, int startPosition, long count)
        {
            if (null == md5) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.md5); }
            if ((ulong)count > Consts.TooBigOrNegativeUL) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.count); }

            if (s.Position != startPosition)
                s.Position = startPosition;

            var buffer = new byte[4096];
            long toRead = count;
            while (toRead > 0)
            {
                int read = s.Read(buffer, 0, (int)Math.Min(toRead, buffer.Length));
                if (0u >= (uint)read)
                    break;

                md5.TransformBlock(buffer, 0, read, null, 0);
                toRead -= read;
            }
        }
    }
}