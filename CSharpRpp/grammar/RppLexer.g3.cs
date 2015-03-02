using System.Collections.Generic;
using System.Globalization;

namespace CSharpRpp
{
    partial class RppLexer
    {
        private static Dictionary<int, string> _tokenToString = new Dictionary<int, string>()
        {
            {KW_Package, "package"},
            {KW_Abstract, "abstract"},
            {KW_Case, "case"},
            {KW_Catch, "catch"},
            {KW_Class, "class"},
            {KW_Def, "def"},
            {KW_Do, "do"},
            {KW_Else, "else"},
            {KW_Extends, "extends"},
            {KW_Final, "final"},
            {KW_For, "for"},
            {KW_ForSome, "forSome"},
            {KW_If, "if"},
            {KW_Implicit, "implicit"},
            {KW_Import, "import"},
            {KW_Lazy, "lazy"},
            {KW_Match, "match"},
            {KW_New, "new"},
            {KW_Null, "null"},
            {KW_Object, "object"},
            {KW_Override, "override"},
            {KW_Private, "private"},
            {KW_Protected, "protected"},
            {KW_Return, "return"},
            {KW_Sealed, "sealed"},
            {KW_Super, "super"},
            {KW_This, "this"},
            {KW_Throw, "throw"},
            {KW_Trait, "trait"},
            {KW_Try, "try"},
            {KW_Type, "type"},
            {KW_Val, "val"},
            {KW_Var, "var"},
            {KW_While, "while"},
            {KW_With, "with"},
            {KW_Yield, "yield"},
            {OP_Semi, ";"},
            {OP_LBracket, "["},
            {OP_RBracket, "]"},
            {OP_LBrace, "{"},
            {OP_RBrace, "}"},
            {OP_Comma, ","},
            {OP_LParen, "("},
            {OP_RParen, ")"},
            {OP_Colon, ":"},
            {OP_Eq, "="},
            {OP_Dot, "."},
            {OP_Star, "*"},
            {BooleanLiteral, "true or false"},
            {Id, "identifier"},
            {IntegerLiteral, "integer literal"},
            {FloatingPointLiteral, "float literal"}
        };

        public static string TokenToString(int token)
        {
            string str;
            if (_tokenToString.TryGetValue(token, out str))
            {
                return str;
            }

            return token.ToString(CultureInfo.CurrentCulture);
        }
    }
}