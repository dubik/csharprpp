using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSharpRpp
{
    public class RppProgram : IRppNode
    {
        private IList<RppClass> _classes = new List<RppClass>();

        public void Add(RppClass clazz)
        {
            _classes.Add(clazz);
        }

        public void PreAnalyze(RppScope scope)
        {
            NodeUtils.PreAnalyze(scope, _classes);
        }

        public IRppNode Analyze(RppScope scope)
        {
            _classes = NodeUtils.Analyze(scope, _classes);
            return this;
        }
    }
}