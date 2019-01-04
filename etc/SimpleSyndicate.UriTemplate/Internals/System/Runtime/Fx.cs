//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
namespace System.Runtime
{
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Threading;

    static class Fx
    {
        private static ExceptionUtility s_exceptionUtility = new ExceptionUtility();

        public static ExceptionUtility Exception { get { return s_exceptionUtility; } }
    
        // Do not call the parameter "message" or else FxCop thinks it should be localized.
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string description)
        {
            if (!condition)
            {
                Assert(description);
            }
        }

        [Conditional("DEBUG")]
        public static void Assert(string description)
        {
            Debug.Assert(false, description);
        }
    }
}
