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
        public ResolvableType Type2 { get; }

        public IEnumerable<IRppExpr> Args => _arguments.AsEnumerable();

        private readonly IList<IRppExpr> _arguments;

        public RppMethodInfo Constructor { get; private set; }

        public RppNew([NotNull] ResolvableType type, [NotNull] IList<IRppExpr> arguments)
        {
            Type2 = type;
            _arguments = arguments;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(SymbolTable scope)
        {
            NodeUtils.Analyze(scope, _arguments);

            Type2.Resolve(scope);

            List<RType> argTypes = Args.Select(a => a.Type2.Value).ToList();
            var constructors = Type2.Value.Constructors;

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