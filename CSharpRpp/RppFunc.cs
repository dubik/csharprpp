using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public interface IRppFunc : IRppNode, IRppNamedNode
    {
        [NotNull]
        RppType ReturnType { get; }

        [NotNull]
        IRppParam[] Params { get; }

        IRppExpr Expr { get; }

        MethodInfo RuntimeType { get; set; }

        MethodBuilder Builder { get; set; }

        bool IsStatic { get; set; }
        bool IsPublic { get; set; }
        bool IsAbstract { get; set; }
        bool IsVariadic { get; set; }
        bool IsOverride { get; set; }
        bool IsConstructor { get; }

        bool IsSynthesized { get; set; }
        bool IsStub { get; set; }

        RppClass Class { get; set; }
        ConstructorInfo ConstructorInfo { get; set; }

        [NotNull]
        IList<RppVariantTypeParam> TypeParams { get; set; }
    }

    public class RppFunc : RppNamedNode, IRppFunc
    {
        public IRppExpr Expr { get; private set; }
        private RppScope _scope;

        public static IList<IRppParam> EmptyParams = new List<IRppParam>();

        public RppType ReturnType { get; private set; }
        public IRppParam[] Params { get; private set; }

        public MethodInfo RuntimeType
        {
            get { return Builder != null ? Builder.GetBaseDefinition() : null; }
            set { throw new NotImplementedException(); }
        }

        public MethodBuilder Builder { get; set; }
        public ConstructorBuilder ConstructorBuilder { get; set; }

        public bool IsStatic { get; set; }

        public bool IsPublic
        {
            get { return !Modifiers.Contains(ObjectModifier.OmPrivate); }
            set { throw new NotSupportedException(); }
        }

        public bool IsAbstract
        {
            get { return Expr is RppEmptyExpr; }
            set { throw new NotSupportedException(); }
        }

        public bool IsVariadic { get; set; }

        // TODO in RppFunc this modifiers are booleans and there is a separate set of modifiers, modifiers which
        // came from parser should be separated
        public bool IsOverride
        {
            get { return Modifiers.Contains(ObjectModifier.OmOverride); }
            set { throw new NotImplementedException(); }
        }

        public bool IsConstructor => Name == "this";

        public bool IsSynthesized { get; set; }
        public bool IsStub { get; set; }

        public RppClass Class { get; set; }
        public ConstructorInfo ConstructorInfo { get; set; }
        public HashSet<ObjectModifier> Modifiers { get; set; }

        public IList<RppVariantTypeParam> TypeParams { get; set; }

        public RType NewReturnType { get; private set; }

        public RppFunc([NotNull] string name) : base(name)
        {
            Initialize(EmptyParams, RppPrimitiveType.UnitTy, RppEmptyExpr.Instance);
        }

        public RppFunc([NotNull] string name, [NotNull] RppType returnType) : base(name)
        {
            Initialize(EmptyParams, returnType, RppEmptyExpr.Instance);
        }

        public RppFunc([NotNull] string name, [NotNull] IEnumerable<IRppParam> funcParams, [NotNull] RppType returnType)
            : base(name)
        {
            Initialize(funcParams, returnType, RppEmptyExpr.Instance);
        }

        public RppFunc([NotNull] string name, [NotNull] IEnumerable<IRppParam> funcParams, [NotNull] RppType returnType, [NotNull] IRppExpr expr) : base(name)
        {
            Initialize(funcParams, returnType, expr);
        }

        /// Returns <code>true</code> if signatures match
        public bool SignatureMatch(RppFunc otherFunc)
        {
            return Params.SequenceEqual(otherFunc.Params, ParamTypeComparer.Default);
        }

        private void Initialize([NotNull] IEnumerable<IRppParam> funcParams, [NotNull] RppType returnType, [NotNull] IRppExpr expr)
        {
            Params = funcParams.ToArray();
            ReturnType = returnType;
            Expr = expr;
            IsVariadic = Params.Any(param => param.IsVariadic);
            TypeParams = Collections.NoVariantTypeParams;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.VisitEnter(this);
            Expr.Accept(visitor);
            visitor.VisitExit(this);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _scope = new RppScope(scope);
            Params.ForEach(_scope.Add);
            // TODO this is probably not needed , because next line adds generic params to the scope
            TypeParams.ForEach(_scope.Add);

            foreach (var typeParam in TypeParams)
            {
                _scope.Add(typeParam.Name, RppNativeType.Create(typeParam.Runtime));
            }


            NodeUtils.Analyze(_scope, Params);
            Expr = NodeUtils.AnalyzeNode(_scope, Expr);

            var runtimeReturnType = ReturnType.Resolve(_scope);
            Debug.Assert(runtimeReturnType != null);
            ReturnType = runtimeReturnType;

            return this;
        }

        #region Equality

        protected bool Equals(RppFunc other)
        {
            return Equals(Name, other.Name) && Equals(ReturnType, other.ReturnType) && Equals(Params, other.Params) && IsStatic.Equals(other.IsStatic) &&
                   IsPublic.Equals(other.IsPublic) && IsAbstract.Equals(other.IsAbstract);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((RppFunc) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ReturnType.GetHashCode();
                hashCode = (hashCode * 397) ^ (Params != null ? Params.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsStatic.GetHashCode();
                hashCode = (hashCode * 397) ^ IsPublic.GetHashCode();
                hashCode = (hashCode * 397) ^ IsAbstract.GetHashCode();
                return hashCode;
            }
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return string.Format("{0} def {1}({2}) : {3}", ModifiersToString(), Name, ParamsToString(), ReturnType);
        }

        public string ModifiersToString()
        {
            IList<string> builder = new List<string>();
            if (IsStatic)
            {
                builder.Add("static");
            }

            if (IsPublic)
            {
                builder.Add("public");
            }

            if (IsAbstract)
            {
                builder.Add("abstract");
            }

            return string.Join(" ", builder);
        }

        private string ParamsToString()
        {
            return string.Join(", ", Params.Select(p => p.Name + ": " + p.Type.ToString()));
        }

        #endregion
    }

    public class ParamTypeComparer : IEqualityComparer<IRppParam>
    {
        public static readonly ParamTypeComparer Default = new ParamTypeComparer();

        public bool Equals(IRppParam x, IRppParam y)
        {
            return x.Type.Equals(y.Type);
        }

        public int GetHashCode(IRppParam obj)
        {
            return obj.GetHashCode();
        }
    }

    public interface IRppParam : IRppNamedNode, IRppExpr
    {
        int Index { get; set; }
        bool IsVariadic { get; set; }

        IRppParam CloneWithNewType(RppType newType);
    }

    [DebuggerDisplay("{Type.ToString()} {Name} [{RuntimeType}]")]
    public sealed class RppParam : RppMember, IRppParam
    {
        public override RppType Type { get; protected set; }
        public override RType Type2 { get; protected set; }

        public int Index { get; set; }

        public bool IsVariadic { get; set; }

        public RppParam([NotNull] string name, [NotNull] RppType type, bool variadic = false) : base(name)
        {
            Type = variadic ? new RppArrayType(type) : type;
            IsVariadic = variadic;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            var resolvedType = Type.Resolve(scope);
            Debug.Assert(resolvedType != null, "Can't resolve type");
            Type = resolvedType;
            return this;
        }

        public RType NewType { get; private set; }

        public IRppParam CloneWithNewType(RppType newType)
        {
            return new RppParam(Name, newType, IsVariadic);
        }
    }
}