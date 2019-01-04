using System.Reflection;
using System.Runtime.InteropServices;

[assembly: NUnit.Framework.NonParallelizable()]
#if DESKTOPCLR
[assembly: NUnit.Framework.Timeout(2 * 60 * 1000)]
#endif
