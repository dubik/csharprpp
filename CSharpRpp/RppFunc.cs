using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

        ConstructorBuilder ConstructorBuilder { get; set; }

        bool IsStatic { get; set; }
        bool IsPublic { get; set; }
        bool IsAbstract { get; set; }
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
            get { return Builder.GetBaseDefinition(); }
            set { throw new NotImplementedException(); }
        }

        public MethodBuilder Builder { get; set; }
        public ConstructorBuilder ConstructorBuilder { get; set; }

        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public bool IsAbstract { get; set; }

        public RppFunc([NotNull] string name, [NotNull] IEnumerable<IRppParam> funcParams, [NotNull] RppType returnType)
            : base(name)
        {
            Initialize(funcParams, returnType, RppEmptyExpr.Instance);
        }

        public RppFunc([NotNull] string name, [NotNull] IEnumerable<IRppParam> funcParams, [NotNull] RppType returnType, [NotNull] IRppExpr expr) : base(name)
        {
            Initialize(funcParams, returnType, expr);
        }

        private void Initialize([NotNull] IEnumerable<IRppParam> funcParams, [NotNull] RppType returnType, [NotNull] IRppExpr expr)
        {
            IsPublic = true;
            Params = funcParams.ToArray();
            ReturnType = returnType;
            Expr = expr;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.VisitEnter(this);
            Expr.Accept(visitor);
            visitor.VisitExit(this);
        }

        public override void PreAnalyze(RppScope scope)
        {
            _scope = new RppScope(scope);
            Params.ForEach(_scope.Add);
            Expr.PreAnalyze(_scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
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

    public interface IRppParam : IRppNamedNode, IRppExpr
    {
        int Index { get; set; }
    }

    [DebuggerDisplay("{Type.ToString()} {Name} [{RuntimeType}]")]
    public sealed class RppParam : RppMember, IRppParam
    {
        public override RppType Type { get; protected set; }

        public int Index { get; set; }

        public RppParam([NotNull] string name, [NotNull] RppType type) : base(name)
        {
            Type = type;
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
    }
}