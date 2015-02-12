using System;
using System.Reflection.Emit;

namespace CSharpRpp
{
    public class RppVar : RppNamedNode, IRppStatementExpr
    {
        public RppType Type { get; private set; }
        public Type RuntimeType { get; private set; }

        private readonly IRppExpr _initExpr;

        private LocalBuilder _builder;

        public RppVar(MutabilityFlag mutability, string name, RppType type, IRppExpr initExpr) : base(name)
        {
            Type = type;

            _initExpr = initExpr ?? new RppEmptyExpr();
        }

        public override void PreAnalyze(RppScope scope)
        {
            _initExpr.PreAnalyze(scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _initExpr.Analyze(scope);

            RuntimeType = Type.Resolve(scope);
            return this;
        }

        public void Codegen(ILGenerator generator)
        {
            _builder = generator.DeclareLocal(RuntimeType);
            
            if (!(_initExpr is RppEmptyExpr))
            {
                _initExpr.Codegen(generator);
                generator.Emit(OpCodes.Stloc, _builder.LocalIndex);
            }
        }
    }
}