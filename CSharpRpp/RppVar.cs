using System;
using System.Reflection.Emit;

namespace CSharpRpp
{
    public class RppVar : RppNamedNode, IRppExpr
    {
        public RppType Type { get; private set; }
        public Type RuntimeType { get; private set; }

        private IRppExpr _initExpr;

        public RppVar(string decl, string name, RppType type, IRppExpr initExpr) : base(name)
        {
            Type = type;
            _initExpr = initExpr;
        }

        public void Codegen(ILGenerator generator)
        {
            throw new NotImplementedException();
        }
    }
}