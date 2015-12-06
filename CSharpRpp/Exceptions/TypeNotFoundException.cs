using Antlr.Runtime;

namespace CSharpRpp.Exceptions
{
    public class TypeNotFoundException : SemanticException
    {
        public IToken Token { get; set; }

        public TypeNotFoundException(IToken token)
        {
            Token = token;
        }

        public string GenerateMessage()
        {
            if (Token != null)
            {
                string firstLine = $"Error({Token.Line}, {Token.CharPositionInLine}) not found: type {Token.Text}";
                string secondLine = TokenUtils.GetTokenLine(Token);
                string pointerLine = $"{TokenUtils.Ident(Token.CharPositionInLine)}^";
                return $"{firstLine}\n{secondLine}\n{pointerLine}";
            }

            return "type not found";
        }
    }
}