using System.Reflection;
using System.Runtime.InteropServices;

[assembly: NUnit.Framework.Category("All")]
#if DESKTOPCLR
[assembly: NUnit.Framework.Timeout(30 * 60 * 1000)]
#endif

