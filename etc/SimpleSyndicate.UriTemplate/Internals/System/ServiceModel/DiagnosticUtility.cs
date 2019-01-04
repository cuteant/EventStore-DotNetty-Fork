using System.Runtime;

namespace System.ServiceModel
{
    public static class DiagnosticUtility
    {
        private static readonly ExceptionUtility s_exceptionUtility = new ExceptionUtility();

        public static ExceptionUtility ExceptionUtility { get { return s_exceptionUtility; } }
    }
}
