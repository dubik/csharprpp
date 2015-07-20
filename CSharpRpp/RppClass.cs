using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    }

    [DebuggerDisplay("{Kind} {Name}, Fields = {_fields.Count}, Funcs = {_funcs.Count}")]
    public class RppClass : RppNamedNode, IRppClass
    {
        private IList<IRppFunc> _funcs;
        private IList<RppField> _fields = Collections.NoFields;
        private IList<RppField> _classParams = Collections.NoFields;
        private readonly List<IRppExpr> _constrExprs;

        public ClassKind Kind { get; private set; }

        [CanBeNull]
        public RppClassScope Scope { get; private set; }

        [NotNull]
        public IEnumerable<IRppFunc> Functions
        {
            get { return _funcs.AsEnumerable(); }
        }

        [NotNull]
        public IRppFunc Constructor { get; private set; }

        [NotNull]
        public IEnumerable<RppField> Fields
        {
            get { return _fields.AsEnumerable(); }
        }

        [NotNull]
        public IEnumerable<RppField> ClassParams
        {
            get { return _classParams.AsEnumerable(); }
        }

        [NotNull]
        public Type RuntimeType { get; set; }

        public RppClass(ClassKind kind, [NotNull] string name) : base(name)
        {
            Kind = kind;
            _funcs = Collections.NoFuncs;
            Constructor = CreateConstructor(Collections.NoExprs);
        }

        public RppBaseConstructorCall BaseConstructorCall { get; private set; }

        public RppClass(ClassKind kind, [NotNull] string name, [NotNull] IList<RppField> classParams, [NotNull] IEnumerable<IRppNode> classBody,
            RppBaseConstructorCall baseConstructorCall) : base(name)
        {
            Kind = kind;
            BaseConstructorCall = baseConstructorCall;
            _classParams = classParams;

            IEnumerable<IRppNode> rppNodes = classBody as IList<IRppNode> ?? classBody.ToList();
            _funcs = rppNodes.OfType<IRppFunc>().ToList();
            _funcs.ForEach(DefineFunc);
            _constrExprs = rppNodes.OfType<IRppExpr>().ToList();
        }

        private void DefineFunc(IRppFunc func)
        {
            func.IsStatic = Kind == ClassKind.Object;
            func.Class = this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.VisitEnter(this);
            _funcs.ForEach(func => func.Accept(visitor));
            visitor.VisitExit(this);
        }

        #region Semantic

        public override void PreAnalyze(RppScope scope)
        {
            Debug.Assert(scope != null, "scope != null");

            BaseConstructorCall.ResolveBaseClass(scope);

            Scope = new RppClassScope(scope);
            _funcs.ForEach(Scope.Add);

            _fields = _classParams.Where(param => param.MutabilityFlag != MutabilityFlag.MF_Unspecified).ToList();
            _fields.ForEach(Scope.Add);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            Debug.Assert(Scope != null, "Scope != null");
            Scope.BaseClassScope = BaseConstructorCall.BaseClass.Scope;

            RppScope constructorScope = new RppScope(Scope);
            _classParams.ForEach(constructorScope.Add);
            Constructor = CreateConstructor(_constrExprs);
            Constructor.PreAnalyze(constructorScope);

            _classParams = NodeUtils.Analyze(Scope, _classParams);
            _fields = NodeUtils.Analyze(Scope, _fields);
            Constructor.Analyze(constructorScope);

            _funcs = NodeUtils.Analyze(Scope, _funcs);

            return this;
        }

        [NotNull]
        private RppFunc CreateConstructor(IEnumerable<IRppExpr> exprs)
        {
            var p = _classParams.Select(rppVar => new RppParam(MakeConstructorArgName(rppVar.Name), rppVar.Type));
            List<IRppNode> assignExprs = new List<IRppNode>();

            foreach (var classParam in _fields)
            {
                string argName = MakeConstructorArgName(classParam.Name);
                RppAssignOp assign = new RppAssignOp(new RppId(classParam.Name), new RppId(argName));
                assignExprs.Add(assign);
            }

            assignExprs.AddRange(exprs);
            assignExprs.Add(CreateParentConstructorCall());

            return new RppFunc(Name, p, RppPrimitiveType.UnitTy, new RppBlockExpr(assignExprs));
        }


        /// <summary>
        /// renaming constructor parameter names so that custom constructor which
        /// refers to classParams would be resolved to field and not to param
        /// </summary>
        /// <param name="baseName">name of the argument</param>
        private string MakeConstructorArgName(string baseName)
        {
            if (_fields.Any(field => field.Name == baseName))
                return "constrparam" + baseName;

            return baseName;
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