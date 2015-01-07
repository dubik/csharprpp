using System.Collections;
using System.Collections.Generic;

namespace CSharpRpp
{
    class Scope
    {
         
    }

    interface IRppNode
    {
        void PreAnalyze(Scope scope);
        IRppNode Analyze(Scope scope);
    }

    class RppProgram
    {
        public void Add()
        {
            
        }
    }

    class RppClass
    {
        
    }

    class RppField
    {
        
    }

    class RppFunc
    {
         
    }

    class RppParam
    {
        
    }

    class RppType
    {
        
    }

    class RppExpr
    {
         
    }

    class Program
    {
        static void Main(string[] args)
        {
            IList<string> k = new List<string>();
            k.Add("Hello");
        }
    }
}
