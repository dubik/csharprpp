using System.Collections.Generic;
using System.Linq;
using CSharpRpp;
using CSharpRpp.TypeSystem;

namespace CSharpRppTest
{
    public class InferenceContext
    {
        public static IList<RType> TypesAsList(RppMethodInfo methodInfo)
        {
            List<RType> list = new List<RType>();
            methodInfo.GenericParameters.Select(gp => gp.Type).ForEach(list.Add);
            methodInfo.Parameters?.Select(p => p.Type).ForEach(list.Add);
            list.Add(methodInfo.ReturnType);
            return list;
        }
    }
}