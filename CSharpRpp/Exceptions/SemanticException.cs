using System;

namespace CSharpRpp.Exceptions
{
    public class SemanticException : Exception
    {
        public int Code { get; set; }

        public SemanticException()
        {
        }

        public SemanticException(int code, string msg) : base(msg)
        {
        }
    }
}