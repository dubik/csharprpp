using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public enum ClassKind
    {
        Class,
        Object
    }

    public interface IRppClass : IRppNamedNode
    {
        IEnumerable<IRppFunc> Functions { get; }

        IEnumerable<RppField> Fields { get; }

        [NotNull]
        IEnumerable<IRppFunc> Constructors { get; }

        [NotNull]
        IEnumerable<RppVariantTypeParam> TypeParams { get; }

        Type RuntimeType { get; }
        SymbolTable Scope { get; }

        RppBaseConstructorCall BaseConstructorCall { get; }

        RType Type2 { get; set; }
    }

    [DebuggerDisplay("{Kind} {Name}, Fields = {_fields.Count}, Funcs = {_funcs.Count}")]
    public class RppClass : RppNamedNode, IRppClass
    {
        private const string Constrparam = "constrparam";
        private IList<IRppFunc> _funcs;
        private IList<RppField> _fields = Collections.NoFields;
        private IList<RppField> _classParams = Collections.NoFields;
        private readonly List<IRppExpr> _constrExprs;

        public ClassKind Kind { get; }

        private SymbolTable _scope;

        [NotNull]
        public SymbolTable Scope
        {
            get
            {
                if (_scope == null)
                {
                    throw new Exception("scope is not initialized");
                }

                return _scope;
            }

            private set { _scope = value; }
        }

        [NotNull]
        public IEnumerable<IRppFunc> Functions => _funcs.AsEnumerable();

        public IRppFunc Constructor { get; }

        [NotNull]
        public IEnumerable<RppField> Fields => _fields.AsEnumerable();

        [NotNull]
        public IEnumerable<RppField> ClassParams => _classParams.AsEnumerable();

        public IEnumerable<RppVariantTypeParam> TypeParams => _typeParams.AsEnumerable();

        [NotNull]
        public Type RuntimeType { get; set; }

        private readonly IList<RppVariantTypeParam> _typeParams;

        public HashSet<ObjectModifier> Modifiers { get; private set; }

        private IList<IRppFunc> _constructors;

        public IEnumerable<IRppFunc> Constructors => _constructors.AsEnumerable();

        public RppField InstanceField { get; }
        public RType Type2 { get; set; }

        public RppClass(ClassKind kind, [NotNull] string name) : base(name)
        {
            Kind = kind;
            _funcs = Collections.NoFuncs;
            Constructor = CreatePrimaryConstructor(Collections.NoExprs);
            _typeParams = Collections.NoVariantTypeParams;
            Modifiers = Collections.NoModifiers;
            _constructors = Collections.NoFuncs;
        }

        public RppBaseConstructorCall BaseConstructorCall { get; }

        public RppClass(ClassKind kind, HashSet<ObjectModifier> modifiers, [NotNull] string name, [NotNull] IList<RppField> classParams,
            [NotNull] IEnumerable<IRppNode> classBody, [NotNull] IList<RppVariantTypeParam> typeParams, RppBaseConstructorCall baseConstructorCall) : base(name)
        {
            Kind = kind;
            BaseConstructorCall = baseConstructorCall;
            _classParams = classParams;

            IEnumerable<IRppNode> rppNodes = classBody as IList<IRppNode> ?? classBody.ToList();
            _funcs = rppNodes.OfType<IRppFunc>().Where(f => !f.IsConstructor).ToList();
            _funcs.ForEach(DefineFunc);
            _constrExprs = rppNodes.OfType<IRppExpr>().ToList();
            _typeParams = typeParams;
            Modifiers = modifiers;

            _constructors = rppNodes.OfType<IRppFunc>().Where(f => f.IsConstructor).ToList();

            _fields = _classParams.Where(param => param.MutabilityFlag != MutabilityFlag.MF_Unspecified).ToList();

            Constructor = CreatePrimaryConstructor(_constrExprs);
            _constructors.Add(Constructor);

            if (kind == ClassKind.Object)
            {
                string objectName = SymbolTable.GetObjectName(Name);
                InstanceField = new RppField(MutabilityFlag.MF_Val, "_instance", Collections.NoStrings, new ResolvableType(new RTypeName(objectName)));
                _fields.Add(InstanceField);
            }
        }

        private void DefineFunc(IRppFunc func)
        {
            func.IsStatic = Kind == ClassKind.Object;
            func.Class = this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.VisitEnter(this);
            _fields.ForEach(field => field.Accept(visitor));
            _constructors.ForEach(constructor => constructor.Accept(visitor));
            _funcs.ForEach(func => func.Accept(visitor));
            visitor.VisitExit(this);
        }

        #region Semantic

        public void PreAnalyze(SymbolTable scope)
        {
            Debug.Assert(scope != null, "scope != null");

            BaseConstructorCall.ResolveBaseClass(scope);

            Scope = new SymbolTable(scope, Type2);

            //_funcs.ForEach(Scope.Add);
            //_fields.ForEach(Scope.Add);

//            _typeParams.ForEach(Scope.Add);
        }

        public override IRppNode Analyze(SymbolTable scope)
        {
            Debug.Assert(Scope != null, "Scope != null");
            //Scope.BaseClassScope = BaseConstructorCall.BaseClass.Scope;

            SymbolTable constructorScope = new SymbolTable(Scope, Type2);
            //_classParams.ForEach(constructorScope.Add);

            _classParams = NodeUtils.Analyze(Scope, _classParams);
            _fields = NodeUtils.Analyze(Scope, _fields);

            // Add all constructors to scope, so that they can be accessed by each other
            // Constructors.ForEach(Scope.Add);

            _constructors = NodeUtils.Analyze(constructorScope, _constructors);
            _funcs = NodeUtils.Analyze(Scope, _funcs);

            return this;
        }

        [NotNull]
        private RppFunc CreatePrimaryConstructor(IEnumerable<IRppExpr> exprs)
        {
            var p = _classParams.Select(rppVar => new RppParam(MakeConstructorArgName(rppVar.Name), rppVar.Type2)).ToList();
            List<IRppNode> assignExprs = new List<IRppNode>();

            foreach (var classParam in _fields)
            {
                string argName = MakeConstructorArgName(classParam.Name);
                RppAssignOp assign = new RppAssignOp(new RppId(classParam.Name), new RppId(argName));
                assignExprs.Add(assign);
            }

            assignExprs.AddRange(exprs);
            assignExprs.Add(CreateParentConstructorCall());

            return new RppFunc("this", p, ResolvableType.UnitTy, new RppBlockExpr(assignExprs));
        }


        /// <summary>
        /// Renaming constructor parameter names so that custom constructor which
        /// refers to classParams would be resolved to field and not to param
        /// </summary>
        /// <param name="baseName">name of the argument</param>
        private string MakeConstructorArgName(string baseName)
        {
            if (_fields.Any(field => field.Name == baseName))
            {
                return Constrparam + baseName;
            }

            return baseName;
        }

        public static string StringConstructorArgName(string name)
        {
            return name.StartsWith(Constrparam) ? name.Substring(Constrparam.Length) : name;
        }

        private IRppExpr CreateParentConstructorCall()
        {
            return BaseConstructorCall;
        }

        #endregion

        #region Equality

        protected bool Equals(RppClass other)
        {
            return Kind == other.Kind && _funcs.SequenceEqual(other._funcs) && _fields.SequenceEqual(other._fields) && Name == other.Name;
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
            return Equals((RppClass) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Kind;
                return hashCode;
            }
        }

        #endregion
    }
}