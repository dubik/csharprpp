using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Expr;
using JetBrains.Annotations;

namespace CSharpRpp.Parser
{
    /*
     case class OverloadQuery(functions: List[RppFunc], implicitCastRules: ImplicitCastRule) {
        val funcByName = functions.groupBy(_.name)

        def find(name: String, args: List[Type]): List[RppFunc] = {
            val candidates = funcByName(name).filter(_.argsTypes.length == args.length)
            findDirectMatch(args, candidates) match {
                case Some(x) => List(x)
                case None => findImplicitMatchedFunctions(args, candidates)
            }
        }

        private def findDirectMatch(args: List[Type], overloads: List[RppFunc]): Option[RppFunc] =
            overloads.find(_.argsTypes == args)

        private def findImplicitMatchedFunctions(args: List[Type], overloads: List[RppFunc]): List[RppFunc] =
            overloads.filter(func => implicitCastRules.canCast(args, func.argsTypes))
    }
     */

    public class OverloadQuery
    {
        [NotNull]
        public static IEnumerable<IRppFunc> Find([NotNull] String name, [NotNull] IEnumerable<RppType> argTypes,
            [NotNull] IReadOnlyCollection<IRppFunc> overloads)
        {
            var argTypesArray = argTypes.ToArray();

            var candidates = new List<IRppFunc>();
            foreach (var candidate in overloads)
            {
                Debug.Assert(candidate.Name == name); // We should have candidates only with the same name

                bool castRequired; // Flag if we need to cast any argument
                IRppParam[] candidateParams = candidate.Params;

                if (SignatureMatched(argTypesArray, candidateParams, out castRequired))
                {
                    if (!castRequired)
                    {
                        return new List<IRppFunc> {candidate};
                    }

                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        public static bool SignatureMatched(IList<RppType> argTypes, IList<IRppParam> candidateParams, out bool castRequired)
        {
            castRequired = false;

            if (candidateParams.Count == 0 && argTypes.Count > 0)
            {
                return false;
            }

            if (candidateParams.Count < argTypes.Count && (candidateParams.Count > 0 && !candidateParams.Last().IsVariadic))
            {
                return false;
            }

            int candidateParamIndex = 0;
            foreach (RppType argType in argTypes)
            {
                IRppParam param = candidateParams[candidateParamIndex];
                RppType paramType = param.Type;

                if (param.IsVariadic)
                {
                    paramType = ((RppArrayType) paramType).SubType;
                }
                else
                {
                    candidateParamIndex++;
                }

                if (!argType.Equals(paramType))
                {
                    castRequired = true;

                    if (!ImplicitCast.CanCast(argType, paramType))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}