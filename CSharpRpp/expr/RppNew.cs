using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Expr;
using CSharpRpp.Parser;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using Mono.Collections.Generic;

namespace CSharpRpp
{
    public class RppNew : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }
        public ResolvableType Type2 { get; private set; }
        public Type RuntimeType { get; private set; }

        [NotNull]
        public RType RefType2 { get; private set; }

        public IEnumerable<RppType> ArgumentTypes { get; private set; }

        private readonly string _typeName;

        public IEnumerable<IRppExpr> Args => _constructorsParams.AsEnumerable();

        private readonly IList<IRppExpr> _constructorsParams;

        public IEnumerable<RppVariantTypeParam> TypeArgs => _typeArgs.AsEnumerable();

        private readonly IList<RppVariantTypeParam> _typeArgs;

        public RppMethodInfo Constructor { get; private set; }

        public RppNew([NotNull] string typeName, [NotNull] IList<IRppExpr> constructorsParams)
        {
            _typeName = typeName;
            _constructorsParams = constructorsParams;
            _typeArgs = ReadOnlyCollection<RppVariantTypeParam>.Empty;
        }

        public RppNew([NotNull] string typeName, [NotNull] IList<IRppExpr> constructorsParams, [NotNull] IList<RppVariantTypeParam> typeArgs)
        {
            _typeName = typeName;
            _constructorsParams = constructorsParams;
            _typeArgs = typeArgs;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        private Dictionary<string, RppVariantTypeParam> _nameToGenericArg;

        public override IRppNode Analyze(RppScope scope)
        {
            NodeUtils.Analyze(scope, _constructorsParams);

            var refClass = scope.LookupType(_typeName);

            RefType2 = refClass;

            _typeArgs.ForEach(arg => arg.Resolve(scope));

            // TODO Find correct constructor
            var constructors = RefType2.Constructors;
            CreateNameToVariantTypeParam();

            List<RType> argTypes = Args.Select(a => a.Type2.Value).ToList();
            List<RppMethodInfo> candidates = OverloadQuery.Find(argTypes, constructors, TypesComparator, CanCast).ToList();
            if (candidates.Count != 1)
            {
                throw new Exception("Can't figure out which overload to use");
            }

            Constructor = candidates[0];

            //Type = CreateType();
            Type2 = new ResolvableType(RefType2);
            return this;
        }

        private void CreateNameToVariantTypeParam()
        {
            _nameToGenericArg = new Dictionary<string, RppVariantTypeParam>();
            int index = 0;
            foreach (var typeParam in RefType2.TypeParameters)
            {
                _nameToGenericArg.Add(typeParam.Name, _typeArgs[index]);
                index++;
            }
        }

        private static bool CanCast(RType source, RType target)
        {
            return ImplicitCast.CanCast(source, target);
        }

        private bool TypesComparator(RType source, RType target)
        {
            /*
            if (target.Runtime.IsGenericParameter)
            {
                RppVariantTypeParam typeParam = _nameToGenericArg[target.Runtime.Name];
                return source.Runtime == typeParam.Runtime;
            }
            */

            return source.Equals(target);
        }

        /// <summary>
        /// Creates type of 'new' expression, if required it creates generic arguments.
        /// </summary>
        /// <returns>type of the 'new' expression</returns>
        /*
        private ResolvedType CreateType()
        {
            if (_typeArgs.Count > 0)
            {
                var genericArgs = _typeArgs.Select(arg => arg.Runtime).ToArray();
                return new RppGenericObjectType((RppClass) RefType2, genericArgs, RefType2.RuntimeType.MakeGenericType(genericArgs));
            }

            return RppNativeType.Create(RefType2.RuntimeType);
        }
        */
    }
}