using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
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

    [DebuggerDisplay("{Kind} {Name}, Fields = {_classParams.Count}, Funcs = {_funcs.Count}")]
    public class RppClass : RppNamedNode, IRppClass
    {
        private IList<RppVar> _classParams;
        private IList<IRppFunc> _funcs;
        private RppScope _scope;

        public ClassKind Kind { get; private set; }

        [NotNull]
        public IEnumerable<IRppFunc> Functions
        {
            get { return _funcs.AsEnumerable(); }
        }

        [NotNull]
        public IRppFunc Constructor { get; private set; }

        [NotNull]
        public Type RuntimeType { get; set; }

        public RppClass(ClassKind kind, [NotNull] string name) : base(name)
        {
            Kind = kind;
            _classParams = Collections.NoFields;
            _funcs = Collections.NoFuncs;
        }

        public RppClass(ClassKind kind, [NotNull] string name, [NotNull] IList<RppVar> classParams, [NotNull] IEnumerable<IRppNode> classBody) : base(name)
        {
            Kind = kind;

            _classParams = classParams;

            _funcs = classBody.OfType<IRppFunc>().ToList();
            _funcs.ForEach(func => func.IsStatic = kind == ClassKind.Object);
            Constructor = CreateConstructor();
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
            _scope = new RppScope(scope);

            _classParams.ForEach(_scope.Add);
            _funcs.ForEach(_scope.Add);

            NodeUtils.PreAnalyze(_scope, _classParams);
            NodeUtils.PreAnalyze(_scope, _funcs);


            Constructor.PreAnalyze(_scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _classParams = NodeUtils.Analyze(_scope, _classParams);
            //Constructor.Analyze(_scope);
            _funcs = NodeUtils.Analyze(_scope, _funcs);

            return this;
        }

        private RppFunc CreateConstructor()
        {
            int index = 1;
            var p = _classParams.Select(rppVar => new RppParam(rppVar.Name, index++, rppVar.Type));
            List<IRppNode> assignExprs = new List<IRppNode> {CreateParentConstructorCall()};

            foreach (var classParam in _classParams)
            {
                RppAssignOp assign = new RppAssignOp(new RppId(classParam.Name, classParam), new RppId(classParam.Name));
                assignExprs.Add(assign);
            }

            return new RppFunc(Name, p, RppPrimitiveType.UnitTy, new RppBlockExpr(assignExprs));
        }

        private static IRppExpr CreateParentConstructorCall()
        {
            return new RppFuncCall("ctor()", Collections.NoExprs);
        }

        #endregion

        #region Equality

        protected bool Equals(RppClass other)
        {
            return Kind == other.Kind && _funcs.SequenceEqual(other._funcs) && _classParams.SequenceEqual(other._classParams) && Name == other.Name;
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