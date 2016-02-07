using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using static CSharpRpp.ListExtensions;

namespace CSharpRpp.TypeSystem
{
    public static class RTypeExtensions
    {
        /// <summary>
        /// Returns sub type of array type
        /// </summary>
        /// <param name="type">array type</param>
        /// <returns>type of element of array</returns>
        public static RType ArrayElementType([NotNull] this RType type)
        {
            if (!type.IsArray)
            {
                throw new ArgumentException("Expected array type", nameof(type));
            }

            if (type.Methods.Count < 2 && type.Methods[1].Name != "apply")
            {
                throw new Exception("For Array second method should be apply");
            }

            var returnType = type.Methods[1].ReturnType;

            if (returnType == null)
            {
                throw new Exception("Return type is not defined so we can't create array type");
            }

            return returnType;
        }

        public static IEnumerable<RType> LinearizeHierarchy([NotNull] this RType type)
        {
            var interfaces = type.LinearizeInterfaces().Distinct();
            var baseClasses = type.LinearizeBaseClasses().Concat(interfaces);

            return type.IsInterface ? baseClasses : baseClasses.Concat(RppTypeSystem.AnyTy);
        }

        private static IEnumerable<RType> LinearizeBaseClasses([NotNull] this RType type)
        {
            if (Equals(type, RppTypeSystem.AnyTy))
            {
                return Collections.NoRTypes;
            }

            // For interfaces BaseType is null
            if (type.BaseType == null)
            {
                return List(type);
            }

            return List(type).Concat(type.BaseType.LinearizeBaseClasses());
        }

        private static IEnumerable<RType> LinearizeInterfaces([NotNull] this RType type)
        {
            if (Equals(type, RppTypeSystem.AnyTy))
            {
                return Collections.NoRTypes;
            }

            // For interfaces BaseType is null
            if (type.BaseType == null)
            {
                return List(type);
            }

            return type.Interfaces.SelectMany(i => i.LinearizeInterfaces().Concat(type.BaseType.LinearizeInterfaces()));
        }
    }
}