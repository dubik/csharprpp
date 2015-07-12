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
        private IList<RppField> _fields;

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
        public Type RuntimeType { get; set; }

        private readonly string _baseClassName;

        public RppClass BaseClass { get; private set; }

        public RppClass(ClassKind kind, [NotNull] string name) : base(name)
        {
            Kind = kind;
            _fields = Collections.NoFields;
            _funcs = Collections.NoFuncs;
        }

        public RppClass(ClassKind kind, [NotNull] string name, [NotNull] IList<RppField> fields, [NotNull] IEnumerable<IRppNode> classBody,
            string baseClass = null) : base(name)
        {
            Kind = kind;
            _baseClassName = baseClass;
            _fields = fields;

            _funcs = classBody.OfType<IRppFunc>().ToList();
            _funcs.ForEach(DefineFunc);
            var exprs = classBody.OfType<IRppExpr>().ToList();
            Constructor = CreateConstructor(exprs);
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
            if (_baseClassName != null)
            {
                BaseClass = (RppClass) scope.Lookup(_baseClassName);
                Scope = new RppClassScope(BaseClass.Scope, scope);
            }
            else
            {
                Scope = new RppClassScope(null, scope);
            }

            _funcs.ForEach(Scope.Add);

            NodeUtils.PreAnalyze(Scope, _fields);
            NodeUtils.PreAnalyze(Scope, _funcs);

            Constructor.PreAnalyze(Scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _fields = NodeUtils.Analyze(Scope, _fields);
            Constructor.Analyze(Scope);
            _funcs = NodeUtils.Analyze(Scope, _funcs);

            return this;
        }

        private RppFunc CreateConstructor(IEnumerable<IRppExpr> exprs)
        {
            var p = _fields.Select(rppVar => new RppParam(MakeConstructorArgName(rppVar.Name), rppVar.Type));
            List<IRppNode> assignExprs = new List<IRppNode> {CreateParentConstructorCall()};

            foreach (var classParam in _fields)
            {
                string argName = MakeConstructorArgName(classParam.Name);
                RppAssignOp assign = new RppAssignOp(new RppId(classParam.Name, classParam), new RppId(argName));
                assignExprs.Add(assign);
            }

            assignExprs.AddRange(exprs);

            return new RppFunc(Name, p, RppPrimitiveType.UnitTy, new RppBlockExpr(assignExprs));
        }


        /// <summary>
        /// renaming constructor parameter names so that custom constructor which
        /// refers to fields would be resolved to field and not to param
        /// </summary>
        /// <param name="baseName">name of the argument</param>
        private string MakeConstructorArgName(string baseName)
        {
            return "constrparam" + baseName;
        }

        private static IRppExpr CreateParentConstructorCall()
        {
            return new RppFuncCall("ctor()", Collections.NoExprs);
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