using System;
using System.IO;
using Microsoft.Extensions.Logging;
#if DESKTOPCLR
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
#endif

namespace EventStore.Common.Utils
{
    public static class FileStreamExtensions
    {
        private static readonly ILogger Log = TraceLogger.GetLogger(typeof(FileStreamExtensions));
        private static Action<FileStream> FlushSafe;
#if DESKTOPCLR
        private static Func<FileStream, SafeFileHandle> GetFileHandle;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlushFileBuffers(SafeFileHandle hFile);

        //[DllImport("kernel32.dll", SetLastError = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool FlushViewOfFile(IntPtr lpBaseAddress, UIntPtr dwNumberOfBytesToFlush);
#endif

        static FileStreamExtensions()
        {
            ConfigureFlush(disableFlushToDisk: false);
        }

        public static void FlushToDisk(this FileStream fs)
        {
            FlushSafe(fs);
        }

        public static void ConfigureFlush(bool disableFlushToDisk)
        {
            if (disableFlushToDisk)
            {
                if (Log.IsInformationLevelEnabled()) Log.LogInformation("FlushToDisk: DISABLED");
                FlushSafe = f => f.Flush(flushToDisk: false);
                return;
            }

#if NETSTANDARD
            FlushSafe = f => f.Flush(flushToDisk: true);
#else
            try
            {
                ParameterExpression arg = Expression.Parameter(typeof(FileStream), "f");
                Expression expr = Expression.Field(arg, typeof(FileStream).GetField("_handle", BindingFlags.Instance | BindingFlags.NonPublic));
                GetFileHandle = Expression.Lambda<Func<FileStream, SafeFileHandle>>(expr, arg).Compile();
                FlushSafe = f =>
                {
                    f.Flush(flushToDisk: false);
                    if (!FlushFileBuffers(GetFileHandle(f)))
                        throw new Exception(string.Format("FlushFileBuffers failed with err: {0}", Marshal.GetLastWin32Error()));
                };
            }
            catch (Exception exc)
            {
                Log.LogError(exc, "Error while compiling sneaky SafeFileHandle getter.");
                FlushSafe = f => f.Flush(flushToDisk: true);
            }
#endif
        }
    }
}
