using System.Collections.Generic;
using System.Linq;
using Antlr.Runtime;
using CSharpRpp.TypeSystem;

namespace CSharpRpp.Exceptions
{
    public class SemanticExceptionFactory
    {
        public static SemanticException TypeNotFound(IToken token)
        {
            return new SemanticException(103, FormatErrorAndPointAtToken(token, $"not found: type {token.Text}"));
        }

        public static SemanticException MemberNotFound(IToken token, string targetTypeName)
        {
            string str = FormatErrorAndPointAtToken(token, $"value {token.Text} is not a member of {targetTypeName}");
            return new SemanticException(104, str);
        }

        public static SemanticException TypeMismatch(IToken token, string requiredType, string foundType)
        {
            string message = $"type mismatch;\n found: {foundType}\n required: {requiredType}";
            string str = FormatErrorAndPointAtToken(token, message);
            return new SemanticException(105, str);
        }

        public static SemanticException ValueNotFound(IToken token)
        {
            return new SemanticException(106, FormatErrorAndPointAtToken(token, $"not found: value {token.Text}"));
        }

        public static SemanticException NotEnoughArguments(IToken token, RppMethodInfo targetMethod)
        {
            string methodString = MethodString(targetMethod);
            return new SemanticException(107, FormatErrorAndPointAtToken(token, $"not enough arguments for method {methodString}"));
        }

        public static SemanticException PatternMatchingCaseClausesHaveDifferentExpressionTypes(IToken token)
        {
            return new SemanticException(108, FormatErrorAndPointAtToken(token, "cases can\'t have different return types"));
        }

        public static SemanticException ValueIsNotMember(IToken token, string typeName)
        {
            return new SemanticException(109, FormatErrorAndPointAtToken(token, $"value {token.Text} is not a member of {typeName}"));
        }

        public static SemanticException MethodGenericArgumentIsNotSpecified(IToken token)
        {
            return new SemanticException(110, FormatErrorAndPointAtToken(token, "please specify generic arguments, type inference is more limited when varargs are used"));
        }

        private static string MethodString(RppMethodInfo method)
        {
            if (method.Name == "ctor")
            {
                return method.ToString().Replace("ctor", "constructor").Replace("constrparam", "");
            }

            return method.ToString();
        }

        private static string FormatErrorAndPointAtToken(IToken token, string errorMsg)
        {
            string firstLine = $"Error({token.Line}, {token.CharPositionInLine}) {errorMsg}";
            string secondLine = TokenUtils.GetTokenLine(token);
            string pointerLine = $"{TokenUtils.Ident(token.CharPositionInLine)}^";
            return $"{firstLine}\n{secondLine}\n{pointerLine}";
        }

        public static SemanticException CreateOverloadFailureException(IToken token, IEnumerable<RppMethodInfo> matchingFunctions, IEnumerable<IRppExpr> args,
            IEnumerable<RppMethodInfo> allFunctions)
        {
            if (matchingFunctions.Any())
            {
                // Not matching at all
                if (allFunctions.Count() > 1) // many alternatives
                {
                }

                // just one alternative
            }
            else
            {
                // Matching too many overloads
                IEnumerable<RppMethodInfo> functions = allFunctions as IList<RppMethodInfo> ?? allFunctions.ToList();
                if (functions.Count() > 1)
                {
                }
                else
                {
                    RppMethodInfo closestMethod = functions.First();
                    if (closestMethod.Parameters.Length > args.Count())
                    {
                        // Not enough arguments
                        return NotEnoughArguments(token, closestMethod);
                    }

                    // Too many arguments
                }
            }

            return new SemanticException(-1, "Not done");
        }
    }
}