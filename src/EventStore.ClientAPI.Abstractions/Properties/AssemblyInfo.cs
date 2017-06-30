using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("EventStore.ClientAPI.Abstractions")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyDescription("EventStore.ClientAPI.Abstractions (Flavor=Debug)")]
#else
[assembly: AssemblyConfiguration("Retail")]
[assembly: AssemblyDescription("EventStore.ClientAPI.Abstractions (Flavor=Retail)")]
#endif
[assembly: AssemblyCompany("CuteAnt Development Team(cuteant@outlook.com)")]
[assembly: AssemblyProduct("EventStore.ClientAPI")]
[assembly: AssemblyCopyright("Copyright (c) 2000-2017 CuteAnt Development Team")]
[assembly: AssemblyTrademark("cuteant@outlook.com")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyMetadata("Serviceable", "True")]
[assembly: NeutralResourcesLanguage("en-US")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("CD293731-28B9-4FAB-ACF9-9AD0AD7394ED")]
[assembly: AssemblyVersion("4.0.0.0")]
#if NETSTANDARD1_0 || NET40
[assembly: AssemblyFileVersion("4.0.0.0")]
#elif NETSTANDARD1_1 || WINDOWS8 || NET45 || NETCORE45
[assembly: AssemblyFileVersion("4.0.1000.0")]
#elif NETSTANDARD1_2 || WINDOWS81 || NET451 || NETCORE451 || WPA81
[assembly: AssemblyFileVersion("4.0.2000.0")]
#elif NETSTANDARD1_3 || NET46
[assembly: AssemblyFileVersion("4.0.3000.0")]
#elif NETSTANDARD1_4 || UAP10_0 || NETCORE50 || NET461
[assembly: AssemblyFileVersion("4.0.4000.0")]
#elif NETSTANDARD1_5 || NET462
[assembly: AssemblyFileVersion("4.0.5000.0")]
#elif NETSTANDARD1_6 || NETCOREAPP1_0 || NET463
[assembly: AssemblyFileVersion("4.0.6000.0")]
#else // this is here to prevent the build system from complaining. It should never be hit
[assembly: AssemblyFileVersion("4.0.9000.0")]
#endif

#if SIGNED
[assembly: InternalsVisibleTo("EventStore.ClientAPI, PublicKey=00240000048000009400000006020000002400005253413100040000010001003df2cefc3e3c196195f046768979f5998131a23270da7485c84d0e46175140c4227e93fe392829d51d1e1ffbe0d6edb3bb0b2b05556f829f2f1a184f23ce052e2b2134ba0ae7aa9143a7959cea16accb18d1417bf48dabac10c2c0828ede943c5960e85713ca29eea555959ea6dbdd41d1000bf62da370883c4dc5c3508a22df")]
[assembly: InternalsVisibleTo("EventStore.ClientAPI.Consumer, PublicKey=00240000048000009400000006020000002400005253413100040000010001003df2cefc3e3c196195f046768979f5998131a23270da7485c84d0e46175140c4227e93fe392829d51d1e1ffbe0d6edb3bb0b2b05556f829f2f1a184f23ce052e2b2134ba0ae7aa9143a7959cea16accb18d1417bf48dabac10c2c0828ede943c5960e85713ca29eea555959ea6dbdd41d1000bf62da370883c4dc5c3508a22df")]
[assembly: InternalsVisibleTo("EventStore.ClientAPI.Embedded, PublicKey=00240000048000009400000006020000002400005253413100040000010001003df2cefc3e3c196195f046768979f5998131a23270da7485c84d0e46175140c4227e93fe392829d51d1e1ffbe0d6edb3bb0b2b05556f829f2f1a184f23ce052e2b2134ba0ae7aa9143a7959cea16accb18d1417bf48dabac10c2c0828ede943c5960e85713ca29eea555959ea6dbdd41d1000bf62da370883c4dc5c3508a22df")]
[assembly: InternalsVisibleTo("EventStore.Core.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001003df2cefc3e3c196195f046768979f5998131a23270da7485c84d0e46175140c4227e93fe392829d51d1e1ffbe0d6edb3bb0b2b05556f829f2f1a184f23ce052e2b2134ba0ae7aa9143a7959cea16accb18d1417bf48dabac10c2c0828ede943c5960e85713ca29eea555959ea6dbdd41d1000bf62da370883c4dc5c3508a22df")]
[assembly: InternalsVisibleTo("EventStore.ClientAPI.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001003df2cefc3e3c196195f046768979f5998131a23270da7485c84d0e46175140c4227e93fe392829d51d1e1ffbe0d6edb3bb0b2b05556f829f2f1a184f23ce052e2b2134ba0ae7aa9143a7959cea16accb18d1417bf48dabac10c2c0828ede943c5960e85713ca29eea555959ea6dbdd41d1000bf62da370883c4dc5c3508a22df")]
#else
[assembly: InternalsVisibleTo("EventStore.ClientAPI")]
[assembly: InternalsVisibleTo("EventStore.ClientAPI.Consumer")]
[assembly: InternalsVisibleTo("EventStore.ClientAPI.Embedded")]
[assembly: InternalsVisibleTo("EventStore.Core.Tests")]
[assembly: InternalsVisibleTo("EventStore.ClientAPI.Tests")]
#endif