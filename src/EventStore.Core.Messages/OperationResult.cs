using System;

namespace EventStore.Core.Messages
{
    public enum OperationResult
    {

        Success = 0,

        PrepareTimeout = 1,

        CommitTimeout = 2,

        ForwardTimeout = 3,

        WrongExpectedVersion = 4,

        StreamDeleted = 5,

        InvalidTransaction = 6,

        AccessDenied = 7
    }
}
