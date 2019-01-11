namespace EventStore.ClientAPI.UserManagement
{
    internal class ChangePasswordDetails
    {
        public readonly string CurrentPassword;

        public readonly string NewPassword;

        public ChangePasswordDetails(string currentPassword, string newPassword)
        {
            if (string.IsNullOrEmpty(currentPassword)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.currentPassword); }
            if (string.IsNullOrEmpty(newPassword)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.newPassword); }
            CurrentPassword = currentPassword;
            NewPassword = newPassword;
        }
    }
}
