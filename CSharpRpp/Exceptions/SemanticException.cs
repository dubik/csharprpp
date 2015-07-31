using System;

namespace CSharpRpp.Exceptions
{
    public class SemanticException : Exception
    {
        public SemanticException()
        {
        }

        public SemanticException(string msg) : base(msg)
        {
        }
    }
}
