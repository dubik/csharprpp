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

    [DebuggerDisplay("{Kind} {Name}, Fields = {_fields.Count}, Funcs = {_funcs.Count}")]
    public class RppClass : RppNamedNode
    {
        private const string Constrparam = "constrparam";
        private IList<RppFunc> _funcs;
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
        public IEnumerable<RppField> Fields => _fields.AsEnumerable();

        [NotNull]
        public IEnumerable<RppField> ClassParams => _classParams.AsEnumerable();

        public IEnumerable<RppVariantTypeParam> TypeParams => _typeParams.AsEnumerable();

        private readonly IList<RppVariantTypeParam> _typeParams;

        public HashSet<ObjectModifier> Modifiers { get; private set; }

        private IList<RppFunc> _constructors;
        private IList<RppClass> _nested;

        public IEnumerable<RppFunc> Constructors => _constructors.AsEnumerable();

        public RppField InstanceField { get; }
        public RType Type { get; set; }

        public RppBaseConstructorCall BaseConstructorCall { get; }

        public RppClass(ClassKind kind, HashSet<ObjectModifier> modifiers, [NotNull] string name, [NotNull] IList<RppField> classParams,
            [NotNull] IEnumerable<IRppNode> classBody, [NotNull] IList<RppVariantTypeParam> typeParams, RppBaseConstructorCall baseConstructorCall) : base(name)
        {
            Kind = kind;
            BaseConstructorCall = baseConstructorCall;
            _classParams = classParams;

            IEnumerable<IRppNode> rppNodes = classBody as IList<IRppNode> ?? classBody.ToList();
            _funcs = rppNodes.OfType<RppFunc>().Where(f => !f.IsConstructor).ToList();
            _funcs.ForEach(DefineFunc);
            _constrExprs = rppNodes.OfType<IRppExpr>().ToList();
            _typeParams = typeParams;
            Modifiers = modifiers;

            _constructors = rppNodes.OfType<RppFunc>().Where(f => f.IsConstructor).ToList();

            _fields = _classParams.Where(param => param.MutabilityFlag != MutabilityFlag.MF_Unspecified).ToList();

            var primaryConstructor = CreatePrimaryConstructor(_constrExprs);
            _constructors.Add(primaryConstructor);

            if (kind == ClassKind.Object)
            {
                string objectName = SymbolTable.GetObjectName(Name);
                InstanceField = new RppField(MutabilityFlag.MF_Val, "_instance", Collections.NoStrings, new ResolvableType(new RTypeName(objectName)));
                _fields.Add(InstanceField);
            }

            _nested = rppNodes.OfType<RppClass>().ToList();
        }

        private void DefineFunc(RppFunc func)
        {
            func.IsStatic = Kind == ClassKind.Object;
            func.Class = this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.VisitEnter(this);
            _fields.ForEach(f => f.Accept(visitor));
            _constructors.ForEach(c => c.Accept(visitor));
            _funcs.ForEach(f => f.Accept(visitor));
            _nested.ForEach(n => n.Accept(visitor));
            visitor.VisitExit(this);
        }

        #region Semantic

        public void PreAnalyze(SymbolTable scope)
        {
            Debug.Assert(scope != null, "scope != null");

            NodeUtils.PreAnalyze(scope, _nested);

            Scope = new SymbolTable(scope, Type);
            BaseConstructorCall.ResolveBaseClass(Scope);
        }

        public override IRppNode Analyze(SymbolTable scope)
        {
            Debug.Assert(Scope != null, "Scope != null");

            NodeUtils.Analyze(scope, _nested);

            SymbolTable constructorScope = new SymbolTable(Scope, Type);

            _classParams = NodeUtils.Analyze(Scope, _classParams);
            _fields = NodeUtils.Analyze(Scope, _fields);

            _constructors = NodeUtils.Analyze(constructorScope, _constructors);
            _funcs = NodeUtils.Analyze(Scope, _funcs);

            return this;
        }

        [NotNull]
        private RppFunc CreatePrimaryConstructor(IEnumerable<IRppExpr> exprs)
        {
            var p = _classParams.Select(rppVar => new RppParam(MakeConstructorArgName(rppVar.Name), rppVar.Type)).ToList();
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
    }
}