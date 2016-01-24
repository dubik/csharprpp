using System.Collections.Generic;
using System.IO;
using System.Text;
using Antlr.Runtime;

namespace CSharpRpp.Exceptions
{
    internal class TokenUtils
    {
        public static string GetTokenLine(IToken token)
        {
            return GetLines(token.InputStream.ToString())[token.Line - 1];
        }

        private static IList<string> GetLines(string text)
        {
            List<string> lines = new List<string>();
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        public static string Ident(int ident)
        {
            StringBuilder res = new StringBuilder();
            while (ident-- > 0)
            {
                res.Append(" ");
            }

            return res.ToString();
        }
    }
}