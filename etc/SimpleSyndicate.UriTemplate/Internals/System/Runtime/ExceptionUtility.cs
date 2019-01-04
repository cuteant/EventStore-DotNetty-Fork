namespace System.Runtime
{
    public class ExceptionUtility
    {
        public ArgumentException Argument(string paramName, string message)
        {
            return new ArgumentException(message, paramName);
        }

        public ArgumentNullException ArgumentNull(string paramName)
        {
            return new ArgumentNullException(paramName);
        }

        public ArgumentException ArgumentNullOrEmpty(string paramName)
        {
            return this.Argument(paramName, "ArgumentNullOrEmpty");
        }

        public ArgumentException ThrowHelperArgument(string paramName, string message)
        {
            return (ArgumentException)this.ThrowHelperError(new ArgumentException(message, paramName));
        }

        public ArgumentException ThrowHelperArgumentNull(string paramName)
        {
            return (ArgumentNullException)this.ThrowHelperError(new ArgumentNullException(paramName));
        }

        public Exception ThrowHelperError(Exception exception)
        {
            return exception;
        }
    }
}
