namespace EventStore.Core.Data
{
    public class StreamAcl
    {
        public readonly string[] ReadRoles;
        public readonly string[] WriteRoles;
        public readonly string[] DeleteRoles;
        public readonly string[] MetaReadRoles;
        public readonly string[] MetaWriteRoles;

        public StreamAcl(string readRole, string writeRole, string deleteRole, string metaReadRole, string metaWriteRole)
                : this(readRole is null ? null : new[]{readRole},
                       writeRole is null ? null : new[]{writeRole},
                       deleteRole is null ? null : new[]{deleteRole},
                       metaReadRole is null ? null : new[]{metaReadRole},
                       metaWriteRole is null ? null : new[]{metaWriteRole})
        {
        }

        public StreamAcl(string[] readRoles, string[] writeRoles, string[] deleteRoles, string[] metaReadRoles, string[] metaWriteRoles)
        {
            ReadRoles = readRoles;
            WriteRoles = writeRoles;
            DeleteRoles = deleteRoles;
            MetaReadRoles = metaReadRoles;
            MetaWriteRoles = metaWriteRoles;
        }

        public override string ToString()
        {
            return string.Format("Read: {0}, Write: {1}, Delete: {2}, MetaRead: {3}, MetaWrite: {4}",
                                 ReadRoles is null ? "<null>" : "[" + string.Join(",", ReadRoles) + "]",
                                 WriteRoles is null ? "<null>" : "[" + string.Join(",", WriteRoles) + "]",
                                 DeleteRoles is null ? "<null>" : "[" + string.Join(",", DeleteRoles) + "]",
                                 MetaReadRoles is null ? "<null>" : "[" + string.Join(",", MetaReadRoles) + "]",
                                 MetaWriteRoles is null ? "<null>" : "[" + string.Join(",", MetaWriteRoles) + "]");
        }
    }
}