using System;
using System.Collections.Generic;
using CSharpRpp.TypeSystem;
using static CSharpRpp.ListExtensions;
using static CSharpRpp.Utils.AstHelper;

namespace CSharpRpp
{
    public class RppVariablePattern : RppMatchPattern
    {
        public string Name { get; set; }

        public RppVariablePattern()
        {
            Name = "_";
        }

        public RppVariablePattern(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override IEnumerable<IRppExpr> DeclareVariables(RType inputType)
        {
            if (Name == "_")
            {
                return Collections.NoExprs;
            }

            RppVar variable = Val(Name, inputType, new RppDefaultExpr(inputType.AsResolvable()));
            variable.Token = Token;
            return List(variable);
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx)
        {
            if (Name == "_")
            {
                return Block(Assign(outOut, thenExpr), Break);
            }

            throw new NotImplementedException();
        }
    }
}