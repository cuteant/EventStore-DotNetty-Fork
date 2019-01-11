namespace EventStore.ClientAPI.UserManagement
{
    internal class UserUpdateInformation
    {
        public readonly string FullName;

        public readonly string[] Groups;

        public UserUpdateInformation(string fullName, string[] groups)
        {
            if (string.IsNullOrEmpty(fullName)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.fullName); }
            if (null == groups) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.groups); }
            FullName = fullName;
            Groups = groups;
        }
    }
}