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

        public RppInteger(string valueStr)
        {
            Value = int.Parse(valueStr);
            Type = RppNativeType.Create(typeof (int));
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
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

        public RppString([NotNull] string valueStr)
        {
            Value = stripQuotes(valueStr);
            Type = RppNativeType.Create(typeof (string));
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
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

        public IEnumerable<IRppExpr> Args
        {
            get { return _argList.AsEnumerable(); }
        }

        private readonly IList<IRppExpr> _argList;

        [NotNull]
        public IRppFunc Function { get; private set; }

        public RppFuncCall([NotNull] string name, [NotNull] IList<IRppExpr> argList) : base(name)
        {
            _argList = argList;
        }

        public override void PreAnalyze(RppScope scope)
        {
            NodeUtils.PreAnalyze(scope, _argList);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            NodeUtils.Analyze(scope, _argList);

            if (Name != "ctor()")
            {
                var resolvedFunc = scope.Lookup(Name) as IRppFunc;
                Debug.Assert(resolvedFunc != null);
                Function = resolvedFunc;
                Type = Function.ReturnType;
            }
            else
            {
                // parent constructor is a special case, so don't resolve function
                Type = RppNativeType.Create(typeof (void));
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
            return _argList.SequenceEqual(other._argList) && Equals(Name, other.Name);
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
                return ((_argList != null ? _argList.GetHashCode() : 0) * 397) ^ (Function != null ? Function.GetHashCode() : 0);
            }
        }

        #endregion
    }

    public class RppMessage : RppFuncCall
    {
        public RppMessage([NotNull] string name, [NotNull] IList<IRppExpr> argList) : base(name, argList)
        {
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class RppBlockExpr : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }

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
                }
            }
            else
            {
                Type = RppNativeType.Create(typeof (void));
            }
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

    /*
     * Utils.size(a)
     * Utils.field
     * Utils.inst.name()
     */

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

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void PreAnalyze(RppScope scope)
        {
            Target.PreAnalyze(scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            Target.Analyze(scope);
            RppObjectType targetType = Target.Type as RppObjectType;

            Debug.Assert(targetType != null, "targetType != null");

            RppScope classScope = new RppScope(null);

            targetType.Class.Functions.ForEach(classScope.Add);
            targetType.Class.Fields.ForEach(classScope.Add);

            Path.PreAnalyze(classScope);
            Path.Analyze(classScope);

            return this;
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

    internal sealed class ClassAsMemberAdapter : RppMember
    {
        public override RppType Type { get; protected set; }

        public ClassAsMemberAdapter(RppClass clazz) : base(clazz.Name)
        {
            Type = new RppObjectType(clazz);
        }
    }

    public class RppId : RppMember
    {
        public override RppType Type { get; protected set; }

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
                var node = scope.Lookup(Name);
                RppMember member = null;

                // Reference to object, e.g. String.isNull(..)
                if (node is RppClass)
                {
                    RppClass clazz = (RppClass) node;
                    if (clazz.Kind != ClassKind.Object)
                    {
                        throw new Exception("Only objects are supported");
                    }

                    member = new ClassAsMemberAdapter(clazz);
                }
                else if (node is RppMember) // localVar, field
                {
                    member = node as RppMember;
                }

                Debug.Assert(member != null);

                Ref = member;
            }
        }

        public override IRppNode Analyze(RppScope scope)
        {
            Type = Ref.Type;

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