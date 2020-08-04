using System;
using System.IO;

namespace EventStore.Transport.Http.EntityManagement
{
    internal class ManagerOperationState: IDisposable
    {
        public readonly Action<HttpEntityManager, byte[]> OnReadSuccess;
        public readonly Action<Exception> OnError;

        public readonly Stream InputStream;
        public readonly Stream OutputStream;

        public ManagerOperationState(Stream inputStream,
                                     Stream outputStream,
                                     Action<HttpEntityManager, byte[]> onReadSuccess, 
                                     Action<Exception> onError)
        {
            if (inputStream is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inputStream); }
            if (outputStream is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outputStream); }
            if (onReadSuccess is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onReadSuccess); }
            if (onError is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onError); }

            InputStream = inputStream;
            OutputStream = outputStream;
            OnReadSuccess = onReadSuccess;
            OnError = onError;
        }

        public void Dispose()
        {
            IOStreams.SafelyDispose(InputStream, OutputStream);
        }
    }
}