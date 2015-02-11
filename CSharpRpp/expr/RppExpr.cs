using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

namespace CSharpRpp
{
    public interface IRppExpr : IRppNode
    {
        RppType Type { get; }
        Type RuntimeType { get; }
        void Codegen(ILGenerator generator);
    }

    // Base class for RppId and RppFuncCall
    public abstract class RppMember : RppNamedNode, IRppExpr
    {
        public abstract RppType Type { get; protected set; }
        public abstract Type RuntimeType { get; protected set; }

        protected RppMember(string name) : base(name)
        {
        }

        public abstract void Codegen(ILGenerator generator);
    }

    public class RppEmptyExpr : RppNode, IRppExpr
    {
        public RppType Type
        {
            get { return RppPrimitiveType.RppUnit; }
        }

        public Type RuntimeType
        {
            get { return typeof (void); }
        }

        public void Codegen(ILGenerator generator)
        {
        }
    }

    [DebuggerDisplay("Op = {_op}")]
    public class BinOp : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        private readonly string _op;
        private IRppExpr _left;
        private IRppExpr _right;

        public BinOp(string op, IRppExpr left, IRppExpr right)
        {
            _op = op;
            _left = left;
            _right = right;
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

        public void Codegen(ILGenerator generator)
        {
            _left.Codegen(generator);
            _right.Codegen(generator);
            switch (_op)
            {
                case "+":
                    generator.Emit(OpCodes.Add);
                    break;
                case "-":
                    generator.Emit(OpCodes.Sub);
                    break;
                case "*":
                    generator.Emit(OpCodes.Mul);
                    break;
                case "/":
                    generator.Emit(OpCodes.Div);
                    break;
                default:
                    Debug.Assert(false, "Don't know how to handle " + _op);
                    break;
            }
        }

        #region Equality

        protected bool Equals(BinOp other)
        {
            return string.Equals(_op, other._op) && Equals(_left, other._left) && Equals(_right, other._right);
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
                var hashCode = (_op != null ? _op.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }

    [DebuggerDisplay("Int: {_value}")]
    public class RppInteger : RppNode, IRppExpr
    {
        private readonly int _value;

        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        public RppInteger(string valueStr)
        {
            _value = int.Parse(valueStr);
            RuntimeType = typeof (int);
            Type = new RppNativeType(RuntimeType);
        }

        public void Codegen(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldc_I4, _value);
        }

        #region Equality

        protected bool Equals(RppInteger other)
        {
            return _value == other._value;
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
            return _value;
        }

        #endregion
    }

    [DebuggerDisplay("String: {_value}")]
    public class RppString : RppNode, IRppExpr
    {
        private readonly string _value;

        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        public RppString(string valueStr)
        {
            _value = stripQuotes(valueStr);
            RuntimeType = typeof (string);
            Type = new RppNativeType(RuntimeType);
        }

        public void Codegen(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldstr, _value);
        }

        private string stripQuotes(string str)
        {
            if (str.Length > 1 && str[0] == '"' && str[str.Length - 1] == '"')
            {
                return str.Substring(1, str.Length - 2);
            }

            return str;
        }
    }

    [DebuggerDisplay("FuncCall - Name: {_funcName}, Params: {_paramList.Count}")]
    public class RppFuncCall : RppMember
    {
        public override RppType Type { get; protected set; }
        public override Type RuntimeType { get; protected set; }

        private readonly IList<IRppExpr> _paramList;
        private IRppFunc _func;

        public RppFuncCall(string name, IList<IRppExpr> paramList) : base(name)
        {
            _paramList = paramList;
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _func = scope.Lookup(Name) as IRppFunc;
            Debug.Assert(_func != null);
            Type = _func.ReturnType;
            RuntimeType = _func.RuntimeReturnType;
            return this;
        }

        public override void Codegen(ILGenerator generator)
        {
            _paramList.ForEach(p => p.Codegen(generator));
            generator.Emit(OpCodes.Call, _func.RuntimeFuncInfo);
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
                return ((_paramList != null ? _paramList.GetHashCode() : 0) * 397) ^ (_func != null ? _func.GetHashCode() : 0);
            }
        }

        #endregion
    }

    public class RppBlockExpr : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }

        public Type RuntimeType { get; private set; }

        private IList<IRppNode> _exprs;

        public RppBlockExpr(IList<IRppNode> exprs)
        {
            _exprs = exprs;
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

        private IRppExpr _ref;

        public RppId(string name) : base(name)
        {
        }

        public override void PreAnalyze(RppScope scope)
        {
            _ref = scope.Lookup(Name) as IRppExpr;
            Debug.Assert(_ref != null);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            Type = _ref.Type;
            RuntimeType = _ref.RuntimeType;

            return this;
        }

        public override void Codegen(ILGenerator generator)
        {
            _ref.Codegen(generator);
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
            return (Name != null ? Name.GetHashCode() : 0);
        }

        #endregion
    }
}