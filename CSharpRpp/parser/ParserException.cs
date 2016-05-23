using System;

namespace CSharpRpp.Parser
{
    public class ParserException : Exception
    {
        public readonly int Code;

        public ParserException(string message) : base(message)
        {
        }

        public ParserException(int code, string message) : base(message)
        {
            Code = code;
        }
    }
}