using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Parser;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppNew : RppNode, IRppExpr
    {
        public ResolvableType Type { get; }

        public IEnumerable<IRppExpr> Args => _arguments.AsEnumerable();

        private IList<IRppExpr> _arguments;

        public RppMethodInfo Constructor { get; private set; }

        public RppNew([NotNull] ResolvableType type, [NotNull] IList<IRppExpr> arguments)
        {
            Type = type;
            _arguments = arguments;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(SymbolTable scope)
        {
            _arguments = NodeUtils.Analyze(scope, _arguments);

            Type.Resolve(scope);

            List<RType> argTypes = Args.Select(a => a.Type.Value).ToList();
            var constructors = Type.Value.Constructors;

            List<RppMethodInfo> candidates = OverloadQuery.Find(argTypes, constructors).ToList();
            if (candidates.Count != 1)
            {
                throw new Exception("Can't figure out which overload to use");
            }

            Constructor = candidates[0];
            return this;
        }
    }
}