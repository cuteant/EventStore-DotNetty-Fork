# EventStore .NETCore Fork

This is a fork from EventStore project: https://github.com/eventstore/eventstore

## ~ Features
  - Very simple deployment of a Windows service:[ClusterNode.WindowsServices](https://github.com/cuteant/EventStore-NETCore-Fork/tree/dev-netcore/src/EventStore.ClusterNode.WindowsServices) [ClusterNode.DotNetCore.WindowsServices](https://github.com/cuteant/EventStore-NETCore-Fork/tree/dev-netcore/src/EventStore.ClusterNode.DotNetCore.WindowsServices)
  - Using [TPL Dataflow](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library) for building asynchronous event data processing pipelines.
  - More efficient serialization(see https://github.com/cuteant/CuteAnt.Serialization).
  - Object-Pooling.
  - Faster Reflection.
  - More friendly and more intuitive api for EventStore-Client.
  - Better error handling with exceptions and better server connection detect.
  - Auto Subscriber.
  - Some Good Ideas for event subscriptions from [EasyNetQ](https://github.com/EasyNetQ/EasyNetQ).

# EasyEventStore

A Nice .NET Client API for EventStore.

Goals:

1. To make working with Event Store on .NET as easy as possible.
2. To build an API that is close to interchangable with EasyNetQ.

## [Samples](https://github.com/cuteant/EventStore-NETCore-Fork/tree/dev-netcore/samples)

To publish with EasyEventStore (assuming you've already created an IEventStoreBus instance):

1. Create an instance of your message, it can be any serializable .NET type.
2. Call the Publish method on IBus passing it your message instance.

Here's the code...

    var message = new MyMessage { Text = "Hello EventStore" };
    bus.PublishEventAsync(message);

To subscribe to a message we need to give EasyEventStore an action to perform whenever a message arrives. We do this by passing subscribe a delegate:

    bus.VolatileSubscribe<MyMessage>((sub, e) => Console.WriteLine(e.Body.Text));
    bus.CatchUpSubscribe<MyMessage>((sub, e) => Console.WriteLine(e.Body.Text));
    bus.PersistentSubscribe<MyMessage>((sub, e) => Console.WriteLine(e.Body.Text));

Now every time that an instance of MyMessage is published, EasyEventStore will call our delegate and print the message's Text property to the console.

To send a message, use the Send method on IEventStoreBus, specifying the name of the stream you wish to send the message to and the message itself:

    bus.SendEventAsync("my.stream", new MyMessage{ Text = "Hello Widgets!" });

To setup a message receiver for a particular message type, use the Receive method on IEventStoreBus:

    bus.VolatileSubscribe("my.stream", (sub, e) => Console.WriteLine("MyMessage: {0}", ((MyMessage)e.Body).Text));

You can set up multiple receivers for different message types on the same queue by using the Receive overload that takes an Action&lt;IHandlerRegistration&gt;, for example:

    bus.VolatileSubscribeAsync("my.stream", settings,
        addHandlers: _ =>_
        .Add<MyMessage>(message => deliveredMyMessage = message)
        .Add<MyOtherMessage>(message => deliveredMyOtherMessage = message);

# ~ ORIGINAL README ~

## Event Store

The open-source, functional database with Complex Event Processing in JavaScript.

This is the repository for the open source version of Event Store, which includes the clustering implementation for high availability. 

## Support

Information on commercial support and options such as LDAP authentication can be found on the Event Store website at https://eventstore.org/support.

## CI Status
[![Build Status](https://dev.azure.com/EventStoreOSS/EventStore/_apis/build/status/EventStore.EventStore?branchName=master)](https://dev.azure.com/EventStoreOSS/EventStore/_build/latest?definitionId=2)

## Documentation
Documentation for Event Store can be found [here](https://eventstore.org/docs/)

## Community
We have a fairly active [google groups list](https://groups.google.com/forum/#!forum/event-store). If you prefer slack, there is also an #eventstore channel [here](http://ddd-cqrs-es.herokuapp.com/).

## Release Packages
The latest release packages are hosted in the downloads section on the [Event Store Website](https://eventstore.org/downloads/)

We also host native packages for Linux on [Package Cloud](https://packagecloud.io/EventStore/EventStore-OSS) and Windows packages can be installed via [Chocolatey](https://chocolatey.org/packages/eventstore-oss) (4.0.0 onwards only).

## Building Event Store

Event Store is written in a mixture of C#, C++ and JavaScript. It can run either on Mono or .NET, however because it contains platform specific code (including hosting the V8 JavaScript engine), it must be built for the platform on which you intend to run it.

### Linux
**Prerequisites**
- [Mono 5.16.0](https://www.mono-project.com/download/)
- [.NET Core SDK 2.1.402](https://www.microsoft.com/net/download)

**Required Environment Variables**
```
export FrameworkPathOverride=/usr/lib/mono/4.7-api
```

### Windows
**Prerequisites**
- [.NET Framework 4.7 (Developer Pack)](https://www.microsoft.com/net/download)
- [.NET Core SDK 2.1.402](https://www.microsoft.com/net/download)

### Mac OS X
**Prerequisites**
- [Mono 5.16.0](https://www.mono-project.com/download/)
- [.NET Core SDK 2.1.402](https://www.microsoft.com/net/download)

**Required Environment Variables**
```
export FrameworkPathOverride=/Library/Frameworks/Mono.framework/Versions/5.16.0/lib/mono/4.7-api/
```

### Build EventStore
Once you've installed the prerequisites for your system, you can launch a `Release` build of EventStore as follows:
```
dotnet build -c Release src/EventStore.sln
```

To start a single node, you can then run:
```
bin/Release/EventStore.ClusterNode/net47/EventStore.ClusterNode.exe --db ../db --log ../logs
```

You'll need to launch the node with `mono` on Linux or Mac OS X.

_Note: The build system has changed after version `4.1.1-hotfix1`, therefore the above instructions will not work for old releases._

### Running the tests
You can launch the tests as follows:

#### EventStore Core tests
```
dotnet test src/EventStore.Core.Tests/EventStore.Core.Tests.csproj -- RunConfiguration.TargetPlatform=x64
```

#### EventStore Projections tests
```
dotnet test src/EventStore.Projections.Core.Tests/EventStore.Projections.Core.Tests.csproj -- RunConfiguration.TargetPlatform=x64
```

## Building the EventStore web UI
The web UI is prebuilt and the files are located under [src/EventStore.ClusterNode.Web/clusternode-web](src/EventStore.ClusterNode.Web/clusternode-web).
If you want to build the web UI, please consult this [repository](https://github.com/EventStore/EventStore.UI) which is also a git submodule of the current repository located under `src/EventStore.UI`.

## Building the Projections Library
The list of precompiled projections libraries can be found in `src/libs/x64`. If you still want to build the projections library please follow the links below.
- [Linux](scripts/build-js1/build-js1-linux/README.md)
- [Windows](scripts/build-js1/build-js1-win/build-js1-win-instructions.md)
- [Mac OS X](scripts/build-js1/build-js1-mac/build-js1-mac.sh)

## Contributing

Development is done on the `master` branch.
We attempt to do our best to ensure that the history remains clean and to do so, we generally ask contributors to squash their commits into a set or single logical commit.

If you want to switch to a particular release, you can check out the tag for this particular version. For example:  
`git checkout oss-v4.1.0`