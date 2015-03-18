using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using JetBrains.Annotations;
using Mono.Collections.Generic;

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
    }

    public class RppLogicalBinOp : BinOp
    {
        private static readonly HashSet<string> LogicalOps = new HashSet<string> {"&&", "||"};
        private static readonly HashSet<string> RelationalOps = new HashSet<string> {"<", ">", "==", "!="};

        public RppLogicalBinOp([NotNull] string op, [NotNull] IRppExpr left, [NotNull] IRppExpr right) : base(op, left, right)
        {
            Type = RppNativeType.Create(typeof (bool));
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            base.Accept(visitor);
            visitor.Visit(this);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            base.Analyze(scope);

            // 10 < id
            // 10 > id
            // 10 != id
            // 10 == 10
            // 10 || left
            // isOk && notRunning

            if (LogicalOps.Contains(Op))
            {
                EnsureType(Left.Type, typeof (bool));
                EnsureType(Right.Type, typeof (bool));
            }
            else if (RelationalOps.Contains(Op))
            {
                // TODO check types
            }
            else
            {
                Debug.Assert(false);
            }

            return this;
        }

        private void EnsureType(RppType type, Type expectedType)
        {
            if (type.Runtime != expectedType)
            {
                throw new Exception(string.Format("Expected {0} type but got {1}", expectedType, type.Runtime));
            }
        }
    }

    public class TypeInference
    {
        private static Dictionary<Type, Type> ftChar = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Char},
            {Types.Byte, Types.Int},
            {Types.Short, Types.Int},
            {Types.Int, Types.Int},
            {Types.Long, Types.Long},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static Dictionary<Type, Type> ftByte = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Int},
            {Types.Byte, Types.Byte},
            {Types.Short, Types.Short},
            {Types.Int, Types.Int},
            {Types.Long, Types.Long},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static Dictionary<Type, Type> ftShort = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Int},
            {Types.Byte, Types.Short},
            {Types.Short, Types.Short},
            {Types.Int, Types.Int},
            {Types.Long, Types.Long},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static Dictionary<Type, Type> ftInt = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Int},
            {Types.Byte, Types.Int},
            {Types.Short, Types.Int},
            {Types.Int, Types.Int},
            {Types.Long, Types.Long},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static Dictionary<Type, Type> ftLong = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Long},
            {Types.Byte, Types.Long},
            {Types.Short, Types.Long},
            {Types.Int, Types.Long},
            {Types.Long, Types.Long},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static Dictionary<Type, Type> ftFloat = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Float},
            {Types.Byte, Types.Float},
            {Types.Short, Types.Float},
            {Types.Int, Types.Float},
            {Types.Long, Types.Float},
            {Types.Float, Types.Float},
            {Types.Double, Types.Double},
        };

        private static Dictionary<Type, Type> ftDouble = new Dictionary<Type, Type>
        {
            {Types.Char, Types.Double},
            {Types.Byte, Types.Double},
            {Types.Short, Types.Double},
            {Types.Int, Types.Double},
            {Types.Long, Types.Double},
            {Types.Float, Types.Double},
            {Types.Double, Types.Double},
        };

        private static Dictionary<Type, Dictionary<Type, Type>> convTable = new Dictionary<Type, Dictionary<Type, Type>>()
        {
            {Types.Char, ftChar},
            {Types.Byte, ftByte},
            {Types.Short, ftShort},
            {Types.Int, ftInt},
            {Types.Long, ftLong},
            {Types.Float, ftFloat},
            {Types.Double, ftDouble},
        };

        public static Type ResolveCommonType(Type left, Type right)
        {
            if (left.IsNumeric() && right.IsNumeric())
            {
            }
            else
            {
                Debug.Fail("Not done yet");
            }

            return null;
        }
    }

    public class RppArithmBinOp : BinOp
    {
        public RppArithmBinOp([NotNull] string op, [NotNull] IRppExpr left, [NotNull] IRppExpr right) : base(op, left, right)
        {
        }

        public override IRppNode Analyze(RppScope scope)
        {
            base.Analyze(scope);
            if (Left.Type.Runtime.IsNumeric())
            {
            }
            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            base.Accept(visitor);
            visitor.Visit(this);
        }
    }

    [DebuggerDisplay("Op = {Op}")]
    public class BinOp : RppNode, IRppExpr
    {
        public RppType Type { get; protected set; }

        [NotNull]
        public string Op { get; private set; }

        public IRppExpr Left { get; private set; }
        public IRppExpr Right { get; private set; }

        public BinOp([NotNull] string op, [NotNull] IRppExpr left, [NotNull] IRppExpr right)
        {
            Op = op;
            Left = left;
            Right = right;
        }

        public override void PreAnalyze(RppScope scope)
        {
            Left.PreAnalyze(scope);
            Right.PreAnalyze(scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            Left = Left.Analyze(scope) as IRppExpr;
            Debug.Assert(Left != null);
            Right = Right.Analyze(scope) as IRppExpr;
            Debug.Assert(Right != null);

            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            Left.Accept(visitor);
            Right.Accept(visitor);
        }

        #region Equality

        protected bool Equals(BinOp other)
        {
            return string.Equals(Op, other.Op) && Equals(Left, other.Left) && Equals(Right, other.Right);
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

    [DebuggerDisplay("Int: {Value}")]
    public sealed class RppInteger : RppLiteralBase<int>
    {
        public RppInteger(int val) : base(val)
        {
        }

        public RppInteger(string valueStr) : base(valueStr)
        {
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        protected override int Parse(string valueStr)
        {
            return int.Parse(valueStr);
        }
    }

    public abstract class RppLiteralBase<T> : RppNode, IRppExpr
    {
        public T Value { get; private set; }

        public RppType Type { get; private set; }

        protected RppLiteralBase(string valueStr)
        {
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            Initialize(Parse(valueStr));
        }

        private void Initialize(T value)
        {
            Value = value;
            Type = RppNativeType.Create(typeof (T));
        }

        protected RppLiteralBase(T value)
        {
            Initialize(value);
        }

        protected abstract T Parse(string valueStr);

        #region Equality

        protected bool Equals(RppLiteralBase<T> other)
        {
            return Value.Equals(other.Value) && Type.Equals(other.Type);
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
            return Equals((RppLiteralBase<T>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Value.GetHashCode() * 397) ^ Type.GetHashCode();
            }
        }

        #endregion
    }

    [DebuggerDisplay("Float: {Value}")]
    public sealed class RppFloat : RppLiteralBase<float>
    {
        public RppFloat(string valueStr) : base(valueStr)
        {
        }

        public RppFloat(float value) : base(value)
        {
        }

        protected override float Parse(string valueStr)
        {
            return float.Parse(valueStr);
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    [DebuggerDisplay("String: {Value}")]
    public sealed class RppString : RppLiteralBase<string>
    {
        public RppString([NotNull] string valueStr) : base(valueStr)
        {
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        protected override string Parse(string valueStr)
        {
            return StripQuotes(valueStr);
        }

        [NotNull]
        private static string StripQuotes([NotNull] string str)
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

        private IList<IRppExpr> _argList;

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

                if (resolvedFunc.IsVariadic)
                {
                    List<IRppParam> funcParams = resolvedFunc.Params.ToList();
                    int variadicIndex = funcParams.FindIndex(p => p.IsVariadic);
                    var args = _argList.Take(variadicIndex).ToList();
                    var variadicParams = _argList.Where((arg, index) => index >= variadicIndex).ToList();
                    _argList = args;

                    IRppParam variadicParam = funcParams.Find(p => p.IsVariadic);
                    var varidadicType = variadicParam.Type as RppArrayType;
                    Debug.Assert(varidadicType != null, "varidadicType != null");
                    RppArray variadicArgsArray = new RppArray(varidadicType.SubType, variadicParams);
                    variadicArgsArray.PreAnalyze(scope);
                    variadicArgsArray = (RppArray) variadicArgsArray.Analyze(scope);
                    _argList.Add(variadicArgsArray);
                }

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

    public class RppArray : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }

        public IEnumerable<IRppExpr> Initializers { get; private set; }

        public int Size { get; private set; }

        public RppArray(RppType type, IEnumerable<IRppExpr> initializers)
        {
            Type = new RppArrayType(type);
            Initializers = initializers;
            Size = Initializers.Count();
        }

        public override IRppNode Analyze(RppScope scope)
        {
            var resolvedType = Type.Resolve(scope);
            Debug.Assert(resolvedType != null);
            Type = resolvedType;

            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
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

    public class RppBox : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }

        public IRppExpr Expression { get; private set; }

        public RppBox([NotNull] IRppExpr expr)
        {
            Expression = expr;
            Debug.Assert(expr.Type.Runtime.IsValueType);
            Type = RppNativeType.Create(typeof (object));
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        protected bool Equals(RppBox other)
        {
            return Type.Equals(other.Type) && Equals(Expression, other.Expression);
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
            return Equals((RppBox) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Type.GetHashCode() * 397) ^ (Expression != null ? Expression.GetHashCode() : 0);
            }
        }
    }
}