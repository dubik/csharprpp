// ----------------------------------------------------------------------
// Copyright © 2014 Microsoft Mobile. All rights reserved.
// Contact: Sergiy Dubovik <sergiy.dubovik@microsoft.com>
//  
// This software, including documentation, is protected by copyright controlled by
// Microsoft Mobile. All rights are reserved. Copying, including reproducing, storing,
// adapting or translating, any or all of this material requires the prior written consent of
// Microsoft Mobile. This material also contains confidential information which may not
// be disclosed to others without the prior written consent of Microsoft Mobile.
// ----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public interface IRppFunc : IRppNode, IRppNamedNode
    {
        [NotNull]
        IRppParam[] Params { get; }

        IRppExpr Expr { get; }

        MethodInfo RuntimeType { get; set; }

        MethodBuilder Builder { get; set; }

        bool IsStatic { get; set; }
        bool IsPublic { get; set; }
        bool IsAbstract { get; set; }
        bool IsVariadic { get; set; }
        bool IsOverride { get; set; }
        bool IsConstructor { get; }

        bool IsSynthesized { get; set; }
        bool IsStub { get; set; }

        RppClass Class { get; set; }
        ConstructorInfo ConstructorInfo { get; set; }

        [NotNull]
        IList<RppVariantTypeParam> TypeParams { get; set; }
    }

    public class RppFunc : RppNamedNode, IRppFunc
    {
        public IRppExpr Expr { get; private set; }
        private SymbolTable _scope;

        public static IList<IRppParam> EmptyParams = new List<IRppParam>();

        public IRppParam[] Params { get; private set; }

        public MethodInfo RuntimeType
        {
            get { return Builder?.GetBaseDefinition(); }
            set { throw new NotImplementedException(); }
        }

        public RppMethodInfo MethodInfo { get; set; }

        public MethodBuilder Builder { get; set; }
        public ConstructorBuilder ConstructorBuilder { get; set; }

        public bool IsStatic { get; set; }

        public bool IsPublic
        {
            get { return !Modifiers.Contains(ObjectModifier.OmPrivate); }
            set { throw new NotSupportedException(); }
        }

        public bool IsAbstract
        {
            get { return Expr is RppEmptyExpr; }
            set { throw new NotSupportedException(); }
        }

        public bool IsVariadic { get; set; }

        // TODO in RppFunc this modifiers are booleans and there is a separate set of modifiers, modifiers which
        // came from parser should be separated
        public bool IsOverride
        {
            get { return Modifiers.Contains(ObjectModifier.OmOverride); }
            set { throw new NotImplementedException(); }
        }

        public bool IsConstructor => Name == "this";

        public bool IsSynthesized { get; set; }
        public bool IsStub { get; set; }

        public RppClass Class { get; set; }
        public ConstructorInfo ConstructorInfo { get; set; }
        public HashSet<ObjectModifier> Modifiers { get; set; }

        public IList<RppVariantTypeParam> TypeParams { get; set; }

        public ResolvableType ReturnType2 { get; private set; }

        public RppFunc([NotNull] string name) : base(name)
        {
            Initialize(EmptyParams, new ResolvableType(RppTypeSystem.UnitTy), RppEmptyExpr.Instance);
        }

        public RppFunc([NotNull] string name, [NotNull] ResolvableType returnType) : base(name)
        {
            Initialize(EmptyParams, returnType, RppEmptyExpr.Instance);
        }

        public RppFunc([NotNull] string name, [NotNull] IEnumerable<IRppParam> funcParams, [NotNull] ResolvableType returnType)
            : base(name)
        {
            Initialize(funcParams, returnType, RppEmptyExpr.Instance);
        }

        public RppFunc([NotNull] string name, [NotNull] IEnumerable<IRppParam> funcParams, [NotNull] ResolvableType returnType,
            [NotNull] IRppExpr expr) : base(name)
        {
            Initialize(funcParams, returnType, expr);
        }

        /// Returns <code>true</code> if signatures match
        public bool SignatureMatch(RppFunc otherFunc)
        {
            return Params.SequenceEqual(otherFunc.Params, ParamTypeComparer.Default);
        }

        private void Initialize([NotNull] IEnumerable<IRppParam> funcParams, [NotNull] ResolvableType returnType,
            [NotNull] IRppExpr expr)
        {
            Params = funcParams.ToArray();
            ReturnType2 = returnType;
            Expr = expr;
            IsVariadic = Params.Any(param => param.IsVariadic);
            TypeParams = Collections.NoVariantTypeParams;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.VisitEnter(this);
            Expr.Accept(visitor);
            visitor.VisitExit(this);
        }

        /// <summary>
        /// Resolves parameters and return types
        /// </summary>
        /// <param name="scope">class scope</param>
        public void ResolveTypes([NotNull] SymbolTable scope)
        {
            // This will make generic parameters available as well
            SymbolTable tempScope = new SymbolTable(scope, MethodInfo);
            NodeUtils.Analyze(tempScope, Params);
            ReturnType2.Resolve(tempScope);
        }

        public override IRppNode Analyze(SymbolTable scope)
        {
            _scope = new SymbolTable(scope, MethodInfo);
            Params.ForEach(p => _scope.AddLocalVar(p.Name, p.Type2.Value, p));
            // TODO this is probably not needed , because next line adds generic params to the scope
            //TypeParams.ForEach(_scope.Add);

            foreach (var typeParam in TypeParams)
            {
                //_scope.Add(typeParam.Name, RppNativeType.Create(typeParam.Runtime));
            }

            Expr = NodeUtils.AnalyzeNode(_scope, Expr);

            return this;
        }

        #region Equality

        protected bool Equals(RppFunc other)
        {
            return Equals(Name, other.Name) && Equals(ReturnType2, other.ReturnType2) && Equals(Params, other.Params) &&
                   IsStatic.Equals(other.IsStatic) &&
                   IsPublic.Equals(other.IsPublic) && IsAbstract.Equals(other.IsAbstract);
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
            return Equals((RppFunc) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ReturnType2.GetHashCode();
                hashCode = (hashCode * 397) ^ (Params?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ IsStatic.GetHashCode();
                hashCode = (hashCode * 397) ^ IsPublic.GetHashCode();
                hashCode = (hashCode * 397) ^ IsAbstract.GetHashCode();
                return hashCode;
            }
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return $"{ModifiersToString()} def {Name}({ParamsToString()}) : {ReturnType2}";
        }

        public string ModifiersToString()
        {
            IList<string> builder = new List<string>();
            if (IsStatic)
            {
                builder.Add("static");
            }

            if (IsPublic)
            {
                builder.Add("public");
            }

            if (IsAbstract)
            {
                builder.Add("abstract");
            }

            return string.Join(" ", builder);
        }

        private string ParamsToString()
        {
            return string.Join(", ", Params.Select(p => p.Name + ": " + p.Type2.ToString()));
        }

        #endregion
    }

    public class ParamTypeComparer : IEqualityComparer<IRppParam>
    {
        public static readonly ParamTypeComparer Default = new ParamTypeComparer();

        public bool Equals(IRppParam x, IRppParam y)
        {
            return x.Type2.Equals(y.Type2);
        }

        public int GetHashCode(IRppParam obj)
        {
            return obj.GetHashCode();
        }
    }

    public interface IRppParam : IRppNamedNode, IRppExpr
    {
        int Index { get; set; }
        bool IsVariadic { get; set; }

        IRppParam CloneWithNewType(RType newType);
    }

    [DebuggerDisplay("{Name}: {Type2}")]
    public sealed class RppParam : RppMember, IRppParam
    {
        public override ResolvableType Type2 { get; protected set; }

        public int Index { get; set; }

        public bool IsVariadic { get; set; }

        public RppParam(string name, ResolvableType type, bool variadic = false) : base(name)
        {
            IsVariadic = variadic;
            //Type = variadic ? new RppArrayType(type) : type;
            Type2 = type;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IRppNode Analyze(Symbols.SymbolTable scope)
        {
            Type2.Resolve(scope);
            if (IsVariadic)
            {
                Type2 = new ResolvableType(Type2.Value.MakeArrayType());
            }

            return this;
        }

        public IRppParam CloneWithNewType(RType newType)
        {
            return new RppParam(Name, new ResolvableType(newType), IsVariadic);
        }
    }
}