using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Expr;
using CSharpRpp.Parser;
using JetBrains.Annotations;
using Mono.Collections.Generic;

namespace CSharpRpp
{
    public class RppNew : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }
        public Type RuntimeType { get; private set; }

        [NotNull]
        public IRppClass RefClass { get; private set; }

        public IEnumerable<RppType> ArgumentTypes { get; private set; }

        private readonly string _typeName;

        public IEnumerable<IRppExpr> Args => _constructorsParams.AsEnumerable();

        private readonly IList<IRppExpr> _constructorsParams;

        public IEnumerable<RppVariantTypeParam> TypeArgs => _typeArgs.AsEnumerable();

        private readonly IList<RppVariantTypeParam> _typeArgs;

        public IRppFunc Constructor { get; private set; }

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

            var refClass = scope.Lookup(_typeName) as IRppClass;
            Debug.Assert(refClass != null);
            RefClass = refClass;

            _typeArgs.ForEach(arg => arg.Resolve(scope));

            // TODO Find correct constructor
            var constructors = RefClass.Constructors;

            CreateNameToVariantTypeParam();

            var candidates = OverloadQuery.Find(Args.Select(a => a.Type).ToList(), constructors, TypesComparator, CanCast).ToList();
            if (candidates.Count != 1)
            {
                throw new Exception("Can't figure out which overload to use");
            }

            Constructor = candidates[0];

            Type = CreateType();

            return this;
        }

        private void CreateNameToVariantTypeParam()
        {
            _nameToGenericArg = new Dictionary<string, RppVariantTypeParam>();
            int index = 0;
            foreach (var typeParam in RefClass.TypeParams)
            {
                _nameToGenericArg.Add(typeParam.Name, _typeArgs[index]);
                index++;
            }
        }

        private static bool CanCast(RppType source, RppType target)
        {
            return ImplicitCast.CanCast(source, target);
        }

        private bool TypesComparator(RppType source, RppType target)
        {
            if (target.Runtime.IsGenericParameter)
            {
                RppVariantTypeParam typeParam = _nameToGenericArg[target.Runtime.Name];
                return source.Runtime == typeParam.Runtime;
            }

            return source.Equals(target);
        }

        /// <summary>
        /// Creates type of 'new' expression, if required it creates generic arguments.
        /// </summary>
        /// <returns>type of the 'new' expression</returns>
        private ResolvedType CreateType()
        {
            if (_typeArgs.Count > 0)
            {
                var genericArgs = _typeArgs.Select(arg => arg.Runtime).ToArray();
                return new RppGenericObjectType((RppClass) RefClass, genericArgs, RefClass.RuntimeType.MakeGenericType(genericArgs));
            }

            return RppNativeType.Create(RefClass.RuntimeType);
        }
    }
}