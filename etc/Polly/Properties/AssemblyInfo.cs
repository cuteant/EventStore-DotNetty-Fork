using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("Polly")]
[assembly: AssemblyProduct("Polly")]
[assembly: AssemblyCompany("App vNext")]
[assembly: AssemblyDescription("Polly is a library that allows developers to express resilience and transient fault handling policies such as Retry, Circuit Breaker, Timeout, Bulkhead Isolation, and Fallback in a fluent and thread-safe manner.")]
[assembly: AssemblyCopyright("Copyright (c) 2016, App vNext")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyVersion("5.0.0.0")]
#if NETSTANDARD1_0 || NET40
[assembly: AssemblyFileVersion("5.0.0.0")]
#elif NETSTANDARD1_1 || WINDOWS8 || NET45 || NETCORE45
[assembly: AssemblyFileVersion("5.0.1000.0")]
#elif NETSTANDARD1_2 || WINDOWS81 || NET451 || NETCORE451 || WPA81
[assembly: AssemblyFileVersion("5.0.2000.0")]
#elif NETSTANDARD1_3 || NET46
[assembly: AssemblyFileVersion("5.0.3000.0")]
#elif NETSTANDARD1_4 || UAP10_0 || NETCORE50 || NET461
[assembly: AssemblyFileVersion("5.0.4000.0")]
#elif NETSTANDARD1_5 || NET462
[assembly: AssemblyFileVersion("5.0.5000.0")]
#elif NETSTANDARD1_6 || NETCOREAPP1_0 || NET463
[assembly: AssemblyFileVersion("5.0.6000.0")]
#else // this is here to prevent the build system from complaining. It should never be hit
[assembly: AssemblyFileVersion("5.0.9000.0")]
#endif

[assembly: CLSCompliant(true)]

//[assembly: InternalsVisibleTo("Polly.Net45.Specs")]
[assembly: InternalsVisibleTo("Polly.Specs, PublicKey=00240000048000009400000006020000002400005253413100040000010001003df2cefc3e3c196195f046768979f5998131a23270da7485c84d0e46175140c4227e93fe392829d51d1e1ffbe0d6edb3bb0b2b05556f829f2f1a184f23ce052e2b2134ba0ae7aa9143a7959cea16accb18d1417bf48dabac10c2c0828ede943c5960e85713ca29eea555959ea6dbdd41d1000bf62da370883c4dc5c3508a22df")]
