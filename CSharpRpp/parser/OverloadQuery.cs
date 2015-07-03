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
                if (candidate.Params.Length > argTypesArray.Length)
                {
                    continue; // Skip candidate, amount of arguments is less the candidate
                }

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

        private static bool SignatureMatched(RppType[] argTypes, IRppParam[] candidateParams, out bool castRequired)
        {
            castRequired = false;

            for (int i = 0; i < argTypes.Length; i++)
            {
                RppType argType = argTypes[i];
                IRppParam param = candidateParams[i];
                RppType paramType = param.Type;

                if (param.IsVariadic)
                {
                    RppType baseVariadicParamType = ((RppArrayType) paramType).SubType;
                }
                else
                {
                    if (!argType.Equals(paramType))
                    {
                        castRequired = true;

                        if (!ImplicitCast.CanCast(argType, paramType))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}