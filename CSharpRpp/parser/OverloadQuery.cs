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
        public delegate bool TypesComparator<in T>(T source, RppType target);

        public delegate bool CanCast<in T>(T source, RppType target);

        public static bool DefaultTypesComparator(RppType source, RppType target)
        {
            return source.Equals(target);
        }

        public static bool DefaultCanCast(RppType source, RppType target)
        {
            return ImplicitCast.CanCast(source, target);
        }

        [NotNull]
        public static IEnumerable<IRppFunc> Find([NotNull] IEnumerable<RppType> argTypes, [NotNull] IEnumerable<IRppFunc> overloads)
        {
            return Find(argTypes, overloads, DefaultTypesComparator, DefaultCanCast);
        }

        [NotNull]
        public static IEnumerable<IRppFunc> Find<T>([NotNull] IEnumerable<T> argTypes, [NotNull] IEnumerable<IRppFunc> overloads,
            TypesComparator<T> typesComparator, CanCast<T> canCast)
        {
            var argTypesArray = argTypes.ToArray();

            var candidates = new List<IRppFunc>();
            foreach (var candidate in overloads)
            {
                bool castRequired; // Flag if we need to cast any argument
                IRppParam[] candidateParams = candidate.Params;

                if (SignatureMatched(argTypesArray, candidateParams, typesComparator, canCast, out castRequired))
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

        public static bool SignatureMatched<T>(IList<T> argTypes, IList<IRppParam> candidateParams, TypesComparator<T> typesComparator,
            CanCast<T> canCast, out bool castRequired)
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
            foreach (T argType in argTypes)
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

                if (!typesComparator(argType, paramType))
                {
                    castRequired = true;

                    if (!canCast(argType, paramType))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}