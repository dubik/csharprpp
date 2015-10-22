using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Expr;
using JetBrains.Annotations;
using System;
using CSharpRpp.TypeSystem;

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

    public interface ITypesComparator<in T>
    {
        bool Compare(T source, RType target);
        bool CanCast(T source, RType target);
    }

    public class OverloadQuery
    {
        public delegate bool TypesComparator<in T>(T source, RType target);

        public delegate bool CanCast<in T>(T source, RType target);

        public static bool DefaultTypesComparator(RType source, RType target)
        {
            return source.Equals(target);
        }

        public static bool DefaultCanCast(RType source, RType target)
        {
            return ImplicitCast.CanCast(source, target);
        }

        [NotNull]
        public static IEnumerable<RppMethodInfo> Find([NotNull] IEnumerable<RType> argTypes, [NotNull] IEnumerable<RppMethodInfo> overloads)
        {
            return Find(argTypes, overloads, DefaultTypesComparator, DefaultCanCast);
        }

        public static IEnumerable<RppMethodInfo> Find<T>([NotNull] IEnumerable<T> argTypes, [NotNull] IEnumerable<RppMethodInfo> overloads,
            ITypesComparator<T> comparator)
        {
            return Find(argTypes, Collections.NoRuntimeTypes, overloads, comparator);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="argTypes"></param>
        /// <param name="typeArgs">specialized function type argument for function</param>
        /// <param name="overloads"></param>
        /// <param name="comparator"></param>
        /// <returns></returns>
        public static IEnumerable<RppMethodInfo> Find<T>([NotNull] IEnumerable<T> argTypes, [NotNull] IEnumerable<Type> typeArgs,
            [NotNull] IEnumerable<RppMethodInfo> overloads,
            ITypesComparator<T> comparator)
        {
            var argTypesArray = argTypes.ToArray();

            var candidates = new List<RppMethodInfo>();
            foreach (var candidate in overloads)
            {
                bool castRequired; // Flag if we need to cast any argument
                RppParameterInfo[] candidateParams = candidate.Parameters;

                int candidateTypeParamCount = candidate.TypeParameters?.Length ?? 0;

                if (candidateTypeParamCount == typeArgs.Count()
                    && SignatureMatched(argTypesArray, typeArgs, candidateParams, comparator, out castRequired))
                {
                    if (!castRequired)
                    {
                        return new List<RppMethodInfo> {candidate};
                    }

                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        [NotNull]
        public static IEnumerable<RppMethodInfo> Find<T>([NotNull] IEnumerable<T> argTypes, [NotNull] IEnumerable<RppMethodInfo> overloads,
            TypesComparator<T> typesComparator, CanCast<T> canCast)
        {
            return Find(argTypes, overloads, new DelegateTypeComparator<T>(typesComparator, canCast));
        }

        public static bool SignatureMatched<T>(IList<T> items, IEnumerable<Type> typeArgs, IList<RppParameterInfo> candidateParams,
            ITypesComparator<T> comparator,
            out bool castRequired)
        {
            castRequired = false;

            if (candidateParams.Count == 0 && items.Count > 0)
            {
                return false;
            }

            if (items.Count == 0 && candidateParams.Count != 0)
            {
                return false;
            }

            bool isCandidateVariadic = candidateParams.Count > 0 && candidateParams.Last().IsVariadic;

            if ((candidateParams.Count < items.Count) && !isCandidateVariadic)
            {
                return false;
            }

            int candidateParamIndex = 0;
            foreach (T item in items)
            {
                RppParameterInfo param = candidateParams[candidateParamIndex];
                RType paramType = param.Type;

                if (param.IsVariadic)
                {
                    throw new NotImplementedException("Variadic is not implemented yet");
                    // paramType = ((RppArrayType) paramType).SubType;
                }
                else
                {
                    candidateParamIndex++;
                }

                // If generic parameter, replace it with specialized type
                // def func[A](x: A)...
                // Though this only works for generic methods. If generic parameter comes
                // class Foo[A] {
                //    def func(x: A)...
                // }
                // from class description, then we should skip, it will be taken care of in the comparator
                /*
                if (paramType.Runtime.IsGenericParameter && typeArgs.Any())
                {
                    paramType = RppNativeType.Create(typeArgs.ElementAt(paramType.Runtime.GenericParameterPosition));
                }*/

                if (!comparator.Compare(item, paramType))
                {
                    castRequired = true;

                    if (!comparator.CanCast(item, paramType))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}