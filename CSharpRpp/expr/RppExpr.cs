using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace CSharpRpp
{
    // Base class for RppId and RppFuncCall
    public abstract class RppMember : RppNamedNode, IRppExpr
    {
        public abstract RppType Type { get; protected set; }
        public abstract Type RuntimeType { get; protected set; }

        protected RppMember(string name) : base(name)
        {
        }
    }

    public class RppEmptyExpr : RppNode, IRppExpr
    {
        public static RppEmptyExpr Instance = new RppEmptyExpr();

        public RppType Type
        {
            get { return RppPrimitiveType.UnitTy; }
        }

        public Type RuntimeType
        {
            get { return typeof (void); }
        }

        public void Codegen(ILGenerator generator)
        {
        }
    }

    [DebuggerDisplay("Op = {Op}")]
    public class BinOp : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        [NotNull]
        public string Op { get; private set; }

        private IRppExpr _left;
        private IRppExpr _right;

        public BinOp([NotNull] string op, [NotNull] IRppExpr left, [NotNull] IRppExpr right)
        {
            Op = op;
            _left = left;
            _right = right;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            _left.Accept(visitor);
            _right.Accept(visitor);
            visitor.Visit(this);
        }

        public override void PreAnalyze(RppScope scope)
        {
            _left.PreAnalyze(scope);
            _right.PreAnalyze(scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _left = _left.Analyze(scope) as IRppExpr;
            Debug.Assert(_left != null);
            _right = _right.Analyze(scope) as IRppExpr;
            Debug.Assert(_right != null);

            return this;
        }

        #region Equality

        protected bool Equals(BinOp other)
        {
            return string.Equals(Op, other.Op) && Equals(_left, other._left) && Equals(_right, other._right);
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
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((BinOp) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Op != null ? Op.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }

    [DebuggerDisplay("Int: {_value}")]
    public class RppInteger : RppNode, IRppExpr
    {
        public int Value { get; private set; }

        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        public RppInteger(string valueStr)
        {
            Value = int.Parse(valueStr);
            RuntimeType = typeof (int);
            Type = new RppNativeType(RuntimeType);
        }

        public void Codegen(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldc_I4, Value);
        }

        #region Equality

        protected bool Equals(RppInteger other)
        {
            return Value == other.Value;
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
            return Equals((RppInteger) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        #endregion
    }

    [DebuggerDisplay("String: {Value}")]
    public class RppString : RppNode, IRppExpr
    {
        [NotNull]
        public string Value { get; private set; }

        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        public RppString([NotNull] string valueStr)
        {
            Value = stripQuotes(valueStr);
            RuntimeType = typeof (string);
            Type = new RppNativeType(RuntimeType);
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public void Codegen(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldstr, Value);
        }

        [NotNull]
        private static string stripQuotes([NotNull] string str)
        {
            if (str.Length > 1 && str[0] == '"' && str[str.Length - 1] == '"')
            {
                return str.Substring(1, str.Length - 2);
            }

            return str;
        }
    }

    public class RppFuncCall : RppMember
    {
        public override RppType Type { get; protected set; }
        public override Type RuntimeType { get; protected set; }

        private readonly IList<IRppExpr> _paramList;

        [NotNull]
        public IRppFunc Function { get; private set; }

        public RppFuncCall([NotNull] string name, [NotNull] IList<IRppExpr> paramList) : base(name)
        {
            _paramList = paramList;
        }

        public override IRppNode Analyze(RppScope scope)
        {
            if (Name != "ctor()")
            {
                var resolvedFunc = scope.Lookup(Name) as IRppFunc;
                Debug.Assert(resolvedFunc != null);
                Function = resolvedFunc;
                Type = Function.ReturnType;
                RuntimeType = Function.RuntimeReturnType;
            }
            else
            {
                // parent constructor is a special case, so don't resolve function
                Type = RppPrimitiveType.UnitTy;
                RuntimeType = typeof (void);
            }

            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("Call: \"{0}\"", Name);
        }

        #region Equality

        protected bool Equals(RppFuncCall other)
        {
            return _paramList.SequenceEqual(other._paramList) && Equals(Name, other.Name);
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
            return Equals((RppFuncCall) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_paramList != null ? _paramList.GetHashCode() : 0) * 397) ^ (Function != null ? Function.GetHashCode() : 0);
            }
        }

        #endregion
    }

    public class RppBlockExpr : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        private IList<IRppNode> _exprs;

        public RppBlockExpr([NotNull] IList<IRppNode> exprs)
        {
            _exprs = exprs;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.VisitEnter(this);
            _exprs.ForEach(expr => expr.Accept(visitor));
            visitor.VisitExit(this);
        }

        public override void PreAnalyze(RppScope scope)
        {
            NodeUtils.PreAnalyze(scope, _exprs);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _exprs = NodeUtils.Analyze(scope, _exprs);

            InitializeType();

            return this;
        }

        private void InitializeType()
        {
            if (_exprs.Any() && _exprs.Last() is IRppExpr)
            {
                var lastExpr = _exprs.Last() as IRppExpr;
                if (lastExpr != null)
                {
                    Type = lastExpr.Type;
                    RuntimeType = lastExpr.RuntimeType;
                }
            }
            else
            {
                RuntimeType = typeof (void);
                Type = new RppNativeType(RuntimeType);
            }
        }

        public void Codegen(ILGenerator generator)
        {
            //_exprs.ForEach(e => e.Codegen(generator));
        }

        #region Equality

        protected bool Equals(RppBlockExpr other)
        {
            return _exprs.SequenceEqual(other._exprs);
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

            return Equals((RppBlockExpr) obj);
        }

        public override int GetHashCode()
        {
            throw new Exception("Not implemented");
        }

        #endregion
    }

    public class RppSelector : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }
        public Type RuntimeType { get; private set; }

        public IRppExpr Target { get; private set; }
        public RppMember Path { get; private set; }

        public RppSelector(IRppExpr target, RppMember path)
        {
            Target = target;
            Path = path;
        }

        public void Codegen(ILGenerator generator)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return string.Format("{{ {0} -> {1} }}", Target, Path);
        }

        #region Equality

        protected bool Equals(RppSelector other)
        {
            return Equals(Target, other.Target) && Equals(Path, other.Path);
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
            return Equals((RppSelector) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Target != null ? Target.GetHashCode() : 0) * 397) ^ (Path != null ? Path.GetHashCode() : 0);
            }
        }

        #endregion
    }

    public class RppId : RppMember
    {
        public override RppType Type { get; protected set; }
        public override Type RuntimeType { get; protected set; }


        [CanBeNull]
        public RppMember Ref { get; private set; }

        public RppId([NotNull] string name) : base(name)
        {
        }

        public RppId([NotNull] string name, [NotNull] RppMember targetRef) : base(name)
        {
            Ref = targetRef;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void PreAnalyze(RppScope scope)
        {
            if (Ref == null)
            {
                var rppExpr = scope.Lookup(Name) as RppMember;
                Debug.Assert(rppExpr != null);
                Ref = rppExpr;
            }
        }

        public override IRppNode Analyze(RppScope scope)
        {
            Type = Ref.Type;
            RuntimeType = Ref.RuntimeType;

            return this;
        }

        public override string ToString()
        {
            return string.Format("Id: \"{0}\"", Name);
        }

        #region Equality

        protected bool Equals(RppId other)
        {
            return string.Equals(Name, other.Name);
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
            return Equals((RppId) obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        #endregion
    }
}