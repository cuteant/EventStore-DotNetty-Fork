using System;
using System.Runtime.CompilerServices;
using EventStore.Transport.Http.Atom;

namespace EventStore.Transport.Http
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        id,
        title,
        name,
        url,
        uri,
        request,
        response,
        summary,
        onSuccess,
        componentText,
        onException,
        contentType,
        body,
        onError,
        onReadSuccess,
        entry,
        type,
        input,
        output,
        onCompleted,
        prefixes,
        outputStream,
        inputStream,
        onRequestSatisfied,
        allowedMethods,
        httpEntity,
        codec,
        collection,
        workspace,
    }

    #endregion

    #region -- ExceptionResource --

    /// <summary>The convention for this enum is using the resource name as the enum name</summary>
    internal enum ExceptionResource
    {
        An_appservice_element_MUST_contain_one_or_more_appworkspace_elements,
        The_appworkspace_element_MUST_contain_one_atomtitle_element,
        The_appcollection_element_MUST_contain_one_atomtitle_element,
        The_appcollection_element_MUST_contain_an_href_attribute,
        atomaccept_element_MUST_contain_value,
        Person_constructs_MUST_contain_exactly_one_atomname_element,
        atomlink_elements_MUST_have_an_href_attribute,
        atomentry_elements_MUST_contain_an_atomsummary_element,
        atomentry_elements_MUST_contain_one_or_more_atomauthor_elements,
        atomentry_elements_MUST_contain_exactly_one_atomupdated_element,
        atomentry_elements_MUST_contain_exactly_one_atomid_element,
        atomentry_elements_MUST_contain_exactly_one_atomtitle_element,
        atomfeed_elements_MUST_contain_exactly_one_atomtitle_element,
        atomfeed_elements_MUST_contain_exactly_one_atomid_element,
        atomfeed_elements_MUST_contain_exactly_one_atomupdated_element,
        atomfeed_elements_MUST_contain_one_or_more_atomauthor_elements,
        atomfeed_elements_SHOULD_contain_one_atomlink_element_with_a_rel_attribute_value_of_self,
    }

    #endregion

    partial class ThrowHelper
    {
        #region -- Exception --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_ThisShouldNeverHappen()
        {
            throw GetException();

            static Exception GetException()
            {
                return new Exception("This should never happen!");
            }
        }

        #endregion

        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_CustomCodec_ContentType()
        {
            throw GetException();

            static ArgumentException GetException()
            {
                return new ArgumentException("contentType", "contentType");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_MediaType_ComponentText()
        {
            throw GetException();

            static ArgumentException GetException()
            {
                return new ArgumentException("componentText", "componentText");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_NotEmptyGuid(ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} should be non-empty GUID.", argumentName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Equal(int expected, int actual, ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Equal(long expected, long actual, ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Equal(bool expected, bool actual, ExceptionArgument argument)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentException($"{argumentName} expected value: {expected}, actual value: {actual}");
            }
        }

        #endregion

        #region -- ArgumentOutOfRangeException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_Positive(ExceptionArgument argument)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentOutOfRangeException(argumentName, $"{argumentName} should be positive.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument argument)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                var argumentName = GetArgumentName(argument);
                return new ArgumentOutOfRangeException(argumentName, $"{argumentName} should be non negative.");
            }
        }

        #endregion

        #region -- AtomSpecificationViolationException --

        public static void ThrowSpecificationViolation(ExceptionResource resource)
        {
            throw GetException();
            AtomSpecificationViolationException GetException()
            {
                return new AtomSpecificationViolationException(GetResourceString(resource)); ;
            }
        }

        #endregion

        #region -- InvalidOperationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException()
        {
            throw GetException();

            static InvalidOperationException GetException()
            {
                return new InvalidOperationException();
            }
        }

        #endregion
    }
}
