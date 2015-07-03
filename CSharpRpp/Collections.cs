using System.Collections.Generic;

namespace CSharpRpp
{
    public static class Collections
    {
        public static IList<RppField> NoFields = new List<RppField>().AsReadOnly();
        public static IList<IRppNode> NoNodes = new List<IRppNode>().AsReadOnly();
        public static IList<IRppFunc> NoFuncs = new List<IRppFunc>().AsReadOnly();
        public static IList<string> NoStrings = new List<string>().AsReadOnly();
        public static IList<IRppExpr> NoExprs = new List<IRppExpr>().AsReadOnly();

        public static IReadOnlyCollection<IRppFunc> NoFuncsCollection = new List<IRppFunc>();
    }
}