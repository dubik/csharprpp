using System;
using System.Collections.Generic;
using System.IO;

namespace CSharpRpp.Reporting
{
    public class Diagnostic
    {
        public IEnumerable<ErrorMessage> Errors => _errors;

        private readonly List<ErrorMessage> _errors = new List<ErrorMessage>();

        public void Error(int code, string message)
        {
            _errors.Add(new ErrorMessage(code, message));
        }

        public void Error(int code, string firstline, string secondline)
        {
            _errors.Add(new ErrorMessage(code, firstline, secondline));
        }

        public void Report()
        {
            TextWriter outStream = Console.Error;

            outStream.WriteLine();
            _errors.ForEach(e => outStream.WriteLine(e.ToString()));
            outStream.WriteLine();
        }
    }
}