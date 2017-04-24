﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using EventStore.Common.Utils;
using EventStore.Core.Util;

namespace EventStore.Core
{
  public class ExclusiveDbLock
  {
    private static readonly ILogger Log = TraceLogger.GetLogger<ExclusiveDbLock>();

    public readonly string MutexName;
    public bool IsAcquired { get { return _acquired; } }

    private Mutex _dbMutex;
    private bool _acquired;

    public ExclusiveDbLock(string dbPath)
    {
      Ensure.NotNullOrEmpty(dbPath, nameof(dbPath));
      MutexName = dbPath.Length <= 250 ? "ESDB:" + dbPath.Replace('\\', '/') : "ESDB-HASHED:" + GetDbPathHash(dbPath);
      MutexName += new string('-', 260 - MutexName.Length);
    }

    public bool Acquire()
    {
      if (_acquired) throw new InvalidOperationException($"DB mutex '{MutexName}' is already acquired.");

      try
      {
        _dbMutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out _acquired);
      }
      catch (AbandonedMutexException exc)
      {
        if (Log.IsInformationLevelEnabled())
        {
          Log.LogInformation(exc,
                            "DB mutex '{0}' is said to be abandoned. "
                            + "Probably previous instance of server was terminated abruptly.",
                            MutexName);
        }
      }

      return _acquired;
    }

    private string GetDbPathHash(string dbPath)
    {
      using (var memStream = new MemoryStream(Helper.UTF8NoBom.GetBytes(dbPath)))
      {
        return BitConverter.ToString(MD5Hash.GetHashFor(memStream)).Replace("-", "");
      }
    }

    public void Release()
    {
      if (!_acquired) throw new InvalidOperationException($"DB mutex '{MutexName}' was not acquired.");
      _dbMutex.ReleaseMutex();
    }
  }
}