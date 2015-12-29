using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    public class RppMethodInfo
    {
        public string Name { get; }
        public RMethodAttributes Attributes { get; }

        [CanBeNull]
        public virtual RType ReturnType { get; set; }

        [NotNull]
        public virtual RppParameterInfo[] Parameters { get; set; }

        public RppTypeParameterInfo[] TypeParameters { get; set; }

        public virtual IReadOnlyCollection<RType> GenericArguments => Collections.NoRTypes;

        [NotNull]
        public RType DeclaringType { get; }

        public virtual MethodBase Native { get; set; }

        public bool IsVariadic => Parameters.Any() && Parameters.Last().IsVariadic;

        public bool IsStatic => DeclaringType.Name.EndsWith("$");

        public bool IsGenericMethod
            => ReturnType.IsGenericType || ReturnType.IsGenericParameter || Parameters.Any(p => p.Type.IsGenericType || p.Type.IsGenericParameter);

        public RppMethodInfo GenericMethodDefinition { get; set; }

        private RppGenericParameter[] _genericParameters;

        public virtual RppGenericParameter[] GenericParameters => _genericParameters;

        public RppMethodInfo([NotNull] string name, [NotNull] RType declaringType, RMethodAttributes attributes,
            [CanBeNull] RType returnType,
            [NotNull] RppParameterInfo[] parameters)
        {
            Name = name;
            DeclaringType = declaringType;
            Attributes = attributes;
            ReturnType = returnType;
            Parameters = parameters;
        }

        public RppGenericParameter[] DefineGenericParameters(string[] genericParameterName)
        {
            if (_genericParameters != null && _genericParameters.Any())
            {
                throw new Exception("there were generic paremeters defined already");
            }

            _genericParameters = RTypeUtils.CreateGenericParameters(genericParameterName, DeclaringType, this).ToArray();
            return _genericParameters;
        }

        public bool HasGenericParameters()
        {
            return GenericParameters != null && GenericParameters.Length > 0;
        }

        #region ToString

        public override string ToString()
        {
            var res = new List<string> {ToString(Attributes), Name + GenericParametersToString() + ParamsToString(), ":", ReturnType?.ToString()};
            return string.Join(" ", res);
        }

        private static readonly List<Tuple<RMethodAttributes, string>> _attrToStr = new List
            <Tuple<RMethodAttributes, string>>
        {
            Tuple.Create(RMethodAttributes.Final, "final"),
            Tuple.Create(RMethodAttributes.Public, "public"),
            Tuple.Create(RMethodAttributes.Private, "public"),
            Tuple.Create(RMethodAttributes.Abstract, "abstract"),
            Tuple.Create(RMethodAttributes.Override, "override"),
            Tuple.Create(RMethodAttributes.Static, "static")
        };

        private static string ToString(RMethodAttributes attrs)
        {
            List<string> res = new List<string>();

            _attrToStr.Aggregate(res, (list, tuple) =>
                                      {
                                          if (attrs.HasFlag(tuple.Item1))
                                          {
                                              list.Add(tuple.Item2);
                                          }
                                          return list;
                                      });

            return string.Join(" ", res);
        }

        private string GenericParametersToString()
        {
            if (_genericParameters != null)
            {
                return "[" + string.Join(", ", GenericParameters.Select(gp => gp.Name)) + "]";
            }

            return string.Empty;
        }

        private string ParamsToString()
        {
            if (Parameters != null)
            {
                return "(" + string.Join(", ", Parameters.Select(p => p.Name + ": " + p.Type.ToString())) + ")";
            }

            return string.Empty;
        }

        #endregion
    }
}