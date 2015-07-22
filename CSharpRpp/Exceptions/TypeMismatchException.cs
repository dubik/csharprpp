using Antlr.Runtime;
using CSharpRpp.Exceptions;

namespace CSharpRpp
{
    public class TypeMismatchException : SemanticException
    {
        public string Found { get; private set; }
        public string Required { get; private set; }
        public IToken Token { get; private set; }

        public TypeMismatchException(IToken token, string found, string required)
        {
            Token = token;
            Found = found;
            Required = required;
        }
    }
}