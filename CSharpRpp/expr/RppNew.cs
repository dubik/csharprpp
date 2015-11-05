using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Expr;
using CSharpRpp.Parser;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using Mono.Collections.Generic;

namespace CSharpRpp
{
    public class RppNew : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }
        public ResolvableType Type2 { get; private set; }
        public Type RuntimeType { get; private set; }

        public IEnumerable<RppType> ArgumentTypes { get; private set; }

        public IEnumerable<IRppExpr> Args => _arguments.AsEnumerable();

        private readonly IList<IRppExpr> _arguments;

        public IEnumerable<RppVariantTypeParam> TypeArgs => _typeArgs.AsEnumerable();

        private readonly IList<RppVariantTypeParam> _typeArgs;

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