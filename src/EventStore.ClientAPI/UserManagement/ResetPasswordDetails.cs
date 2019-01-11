namespace EventStore.ClientAPI.UserManagement
{
    internal class ResetPasswordDetails
    {
        public readonly string NewPassword;

        public ResetPasswordDetails(string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.newPassword); }
            NewPassword= newPassword;
        }
    }
}
