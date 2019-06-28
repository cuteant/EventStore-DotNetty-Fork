﻿using System;
using System.Globalization;
using System.IO;
using EventStore.Common.Utils;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.Monitoring.Stats
{
    public class EsDriveInfo
    {
        ///<summary>
        ///Data storage path
        ///</summary>
        public readonly string DiskName;
        ///<summary>
        ///Total bytes of space available to Event Store
        ///</summary>
        public readonly long TotalBytes;
        ///<summary>
        ///Remaining bytes of space available to Event Store
        ///</summary>
        public readonly long AvailableBytes;
        ///<summary>
        ///Total bytes of space used by Event Store
        ///</summary>
        public readonly long UsedBytes;
        ///<summary>
        ///Percentage usage of space used by Event Store
        ///</summary>
        public readonly string Usage;

        public static EsDriveInfo FromDirectory(string path, ILogger log)
        {
            try
            {
                if (OS.IsUnix)
                {
                    return GetEsDriveInfoUnix(path, log);
                }

                var driveName = Directory.GetDirectoryRoot(path);
                var drive = new DriveInfo(driveName);
                var esDrive = new EsDriveInfo(drive.Name, drive.TotalSize, drive.AvailableFreeSpace);
                return esDrive;
            }
            catch (Exception ex)
            {
                if (log.IsDebugLevelEnabled()) log.Error_while_reading_drive_info_for_path(path, ex);
                return null;
            }
        }

        private EsDriveInfo(string diskName, long totalBytes, long availableBytes)
        {
            DiskName = diskName;
            TotalBytes = totalBytes;
            AvailableBytes = availableBytes;
            UsedBytes = TotalBytes - AvailableBytes;
            Usage = TotalBytes != 0
                    ? (UsedBytes * 100 / TotalBytes).ToString(CultureInfo.InvariantCulture) + "%"
                    : "0%";
        }

        private static EsDriveInfo GetEsDriveInfoUnix(string directory, ILogger log)
        {
            // http://unix.stackexchange.com/questions/11311/how-do-i-find-on-which-physical-device-a-folder-is-located

            // example

            // Filesystem     1K-blocks      Used Available Use% Mounted on
            // /dev/sda1      153599996 118777100  34822896  78% /media/CC88FD3288FD1C20

            try
            {
                if (!Directory.Exists(directory)) return null;
                var driveInfo = ShellExecutor.GetOutput("df", $"-P {directory}");
                var driveInfoLines = driveInfo.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (0u >= (uint)driveInfoLines.Length) return null;
                var ourline = driveInfoLines[1];
                var trimmedLine = SystemStatsHelper.SpacesRegex.Replace(ourline, " ");
                var info = trimmedLine.Split(' ');

                var totalBytes = long.Parse(info[1].Trim()) * 1024; // the '1024-blocks' column
                var availableBytes = long.Parse(info[3].Trim()) * 1024; // the 'Available' column
                var mountPoint = info[5];                    // the 'Mounted on' column

                return new EsDriveInfo(mountPoint, totalBytes, availableBytes);
            }
            catch (Exception ex)
            {
                if (log.IsDebugLevelEnabled()) log.Could_not_get_drive_name_for_directory(ex, directory);
                return null;
            }
        }
    }
}
