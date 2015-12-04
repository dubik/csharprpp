﻿using System;
using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Parser;
using CSharpRpp.Reporting;
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

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            _arguments = NodeUtils.Analyze(scope, _arguments, diagnostic);

            Type.Resolve(scope);

            List<RType> argTypes = Args.Select(a => a.Type.Value).ToList();
            var constructors = Type.Value.Constructors;

            var genericArguments = Type.Value.GenericArguments.ToList();
            DefaultTypesComparator comparator = new DefaultTypesComparator(genericArguments.ToArray());
            List<RppMethodInfo> candidates = OverloadQuery.Find(Args, new RType[0], constructors, comparator).ToList();
            if (candidates.Count != 1)
            {
                throw new Exception("Can't figure out which overload to use");
            }

            Constructor = candidates[0];
            return this;
        }
    }
}