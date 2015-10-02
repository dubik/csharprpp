using System;
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
        public static IList<RppType> NoTypes = new List<RppType>().AsReadOnly();
        public static IList<Type> NoRuntimeTypes = new List<Type>().AsReadOnly();
        public static IList<RppVariantTypeParam> NoVariantTypeParams = new List<RppVariantTypeParam>().AsReadOnly();
        public static HashSet<ObjectModifier> NoModifiers = new HashSet<ObjectModifier>();

        public static IReadOnlyCollection<IRppFunc> NoFuncsCollection = new List<IRppFunc>();
    }
}