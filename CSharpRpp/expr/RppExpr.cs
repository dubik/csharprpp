using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr.Runtime;
using CSharpRpp.Exceptions;
using CSharpRpp.Parser;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using static CSharpRpp.TypeSystem.ResolvableType;

namespace CSharpRpp
{
    // Base class for RppId and RppFuncCall
    public abstract class RppMember : RppNamedNode, IRppExpr
    {
        public abstract ResolvableType Type { get; protected set; }

        public RType TargetType2 { get; set; }

        protected RppMember(string name) : base(name)
        {
        }
    }

    public class RppEmptyExpr : RppNode, IRppExpr
    {
        public static RppEmptyExpr Instance = new RppEmptyExpr();

        public ResolvableType Type => UnitTy;
    }

    public class RppNull : RppNode, IRppExpr
    {
        public ResolvableType Type => NullTy;

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class RppLogicalBinOp : BinOp
    {
        internal static readonly HashSet<string> Ops = new HashSet<string> {"&&", "||"};

        public RppLogicalBinOp([NotNull] string op, [NotNull] IRppExpr left, [NotNull] IRppExpr right) : base(op, left, right)
        {
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            base.Analyze(scope, diagnostic);

            // 10 || left
            // isOk && notRunning

            EnsureType(Left.Type.Value, RppTypeSystem.BooleanTy);
            EnsureType(Right.Type.Value, RppTypeSystem.BooleanTy);

            return this;
        }

        private static void EnsureType(RType type, RType expectedType)
        {
            if (!Equals(type, expectedType))
            {
                throw new Exception($"Expected {expectedType} type but got {type}");
            }
        }
    }

    public class RppRelationalBinOp : BinOp
    {
        internal static readonly HashSet<string> Ops = new HashSet<string> {"<", ">", "==", "!="};

        // 10 < id
        // 10 > id
        // 10 != id
        // 10 == 10
        public RppRelationalBinOp([NotNull] string op, [NotNull] IRppExpr left, [NotNull] IRppExpr right) : base(op, left, right)
        {
            Type = BooleanTy;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            base.Analyze(scope, diagnostic);

            return this;
        }
    }

    public class RppArithmBinOp : BinOp
    {
        internal static readonly HashSet<string> Ops = new HashSet<string> {"+", "-", "/", "*", "%"};

        public RppArithmBinOp([NotNull] string op, [NotNull] IRppExpr left, [NotNull] IRppExpr right) : base(op, left, right)
        {
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            base.Analyze(scope, diagnostic);

            Type = new ResolvableType(TypeInference.ResolveCommonType(Left.Type.Value, Right.Type.Value));

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
        public ResolvableType Type { get; protected set; }

        [NotNull]
        public string Op { get; private set; }

        public IRppExpr Left { get; private set; }
        public IRppExpr Right { get; private set; }

        public static BinOp Create([NotNull] string op, [NotNull] IRppExpr left, [NotNull] IRppExpr right)
        {
            if (RppArithmBinOp.Ops.Contains(op))
            {
                return new RppArithmBinOp(op, left, right);
            }

            if (RppLogicalBinOp.Ops.Contains(op))
            {
                return new RppLogicalBinOp(op, left, right);
            }

            if (RppRelationalBinOp.Ops.Contains(op))
            {
                return new RppRelationalBinOp(op, left, right);
            }

            if (op == "=")
            {
                return new RppAssignOp(left, right);
            }

            Debug.Fail("Don't know how to handle op: " + op);
            return null;
        }

        protected BinOp([NotNull] string op, [NotNull] IRppExpr left, [NotNull] IRppExpr right)
        {
            Op = op;
            Left = left;
            Right = right;
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            Left = Left.Analyze(scope, diagnostic) as IRppExpr;
            Debug.Assert(Left != null);
            Right = Right.Analyze(scope, diagnostic) as IRppExpr;
            Debug.Assert(Right != null);

            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
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
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((BinOp) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = (Op != null ? Op.GetHashCode() : 0);
            return hashCode;
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

        public ResolvableType Type { get; private set; }

        protected RppLiteralBase(string valueStr)
        {
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            Initialize(Parse(valueStr));
        }

        private void Initialize(T value)
        {
            Value = value;
            Type = new ResolvableType(RppTypeSystem.ImportPrimitive(typeof (T)));
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

    public sealed class RppBooleanLiteral : RppLiteralBase<bool>
    {
        public RppBooleanLiteral([NotNull] string valueStr) : base(valueStr)
        {
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        protected override bool Parse(string valueStr)
        {
            return bool.Parse(valueStr);
        }
    }

    public class RppFuncCall : RppMember
    {
        public sealed override ResolvableType Type { get; protected set; }

        public IEnumerable<IRppExpr> Args => ArgList.AsEnumerable();

        protected IList<IRppExpr> ArgList;

        [NotNull]
        public RppMethodInfo Function { get; }

        public bool IsConstructorCall => Name == "this";

        private readonly IList<ResolvableType> _typeArgs;

        public IEnumerable<ResolvableType> TypeArgs => _typeArgs;

        public RppFuncCall([NotNull] string name, [NotNull] IList<IRppExpr> argList) : this(name, argList, Collections.NoResolvableTypes)
        {
        }

        public RppFuncCall([NotNull] string name, [NotNull] IList<IRppExpr> argList, [NotNull] IList<ResolvableType> typeArgList) : base(name)
        {
            ArgList = argList;
            _typeArgs = typeArgList;
        }

        public RppFuncCall([NotNull] string name, [NotNull] IList<IRppExpr> argList, RppMethodInfo function, ResolvableType type,
            [NotNull] IList<ResolvableType> typeArgList)
            : this(name, argList, typeArgList)
        {
            Function = function;
            Type = type;
        }

        private static IList<IRppExpr> ReplaceUndefinedClosureTypesIfNeeded(IEnumerable<IRppExpr> exprs, RppParameterInfo[] funcParams)
        {
            IEnumerable<IRppExpr> rppExprs = exprs as IList<IRppExpr> ?? exprs.ToList();
            IEnumerable<ResolvableType> funcParamTypes = ExpandVariadicParam(funcParams, rppExprs.Count()).ToList();
            return rppExprs.Zip(funcParamTypes, TypeInference.ReplaceUndefinedClosureTypesIfNeeded).ToList();
        }

        /// <summary>
        /// Expands variadic argument into arguments to make specified amount of arguments.
        /// For example:
        /// <code>
        /// [String, Int*],4
        /// </code>
        /// would be expanded into
        /// [String, Int, Int, Int]
        /// </summary>
        /// <param name="funcParams">input list of params</param>
        /// <param name="totalNumberOfParams">how many params needs to be</param>
        /// <returns>expanded list of types as in example above</returns>
        private static IEnumerable<ResolvableType> ExpandVariadicParam(RppParameterInfo[] funcParams, int totalNumberOfParams)
        {
            List<RType> expandedList = funcParams.Where(p => !p.IsVariadic).Select(p => p.Type).ToList();
            if (funcParams.Any() && funcParams.Last().IsVariadic)
            {
                // Variadic param is an array, so extract sub type.
                RType variadicParamType = ((RType) funcParams.Last().Type).SubType();
                Enumerable.Range(0, totalNumberOfParams - expandedList.Count).ForEach(i => expandedList.Add(variadicParamType));
            }

            return expandedList.Select(t => new ResolvableType(t));
        }

        // TODO This needs to be rewritten.
        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            // Skip closures because they may have missing types
            ArgList = NodeUtils.AnalyzeWithPredicate(scope, ArgList, node => !(node is RppClosure), diagnostic);

            _typeArgs.ForEach(arg => arg.Resolve(scope));

            List<RType> genericArguments = _typeArgs.Select(ta => ta.Value).ToList();

            // Search for a function which matches signature and possible gaps in types (for closures)
            FunctionResolution.ResolveResults resolveResults = FunctionResolution.ResolveFunction(Name, ArgList, genericArguments, scope);
            if (resolveResults == null)
            {
                throw SemanticExceptionFactory.MemberNotFound(Token, TargetType2.Name);
            }

            IList<IRppExpr> args = ReplaceUndefinedClosureTypesIfNeeded(ArgList, resolveResults.Function.Parameters);
            //var args = ArgList;
            NodeUtils.AnalyzeWithPredicate(scope, args, node => node is RppClosure, diagnostic);
            if (resolveResults.Function.IsVariadic)
            {
                args = RewriteArgListForVariadicParameter(scope, args, resolveResults.Function);
            }

            return resolveResults.RewriteFunctionCall(TargetType2, Name, args, genericArguments);
        }

        private static IEnumerable<RppClosure> ArgListOfClosures(IEnumerable<IRppExpr> args)
        {
            return args.OfType<RppClosure>();
        }

        private static IEnumerable<IRppExpr> ArgListWithoutClosures(IEnumerable<IRppExpr> args)
        {
            return args.Where(arg => !(arg is RppClosure));
        }

        /// <summary>
        /// Rewrites given list of arguments so that they can be used to call a function with variadic parameter.
        /// For instance:
        /// <code>def fun(id: Int, list: Float*) : Unit</code>
        /// is called with <code>fun(1, 4.5f, 3.5f)</code>
        /// Function rewrites it the following way [1, [4.5f, 3.5f]]
        /// </summary>
        /// <param name="scope">current scope, needed to anaylize array which contains variable number of args</param>
        /// <param name="args">list of expressions</param>
        /// <param name="function">target function</param>
        /// <returns>list of arguments</returns>
        private static List<IRppExpr> RewriteArgListForVariadicParameter(SymbolTable scope, IList<IRppExpr> args, RppMethodInfo function)
        {
            List<RppParameterInfo> funcParams = function.Parameters.ToList();
            int variadicIndex = funcParams.FindIndex(p => p.IsVariadic);
            List<IRppExpr> newArgList = args.Take(variadicIndex).ToList();
            var variadicParams = args.Where((arg, index) => index >= variadicIndex).ToList();

            RppParameterInfo variadicParam = funcParams.Find(p => p.IsVariadic);

            RType elementType = GetElementType(variadicParam.Type);
            variadicParams = variadicParams.Select(param => BoxIfValueType(param, elementType)).ToList();

            RppArray variadicArgsArray = new RppArray(elementType, variadicParams);
            variadicArgsArray = (RppArray) variadicArgsArray.Analyze(scope, null);

            newArgList.Add(variadicArgsArray);
            return newArgList;
        }

        private static RType GetElementType(RType arrayType)
        {
            return arrayType.SubType();
        }

        private static IRppExpr BoxIfValueType(IRppExpr arg, RType targetType)
        {
            if ((Equals(arg.Type.Value, RppTypeSystem.IntTy) || Equals(arg.Type.Value, RppTypeSystem.FloatTy)) && targetType == RppTypeSystem.AnyTy)
            {
                return new RppBox(arg);
            }

            return arg;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"Call: \"{Name}\"";
        }

        #region Equality

        protected bool Equals(RppFuncCall other)
        {
            return ArgList.SequenceEqual(other.ArgList) && Equals(Name, other.Name);
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
                return ((ArgList?.GetHashCode() ?? 0) * 397) ^ Function.GetHashCode();
            }
        }

        #endregion
    }

    public class RppBaseConstructorCall : RppFuncCall
    {
        public RppMethodInfo BaseConstructor { get; private set; }

        public ResolvableType BaseClassType2 { get; }

        public static RppBaseConstructorCall Object = new RppBaseConstructorCall(AnyTy, Collections.NoExprs);

        public RppBaseConstructorCall([NotNull] ResolvableType baseClassType, [NotNull] IList<IRppExpr> argList)
            : base("ctor()", argList)
        {
            BaseClassType2 = baseClassType;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public void ResolveBaseClass(SymbolTable scope)
        {
            BaseClassType2.Resolve(scope);
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            NodeUtils.Analyze(scope, ArgList, diagnostic);

            SymbolTable sc = new SymbolTable(null, BaseClassType2.Value);
            BaseConstructor = FindMatchingConstructor(ArgList, sc);
            Type = UnitTy;
            return this;
        }

        private RppMethodInfo FindMatchingConstructor(IEnumerable<IRppExpr> args, SymbolTable scope)
        {
            IReadOnlyCollection<RppMethodInfo> overloads = scope.LookupFunction("this");
            IEnumerable<RType> typeArgs = Collections.NoRTypes;
            var candidates = OverloadQuery.Find(args, typeArgs, overloads, new DefaultTypesComparator(null)).ToList();
            if (candidates.Count > 1)
            {
                throw new Exception("Can't figure out which overload to use");
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[0];
        }

        /*
        private static bool CanCast(RppType source, RppType target)
        {
            return OverloadQuery.DefaultCanCast(source, target);
        }

        private bool TypesComparator(RppType source, RppType target)
        {
            if (target.Runtime.IsGenericParameter)
            {
                RppType specializedTarget = BaseClassTypeArgs.ElementAt(target.Runtime.GenericParameterPosition);
                return OverloadQuery.DefaultTypesComparator(source, specializedTarget);
            }

            return OverloadQuery.DefaultTypesComparator(source, target);
        }
        */

        public override string ToString()
        {
            return $"{BaseClassType2}::{Name}";
        }
    }

    public class RppArray : RppNode, IRppExpr
    {
        public ResolvableType Type { get; private set; }

        public IEnumerable<IRppExpr> Initializers { get; }

        public int Size { get; private set; }

        private readonly RType _elementType;

        public RppArray(RType type, IEnumerable<IRppExpr> initializers)
        {
            _elementType = type;
            Initializers = initializers;
            Size = Initializers.Count();
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            Type = new ResolvableType(_elementType.MakeArrayType());

            //var resolvedType = Type.Resolve(scope);
            //Debug.Assert(resolvedType != null);
            //Type = resolvedType;

            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class RppBlockExpr : RppNode, IRppExpr
    {
        public ResolvableType Type { get; private set; }

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

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            _exprs = NodeUtils.Analyze(scope, _exprs, diagnostic);

            InitializeType();

            return this;
        }

        private void InitializeType()
        {
            if (_exprs.Any() && _exprs.Last() is IRppExpr && !(_exprs.Last() is RppVar))
            {
                var lastExpr = _exprs.Last() as IRppExpr;
                if (lastExpr != null)
                {
                    Type = lastExpr.Type;
                }
            }
            else
            {
                Type = new ResolvableType(RppTypeSystem.UnitTy);
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
        public ResolvableType Type => Path.Type;

        public IRppExpr Target { get; }
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

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            Target.Analyze(scope, diagnostic);
            RType targetType = Target.Type.Value;

            Debug.Assert(targetType != null, "targetType != null");

            SymbolTable classScope = new SymbolTable(scope, targetType);

            Path.TargetType2 = targetType;
            Path = (RppMember) Path.Analyze(classScope, diagnostic);
            return this;
        }

        public override string ToString()
        {
            return $"{{ {Target} -> {Path} }}";
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
                return ((Target?.GetHashCode() ?? 0) * 397) ^ (Path?.GetHashCode() ?? 0);
            }
        }

        #endregion
    }

    public class RppId : RppMember
    {
        public override ResolvableType Type { get; protected set; }

        public bool IsVar => Ref is RppVar;
        public bool IsField => Field != null;
        public bool IsParam => Ref is RppParam;
        public bool IsObject => Type.Value.IsObject;
        public IRppNamedNode Ref { get; private set; }

        public RppFieldInfo Field { get; private set; }


        public RppId([NotNull] string name) : base(name)
        {
        }

        public RppId([NotNull] string name, [NotNull] IRppNamedNode targetRef) : base(name)
        {
            Ref = targetRef;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        // TODO Replace RppId with something like RppFieldAcces, RppParamAccess and so on those Ref and Field looks very bad
        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            TypeSymbol objectType = scope.LookupObject(Name);
            // Lookup <name> or <name>$
            if (objectType != null)
            {
                Type = new ResolvableType(objectType.Type);
            }
            else
            {
                Symbol symbol = scope.Lookup(Name);
                if (symbol != null)
                {
                    if (symbol.IsLocal)
                    {
                        Type = new ResolvableType(symbol.Type);
                        Ref = ((LocalVarSymbol) symbol).Var;
                    }
                }
                else
                {
                    RppFieldInfo fieldSymbol = scope.LookupField(Name);
                    if (fieldSymbol != null)
                    {
                        Field = fieldSymbol;
                        Type = new ResolvableType(fieldSymbol.Type);
                    }
                }
            }
            return this;
        }

        public override string ToString()
        {
            return $"Id: \"{Name}\"";
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
        public ResolvableType Type { get; private set; }

        public IRppExpr Expression { get; }

        public RppBox([NotNull] IRppExpr expr)
        {
            Expression = expr;
//            Debug.Assert(expr.Type.Runtime.IsValueType);
//            Type = RppNativeType.Create(typeof (object));
            Type = new ResolvableType(RppTypeSystem.AnyTy);
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
                return (Type.GetHashCode() * 397) ^ (Expression?.GetHashCode() ?? 0);
            }
        }
    }
}