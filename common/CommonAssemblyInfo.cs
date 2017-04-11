using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("Event Store LLP")]
[assembly: AssemblyProduct("EventStore")]
[assembly: AssemblyCopyright("Copyright © Event Store LLP. All rights reserved.")]
[assembly: AssemblyTrademark("cuteant@outlook.com")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
[assembly: AssemblyMetadata("Serviceable", "True")]
[assembly: NeutralResourcesLanguage("en-US")]

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
