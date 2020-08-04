using DotNetty.Common.Internal;

namespace EventStore.Transport.Tcp
{
    /// <summary>TBD</summary>
    public readonly struct Disassociated
    {
        /// <summary>TBD</summary>
        public readonly DisassociateInfo Info;

        public readonly string Reason;

        public readonly ClosedConnectionException Error;

        /// <summary>TBD</summary>
        public Disassociated(DisassociateInfo info)
        {
            Info = info;
            Reason = null;
            Error = null;
        }

        /// <summary>TBD</summary>
        public Disassociated(DisassociateInfo info, string reason)
        {
            Info = info;
            Reason = reason;
            Error = null;
        }

        /// <summary>TBD</summary>
        public Disassociated(DisassociateInfo info, ClosedConnectionException error)
        {
            Info = info;
            Reason = error.Message;
            Error = error;
        }

        public override string ToString()
        {
            var sb = StringBuilderManager.Allocate();
            sb.AppendFormat("DisassociateInfo : {0}, ", Info);
            if (!string.IsNullOrEmpty(Reason)) { sb.AppendLine(Reason); }
            if (Error is object) { sb.AppendLine(Error.ToString()); }
            return StringBuilderManager.ReturnAndFree(sb);
        }
    }

    /// <summary>Supertype of possible disassociation reasons</summary>
    public enum DisassociateInfo
    {
        /// <summary>TBD</summary>
        Success = 0,

        /// <summary>TBD</summary>
        Shutdown = 1,

        /// <summary>TBD</summary>
        CodecError = 2,

        /// <summary>TBD</summary>
        InvalidConnection = 3,

        /// <summary>TBD</summary>
        Unknown = 4,
    }
}
