using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;

namespace CSharpRpp.Parser
{
    public class FuncValidator
    {
        public static void Validate(IList<RppFunc> functions)
        {
            IList<RppFunc> workingSet = new List<RppFunc>();
            foreach (RppFunc function in functions)
            {
                if (workingSet.Count > 0)
                {
                    Validate(workingSet, function);
                }

                Validate(function);
                workingSet.Add(function);
            }
        }

        private static void Validate(IEnumerable<RppFunc> definedFunctions, RppFunc function)
        {
            var foundFunc = definedFunctions.First(func => func.Name == function.Name);
            if (foundFunc != null)
            {
                CheckReturnValue(function, foundFunc);
                CheckParams(function, foundFunc);
                CheckTheSameAmountOfParams(function, foundFunc);
            }
        }

        private static void CheckTheSameAmountOfParams(RppFunc function, RppFunc foundFunc)
        {
            if (foundFunc.Params.SequenceEqual(function.Params))
            {
                throw new Exception("Functions have the same amount of parameters");
            }
        }

        private static void CheckReturnValue(RppFunc function, RppFunc foundFunc)
        {
            if (!foundFunc.ReturnType.Equals(function.ReturnType))
            {
                throw new Exception("Return type should match for the functions with the same name");
            }
        }

        private static void CheckParams(RppFunc function, RppFunc foundFunc)
        {
            if (!foundFunc.Params.SequenceEqual(function.Params))
            {
                throw new Exception("Duplicated function with the same signature");
            }
        }

        private static void Validate(IRppFunc function)
        {
            function.Params.ForEachWithIndex((index, param) =>
                                             {
                                                 if (RppPrimitiveType.UnitTy.Equals(param.Type))
                                                 {
                                                     throw new Exception(string.Format("Parameter {0} can't be Unit", param.Name));
                                                 }
                                             });
        }
    }
}