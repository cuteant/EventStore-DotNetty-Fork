using System;
using System.Runtime.CompilerServices;

namespace EventStore.Transport.Tcp.Messages
{
    [Flags]
    public enum TcpFlags : byte
    {
        None = 0x00,
        Authenticated = 0x01,
        TrustedWrite = 0x02
    }

    public sealed class TcpPackage
    {
        public readonly TcpCommand Command;
        public readonly TcpFlags Flags;
        public readonly Guid CorrelationId;
        public readonly string Login;
        public readonly string Password;
        public readonly byte[] Data;

        public int Length => Data is object ? Data.Length + 18 : 18; // ignore login/pwd

        public TcpPackage() { }

        public TcpPackage(TcpCommand command, Guid correlationId, byte[] data)
            : this(command, TcpFlags.None, correlationId, null, null, data)
        {
        }

        public TcpPackage(TcpCommand command, TcpFlags flags, Guid correlationId, string login, string password, byte[] data)
        {
            if ((flags & TcpFlags.Authenticated) != 0)
            {
                if (null == login) { ThrowArgumentNullException_Login(); }
                if (password is null) { ThrowArgumentNullException_Password(); }
            }
            else
            {
                if (login is object) { ThrowArgumentException_Login(); }
                if (password is object) { ThrowArgumentException_Password(); }
            }

            Command = command;
            Flags = flags;
            CorrelationId = correlationId;
            Login = login;
            Password = password;
            Data = data;
        }

        #region ** Throw helper **

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentException_Login()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Login provided for non-authorized TcpPackage.", "login");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentException_Password()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Password provided for non-authorized TcpPackage.", "password");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentNullException_Login()
        {
            throw GetException();
            ArgumentNullException GetException()
            {
                return new ArgumentNullException("login");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentNullException_Password()
        {
            throw GetException();
            ArgumentNullException GetException()
            {
                return new ArgumentNullException("password");
            }
        }

        #endregion
    }
}
