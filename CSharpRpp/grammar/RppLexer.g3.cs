using System.Collections.Generic;

namespace CSharpRpp
{
    partial class RppLexer
    {
        private static Dictionary<int, string> TokenToString = new Dictionary<int, string>()
        {
            {1, "Hello"}
        };

        static public string GetDescr()
        {
            return "Hello";
        }
    }
}
