using System;
using System.Collections.Generic;
using System.Text;

namespace System.ServiceModel
{
    public static class SR
    {
        public static string ObjectIsReadOnly = "ObjectIsReadOnly";

        public static string UTQueryCannotEndInAmpersand = "UTQueryCannotEndInAmpersand";

        public static string UTQueryCannotHaveEmptyName = "UTQueryCannotHaveEmptyName";

        public static string UTQueryMustHaveLiteralNames = "UTQueryMustHaveLiteralNames";

        public static string UTQueryNamesMustBeUnique = "UTQueryNamesMustBeUnique";

        public static string UTAdditionalDefaultIsInvalid = "UTAdditionalDefaultIsInvalid";

        public static string UTDefaultValueToQueryVarFromAdditionalDefaults = "UTDefaultValueToQueryVarFromAdditionalDefaults";

        public static string UTNullableDefaultAtAdditionalDefaults = "UTNullableDefaultAtAdditionalDefaults";

        public static string UTBadBaseAddress = "UTBadBaseAddress";

        public static string UTBindByPositionNoVariables = "UTBindByPositionNoVariables";

        public static string UTBothLiteralAndNameValueCollectionKey = "UTBothLiteralAndNameValueCollectionKey";

        public static string UTBindByNameCalledWithEmptyKey = "UTBindByNameCalledWithEmptyKey";

        public static string UTDefaultValuesAreImmutable = "UTDefaultValuesAreImmutable";

        public static string UTStarVariableWithDefaultsFromAdditionalDefaults = "UTStarVariableWithDefaultsFromAdditionalDefaults";

        public static string UTDefaultValueToCompoundSegmentVarFromAdditionalDefaults = "UTDefaultValueToCompoundSegmentVarFromAdditionalDefaults";

        public static string UTInvalidWildcardInVariableOrLiteral = "UTInvalidWildcardInVariableOrLiteral";

        public static string UTVarNamesMustBeUnique = "UTVarNamesMustBeUnique";

        public static string UTInvalidDefaultPathValue = "UTInvalidDefaultPathValue";

        public static string UTDefaultValueToQueryVar = "UTDefaultValueToQueryVar";

        public static string UTBindByPositionWrongCount = "UTBindByPositionWrongCount";

        public static string UTNullableDefaultMustBeFollowedWithNullables = "UTNullableDefaultMustBeFollowedWithNullables";

        public static string UTNullableDefaultMustNotBeFollowedWithWildcard = "UTNullableDefaultMustNotBeFollowedWithWildcard";

        public static string UTNullableDefaultMustNotBeFollowedWithLiteral = "UTNullableDefaultMustNotBeFollowedWithLiteral";

        public static string BindUriTemplateToNullOrEmptyPathParam = "BindUriTemplateToNullOrEmptyPathParam";

        public static string UTInvalidVarDeclaration = "UTInvalidVarDeclaration";

        public static string UTStarVariableWithDefaults = "UTStarVariableWithDefaults";

        public static string UTTMustBeAbsolute = "UTTMustBeAbsolute";

        public static string UTTCannotChangeBaseAddress = "UTTCannotChangeBaseAddress";

        public static string UTTBaseAddressMustBeAbsolute = "UTTBaseAddressMustBeAbsolute";

        public static string UTTMultipleMatches = "UTTMultipleMatches";

        public static string UTTBaseAddressNotSet = "UTTBaseAddressNotSet";

        public static string UTTEmptyKeyValuePairs = "UTTEmptyKeyValuePairs";

        public static string UTTNullTemplateKey = "UTTNullTemplateKey";

        public static string UTTInvalidTemplateKey = "UTTInvalidTemplateKey";

        public static string UTInvalidFormatSegmentOrQueryPart = "UTInvalidFormatSegmentOrQueryPart";

        public static string UTDefaultValueToCompoundSegmentVar = "UTDefaultValueToCompoundSegmentVar";

        public static string UTDoesNotSupportAdjacentVarsInCompoundSegment = "UTDoesNotSupportAdjacentVarsInCompoundSegment";

        public static string UTCSRLookupBeforeMatch = "UTCSRLookupBeforeMatch";

        public static string UTTDuplicate = "UTTDuplicate";

        public static string UTTOtherAmbiguousQueries = "UTTOtherAmbiguousQueries";

        public static string UTTAmbiguousQueries = "UTTAmbiguousQueries";

        public static string UTQueryCannotHaveCompoundValue = "UTQueryCannotHaveCompoundValue";

        public static string GetString(string message1)
        {
            string message = string.Empty;
            if (!string.IsNullOrWhiteSpace(message1))
            {
                message = message + message1 + "\n\n";
            }
            return message;
        }

        public static string GetString(string message1, string message2)
        {
            string message = GetString(message1);
            if (!string.IsNullOrWhiteSpace(message2))
            {
                message = message + message2 + "\n\n";
            }
            return message;
        }

        public static string GetString(string message1, UriTemplate message2)
        {
            string message = GetString(message1);
            if (message2 != null)
            {
                message = message + message2.ToString() + "\n\n";
            }
            return message;
        }

        public static string GetString(string message1, string message2, string message3)
        {
            string message = GetString(message1, message2);
            if (!string.IsNullOrWhiteSpace(message3))
            {
                message = message + message3 + "\n\n";
            }
            return message;
        }

        public static string GetString(string message1, string message2, int message3)
        {
            return GetString(message1, message2) + message3.ToString() + "\n\n";
        }

        public static string GetString(string message1, UriTemplate message2, string message3)
        {
            string message = GetString(message1, message2);
            if (!string.IsNullOrWhiteSpace(message3))
            {
                message = message + message3 + "\n\n";
            }
            return message;
        }

        public static string GetString(string message1, string message2, string message3, string message4)
        {
            string message = GetString(message1, message2, message3);
            if (!string.IsNullOrWhiteSpace(message4))
            {
                message = message + message4 + "\n\n";
            }
            return message;
        }

        public static string GetString(string message1, UriTemplate message2, string message3, string message4)
        {
            string message = GetString(message1, message2, message3);
            if (!string.IsNullOrWhiteSpace(message4))
            {
                message = message4 + "\n\n";
            }
            return message;
        }

        public static string GetString(string message1, string message2, int message3, int message4, int message5)
        {
            return GetString(message1, message2, message3) + message4.ToString() + "\n\n" + message5.ToString() + "\n\n";
        }
    }
}
