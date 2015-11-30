using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Symbols;

namespace CSharpRpp
{
    [DebuggerDisplay("Classes = {_classes.Count}")]
    public class RppProgram : RppNode
    {
        public IEnumerable<RppClass> Classes => _classes.AsEnumerable();

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)] private IList<RppClass> _classes = new List<RppClass>();

        public void Add(RppClass clazz)
        {
            _classes.Add(clazz);
        }

        public void PreAnalyze(SymbolTable scope)
        {
            _classes.ForEach(c => scope.AddType(c.Type));

            NodeUtils.PreAnalyze(scope, _classes);
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
            _classes.ForEach(clazz => clazz.Accept(visitor));
        }

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            _classes = NodeUtils.Analyze(scope, _classes);
            return this;
        }
    }
}