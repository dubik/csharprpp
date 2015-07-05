﻿using System;
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
            Constructor = CreateConstructor();
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
            }

            _scope = new RppScope(scope);

            _funcs.ForEach(_scope.Add);

            NodeUtils.PreAnalyze(_scope, _fields);
            NodeUtils.PreAnalyze(_scope, _funcs);


            Constructor.PreAnalyze(_scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            _fields = NodeUtils.Analyze(_scope, _fields);
            //Constructor.Analyze(_scope);
            _funcs = NodeUtils.Analyze(_scope, _funcs);

            return this;
        }

        private RppFunc CreateConstructor()
        {
            var p = _fields.Select(rppVar => new RppParam(rppVar.Name, rppVar.Type));
            List<IRppNode> assignExprs = new List<IRppNode> {CreateParentConstructorCall()};

            foreach (var classParam in _fields)
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