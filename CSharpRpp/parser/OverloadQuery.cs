using System.Collections.Generic;
using System.Linq;
using CSharpRpp.Expr;
using JetBrains.Annotations;
using CSharpRpp.TypeSystem;

namespace CSharpRpp.Parser
{
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
            return Find(argTypes, Collections.NoRTypes, overloads, comparator);
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
        public static IEnumerable<RppMethodInfo> Find<T>([NotNull] IEnumerable<T> argTypes, [NotNull] IEnumerable<RType> typeArgs,
            [NotNull] IEnumerable<RppMethodInfo> overloads,
            ITypesComparator<T> comparator)
        {
            int typeArgsCount = typeArgs.Count();

            var argTypesArray = argTypes.ToArray();

            var candidates = new List<RppMethodInfo>();
            foreach (var candidate in overloads)
            {
                bool castRequired; // Flag if we need to cast any argument
                RppParameterInfo[] candidateParams = candidate.Parameters;

                int candidateTypeParamCount = candidate.GenericParameters?.Length ?? 0;

                // If no type args specified, then we shouldn't check if they match candidate generic parameters count
                bool passTypeArgsCount = true;
                if (typeArgsCount != 0)
                {
                    passTypeArgsCount = candidateTypeParamCount == typeArgsCount;
                }

                if (passTypeArgsCount
                    && SignatureMatched(argTypesArray, candidateParams, comparator, out castRequired))
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

        public static bool SignatureMatched<T>(IList<T> items, IList<RppParameterInfo> candidateParams,
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
                    paramType = param.Type.SubType();
                }
                else
                {
                    candidateParamIndex++;
                }

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