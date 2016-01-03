using Antlr.Runtime;

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

        private static string FormatErrorAndPointAtToken(IToken token, string errorMsg)
        {
            string firstLine = $"Error({token.Line}, {token.CharPositionInLine}) {errorMsg}";
            string secondLine = TokenUtils.GetTokenLine(token);
            string pointerLine = $"{TokenUtils.Ident(token.CharPositionInLine)}^";
            return $"{firstLine}\n{secondLine}\n{pointerLine}";
        }
    }
}