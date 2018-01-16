﻿using System;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.ClientOperations
{
    internal interface IClientOperation
    {
        TcpPackage CreateNetworkPackage(Guid correlationId);
        InspectionResult InspectPackage(in TcpPackage package);
        void Fail(Exception exception);
    }

    internal interface ISubscription: IClientOperation
    {
        
    }
}