using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CSharpRpp.Native;
using CSharpRpp.Parser;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using static CSharpRpp.TypeSystem.ResolvableType;

namespace CSharpRpp
{
    // Base class for RppId and RppFuncCall
    public abstract class RppMember : RppNamedNode, IRppExpr
    {
        public abstract RppType Type { get; protected set; }
        public abstract ResolvableType Type2 { get; protected set; }

        // TODO perhaps it would make more sense to add target to constructor as a parameter (not type, but node)
        public RppObjectType TargetType { get; set; }
        public RType TargetType2 { get; set; }

        protected RppMember(string name) : base(name)
        {
        }
    }

    public class RppEmptyExpr : RppNode, IRppExpr
    {
        public static RppEmptyExpr Instance = new RppEmptyExpr();

        public RppType Type => RppPrimitiveType.UnitTy;

        public ResolvableType Type2 => UnitTy;
    }

    public class RppNull : RppNode, IRppExpr
    {
        public RppType Type => RppNullType.Instance;

        public ResolvableType Type2 => NullTy;

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
            Type = RppNativeType.Create(typeof (bool));
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            base.Analyze(scope);

            // 10 || left
            // isOk && notRunning

            EnsureType(Left.Type, Types.Bool);
            EnsureType(Right.Type, Types.Bool);

            return this;
        }

        private void EnsureType(RppType type, Type expectedType)
        {
            if (type.Runtime != expectedType)
            {
                throw new Exception(String.Format("Expected {0} type but got {1}", expectedType, type.Runtime));
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
            Type = RppNativeType.Create(Types.Bool);
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            base.Analyze(scope);

            return this;
        }
    }

    public class RppArithmBinOp : BinOp
    {
        internal static readonly HashSet<string> Ops = new HashSet<string> {"+", "-", "/", "*", "%"};

        public RppArithmBinOp([NotNull] string op, [NotNull] IRppExpr left, [NotNull] IRppExpr right) : base(op, left, right)
        {
        }

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            base.Analyze(scope);

            Type2 = new ResolvableType(TypeInference.ResolveCommonType(Left.Type2.Value, Right.Type2.Value));

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
        public ResolvableType Type2 { get; protected set; }

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

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            Left = Left.Analyze(scope) as IRppExpr;
            Debug.Assert(Left != null);
            Right = Right.Analyze(scope) as IRppExpr;
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
        public ResolvableType Type2 { get; private set; }

        protected RppLiteralBase(string valueStr)
        {
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            Initialize(Parse(valueStr));
        }

        private void Initialize(T value)
        {
            Value = value;
            Type2 = new ResolvableType(RppTypeSystem.ImportPrimitive(typeof (T)));
        }

        protected RppLiteralBase(T value)
        {
            Initialize(value);
        }

        protected abstract T Parse(string valueStr);

        #region Equality

        protected bool Equals(RppLiteralBase<T> other)
        {
            return Value.Equals(other.Value) && Type2.Equals(other.Type2);
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
                return (Value.GetHashCode() * 397) ^ Type2.GetHashCode();
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
        public override sealed RppType Type { get; protected set; }
        public override sealed ResolvableType Type2 { get; protected set; }

        public IEnumerable<IRppExpr> Args => ArgList.AsEnumerable();

        protected readonly IList<IRppExpr> ArgList;

        [NotNull]
        public RppMethodInfo Function { get; }

        public bool IsConstructorCall => Name == "this";

        private readonly IList<RppVariantTypeParam> _typeArgs;

        public IEnumerable<RppVariantTypeParam> TypeArgs => _typeArgs;

        public RppFuncCall([NotNull] string name, [NotNull] IList<IRppExpr> argList) : this(name, argList, Collections.NoVariantTypeParams)
        {
        }

        public RppFuncCall([NotNull] string name, [NotNull] IList<IRppExpr> argList, [NotNull] IList<RppVariantTypeParam> typeArgList) : base(name)
        {
            ArgList = argList;
            _typeArgs = typeArgList;
        }

        public RppFuncCall([NotNull] string name, [NotNull] IList<IRppExpr> argList, RppMethodInfo function, ResolvableType type,
            [NotNull] IList<RppVariantTypeParam> typeArgList)
            : this(name, argList, typeArgList)
        {
            Function = function;
            Type2 = type;
        }

        private static IList<IRppExpr> ReplaceUndefinedClosureTypesIfNeeded(IEnumerable<IRppExpr> exprs, RppParameterInfo[] funcParams)
        {
            IEnumerable<IRppExpr> rppExprs = exprs as IList<IRppExpr> ?? exprs.ToList();
            var funcParamTypes = ExpandVariadicParam(funcParams, rppExprs.Count());
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
        private static IEnumerable<RppType> ExpandVariadicParam(RppParameterInfo[] funcParams, int totalNumberOfParams)
        {
            /*
            List<RppType> expandedList = funcParams.Where(p => !p.IsVariadic).Select(p => p.Type).ToList();
            if (funcParams.Count > 0 && funcParams.Last().IsVariadic)
            {
                // Variadic param is an array, so extract sub type.
                RppType variadicParamType = ((RppArrayType) funcParams.Last().Type).SubType;
                Enumerable.Range(0, totalNumberOfParams - expandedList.Count).ForEach(i => expandedList.Add(variadicParamType));
            }

            return expandedList;
            */
            throw new NotImplementedException("Not done yet");
        }

        // TODO This needs to be rewritten.
        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            // Skip closures because they may have missing types
            NodeUtils.Analyze(scope, ArgListWithoutClosures(ArgList));

            _typeArgs.ForEach(arg => arg.Resolve(scope));

            // Search for a function which matches signature and possible gaps in types (for closures)
            FunctionResolution.ResolveResults resolveResults = FunctionResolution.ResolveFunction(Name, ArgList, TypeArgs, scope);
            //IList<IRppExpr> args = ReplaceUndefinedClosureTypesIfNeeded(ArgList, resolveResults.Function.Parameters);
            var args = ArgList;
            NodeUtils.Analyze(scope, ArgListOfClosures(args));
            if (resolveResults.Function.IsVariadic)
            {
                args = RewriteArgListForVariadicParameter(scope, args, resolveResults.Function);
            }

            return resolveResults.RewriteFunctionCall(TargetType, Name, args, _typeArgs);
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
        private static List<IRppExpr> RewriteArgListForVariadicParameter(Symbols.SymbolTable scope, IList<IRppExpr> args, RppMethodInfo function)
        {
            List<RppParameterInfo> funcParams = function.Parameters.ToList();
            int variadicIndex = funcParams.FindIndex(p => p.IsVariadic);
            List<IRppExpr> newArgList = args.Take(variadicIndex).ToList();
            var variadicParams = args.Where((arg, index) => index >= variadicIndex).ToList();

            RppParameterInfo variadicParam = funcParams.Find(p => p.IsVariadic);

            RType elementType = GetElementType(variadicParam.Type);
            variadicParams = variadicParams.Select(param => BoxIfValueType(param, elementType)).ToList();

            RppArray variadicArgsArray = new RppArray(elementType, variadicParams);
            variadicArgsArray = (RppArray) variadicArgsArray.Analyze(scope);

            newArgList.Add(variadicArgsArray);
            return newArgList;
        }

        private static RType GetElementType(RType arrayType)
        {
            return arrayType.SubType();
            /*
            RppArrayType type = arrayType as RppArrayType;
            if (type != null)
            {
                return type.SubType;
            }

            if (arrayType is RppNativeType)
            {
                Type nativeArrayType = arrayType.Runtime;
                return RppNativeType.Create(nativeArrayType.GetElementType());
            }

            Debug.Assert(false, "arrayType is not arraytype ");
            return null;
            */
        }

        private static IRppExpr BoxIfValueType(IRppExpr arg, RType targetType)
        {
            if ((Equals(arg.Type2.Value, RppTypeSystem.IntTy) || Equals(arg.Type2.Value, RppTypeSystem.FloatTy)) && targetType == RppTypeSystem.AnyTy)
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
        public string BaseClassName { get; }

        public IRppClass BaseClass { get; private set; }

        public RppMethodInfo BaseConstructor { get; private set; }

        public IEnumerable<RppType> BaseClassTypeArgs { get; }
        public IEnumerable<RTypeName> BaseClassTypeArgs2 { get; private set; }
        public ResolvedType BaseClassType { get; private set; }

        public RType BaseClassType2 { get; private set; }

        public static RppBaseConstructorCall Object = new RppBaseConstructorCall("Object", Collections.NoExprs, Collections.NoTypes);

        public RppBaseConstructorCall([CanBeNull] string baseClassName, [NotNull] IList<IRppExpr> argList, IEnumerable<RppType> baseClassTypeArgs)
            : base("ctor()", argList)
        {
            BaseClassName = baseClassName ?? "Object";
            BaseClassTypeArgs = baseClassTypeArgs;
        }

        public RppBaseConstructorCall([CanBeNull] string baseClassName, [NotNull] IList<IRppExpr> argList, IEnumerable<RTypeName> baseClassTypeArgs)
            : base("ctor()", argList)
        {
            BaseClassName = baseClassName ?? "Object";
            BaseClassTypeArgs2 = baseClassTypeArgs;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public void ResolveBaseClass(SymbolTable scope)
        {
            switch (BaseClassName)
            {
                case "Object":
                    BaseClassType2 = new RType("Object", typeof (object));
                    BaseClass = new RppNativeClass(typeof (object));
                    break;
                case "Exception":
                    BaseClassType2 = new RType("Exception", typeof (Exception));
                    BaseClass = new RppNativeClass(typeof (Exception));
                    break;
                default:
                    BaseClassType2 = scope.LookupType(BaseClassName).Type;
                    if (BaseClassType2 == null)
                    {
                        throw new Exception($"Can't find {BaseClassName} class");
                    }
                    break;
            }
        }

        public override IRppNode Analyze(SymbolTable scope)
        {
            NodeUtils.Analyze(scope, ArgList);

            /*
            BaseClassTypeArgs = BaseClassTypeArgs.Select(type => type.Resolve(scope)).ToList();

            var types = BaseClassTypeArgs.Select(type => type.Runtime).ToArray();

            IEnumerable<RppType> args = Args.Select(a => a.Type).ToList();
            if (BaseClass.RuntimeType.IsGenericType)
            {
                BaseClassType = new RppGenericObjectType(BaseClass, types, BaseClass.RuntimeType.MakeGenericType(types));
            }
            else
            {
                BaseClassType = new RppObjectType(BaseClass);
            }

            BaseConstructor = FindMatchingConstructor(args);

            // parent constructor is a special case, so don't resolve function
            Type = RppNativeType.Create(typeof (void));
            */
            SymbolTable sc = new SymbolTable(null, BaseClassType2, BaseClass?.Scope);
            BaseConstructor = FindMatchingConstructor(ArgList, sc);
            Type2 = UnitTy;
            return this;
        }

        private RppMethodInfo FindMatchingConstructor(IEnumerable<IRppExpr> args, Symbols.SymbolTable scope)
        {
            IReadOnlyCollection<RppMethodInfo> overloads = scope.LookupFunction("this");
            IEnumerable<Type> typeArgs = Collections.NoRuntimeTypes;
            var candidates = OverloadQuery.Find(args, typeArgs, overloads, new DefaultTypesComparator(scope)).ToList();
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
    }

    public class RppArray : RppNode, IRppExpr
    {
        public RppType Type { get; private set; }
        public ResolvableType Type2 { get; private set; }

        public IEnumerable<IRppExpr> Initializers { get; private set; }

        public int Size { get; private set; }

        private readonly RType _elementType;

        public RppArray(RType type, IEnumerable<IRppExpr> initializers)
        {
            _elementType = type;
            Initializers = initializers;
            Size = Initializers.Count();
        }

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            Type2 = new ResolvableType(_elementType.MakeArrayType());

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
        public RppType Type { get; private set; }
        public ResolvableType Type2 { get; private set; }

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

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            _exprs = NodeUtils.Analyze(scope, _exprs);

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
                    Type2 = lastExpr.Type2;
                }
            }
            else
            {
                Type2 = new ResolvableType(RppTypeSystem.UnitTy);
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
        public ResolvableType Type2 { get; private set; }
        public Type RuntimeType { get; private set; }

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

        public override IRppNode Analyze(SymbolTable scope)
        {
            Target.Analyze(scope);
            RType targetType = Target.Type2.Value;

            Debug.Assert(targetType != null, "targetType != null");

            SymbolTable classScope = new SymbolTable(null, targetType, null);

            // targetType.Class.Functions.ForEach(classScope.Add);
            // targetType.Class.Fields.ForEach(classScope.Add);

            /*
            if (Target.Type is RppGenericObjectType)
            {
                RppGenericObjectType targetGenericType = (RppGenericObjectType) Target.Type;
                foreach (var pair in targetGenericType.Class.TypeParams.Zip(targetGenericType.GenericArguments, Tuple.Create))
                {
                    classScope.Add(pair.Item1.Name, RppNativeType.Create(pair.Item2));
                }
            }
            */
            //Path.TargetType = targetType;
            Path.TargetType2 = targetType;
            Path = (RppMember) Path.Analyze(classScope);
            //Type = Path.Type.Runtime.IsGenericParameter ? classScope.LookupGenericType(Path.Type.Runtime.Name) : Path.Type;
            Type2 = Path.Type2;
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

    internal sealed class ClassAsMemberAdapter : RppMember
    {
        public override RppType Type { get; protected set; }
        public override ResolvableType Type2 { get; protected set; }

        public ClassAsMemberAdapter(RppClass clazz) : base(clazz.Name)
        {
            Type = new RppObjectType(clazz);
        }
    }

    public class RppId : RppMember
    {
        public override RppType Type { get; protected set; }
        public override ResolvableType Type2 { get; protected set; }

        public bool IsVar => Ref is RppVar;
        public bool IsField => Field != null;
        public bool IsParam => Ref is RppParam;
        public IRppNamedNode Ref { get; private set; }

        public RppFieldInfo Field { get; private set; }

        public RppId([NotNull] string name) : base(name)
        {
        }

        public RppId([NotNull] string name, [NotNull] RppMember targetRef) : base(name)
        {
            throw new NotImplementedException();
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        // TODO Replace RppId with something like RppFieldAcces, RppParamAccess and so on those Ref and Field looks very bad
        public override IRppNode Analyze(SymbolTable scope)
        {
            // Lookup <name> or <name>$
            Symbol symbol = scope.Lookup(Name);
            if (symbol != null)
            {
                if (symbol.IsLocal)
                {
                    Type2 = new ResolvableType(symbol.Type);
                    Ref = ((LocalVarSymbol) symbol).Var;
                }
            }
            else
            {
                RppFieldInfo fieldSymbol = scope.LookupField(Name);
                if (fieldSymbol != null)
                {
                    Field = fieldSymbol;
                    Type2 = new ResolvableType(fieldSymbol.Type);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }


            // Reference to object, e.g. String.isNull(..)
            /*
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
                */

            // If generic like 'A', then find real type
            /*
            if (Ref.Type2.IsGenericParameter())
            {
                Type = scope.LookupGenericType(Ref.Type.Runtime.Name);
                return this;
            }*/

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
        public RppType Type { get; }
        public ResolvableType Type2 { get; private set; }

        public IRppExpr Expression { get; }

        public RppBox([NotNull] IRppExpr expr)
        {
            Expression = expr;
//            Debug.Assert(expr.Type.Runtime.IsValueType);
//            Type = RppNativeType.Create(typeof (object));
            Type2 = new ResolvableType(RppTypeSystem.AnyTy);
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        protected bool Equals(RppBox other)
        {
            return Type2.Equals(other.Type2) && Equals(Expression, other.Expression);
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