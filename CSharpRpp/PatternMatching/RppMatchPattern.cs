using System.Collections.Generic;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public abstract class RppMatchPattern : RppNode
    {
        /// <summary>
        /// Creates RppVar(s) with new variables (if any), needed for semantic analysis
        /// </summary>
        /// <returns></returns>
        [NotNull]
        public virtual IEnumerable<IRppExpr> DeclareVariables([NotNull] RType inputType)
        {
            return Collections.NoExprs;
        }

        public abstract IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx);
    }
}