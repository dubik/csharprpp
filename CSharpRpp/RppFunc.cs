﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Antlr3.ST.Language;

namespace CSharpRpp
{
    public interface IRppFunc : IRppNode, IRppNamedNode
    {
        MethodInfo RuntimeFuncInfo { get; }

        RppType ReturnType { get; }
        Type RuntimeReturnType { get; }
        IRppParam[] Params { get; }

        bool IsStatic { get; set; }
        bool IsPublic { get; set; }
        bool IsAbstract { get; set; }

        void CodegenMethodStubs(TypeBuilder typeBuilder);
        void Codegen(CodegenContext ctx);
    }

    public class RppFunc : RppNamedNode, IRppFunc
    {
        private IRppExpr _expr;
        private RppScope _scope;

        public static IList<IRppParam> EmptyParams = new List<IRppParam>();

        public RppType ReturnType { get; private set; }
        public Type RuntimeReturnType { get; private set; }
        public IRppParam[] Params { get; private set; }

        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public bool IsAbstract { get; set; }

        #region Codegen

        private MethodBuilder _methodBuilder;

        #endregion

        public RppFunc(string name, IEnumerable<IRppParam> funcParams, RppType returnType, IRppExpr expr) : base(name)
        {
            Params = funcParams.ToArray();
            ReturnType = returnType;
            _expr = expr ?? new RppEmptyExpr();
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
            _expr.Accept(visitor);
        }

        public override void PreAnalyze(RppScope scope)
        {
            _scope = new RppScope(scope);

            Params.ForEach(scope.Add);
            _expr.PreAnalyze(_scope);
        }

        public override IRppNode Analyze(RppScope scope)
        {
            Params = NodeUtils.Analyze(_scope, Params).ToArray();
            _expr = NodeUtils.AnalyzeNode(_scope, _expr);

            RuntimeReturnType = ReturnType.Resolve(_scope);

            return this;
        }

        protected bool Equals(RppFunc other)
        {
            return Equals(Name, other.Name) && Equals(ReturnType, other.ReturnType) && Equals(Params, other.Params) && IsStatic.Equals(other.IsStatic) &&
                   IsPublic.Equals(other.IsPublic) && IsAbstract.Equals(other.IsAbstract);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RppFunc) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (ReturnType != null ? ReturnType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Params != null ? Params.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsStatic.GetHashCode();
                hashCode = (hashCode * 397) ^ IsPublic.GetHashCode();
                hashCode = (hashCode * 397) ^ IsAbstract.GetHashCode();
                return hashCode;
            }
        }

        #region ToString

        public override string ToString()
        {
            return string.Format("{0} def {1}({2}) : {3}", ModifiersToString(), Name, ParamsToString(), ReturnType);
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
            return string.Join(", ", Params.Select(p => p.Name + ": " + p.Type.ToString()));
        }

        #endregion

        #region Codegen

        public MethodInfo RuntimeFuncInfo
        {
            get { return _methodBuilder.GetBaseDefinition(); }
        }

        public void CodegenMethodStubs(TypeBuilder typeBuilder)
        {
            _methodBuilder = typeBuilder.DefineMethod(Name, MethodAttributes.Public | MethodAttributes.Static);
        }

        public void Codegen(CodegenContext ctx)
        {
            _methodBuilder.SetReturnType(RuntimeReturnType);
            CodegenParams(Params, _methodBuilder);

            ILGenerator generator = _methodBuilder.GetILGenerator();
            _expr.Codegen(generator);

            if (RuntimeReturnType == typeof (void) && _expr.RuntimeType != typeof (void))
            {
                generator.Emit(OpCodes.Pop);
            }

            generator.Emit(OpCodes.Ret);
        }

        private static void CodegenParams(IEnumerable<IRppParam> paramList, MethodBuilder methodBuilder)
        {
            Type[] parameterTypes = paramList.Select(param => param.RuntimeType).ToArray();
            methodBuilder.SetParameters(parameterTypes);
            // paramList.ForEachWithIndex((index, param) => methodBuilder.DefineParameter(index, ParameterAttributes.In, param.Name));
        }

        #endregion

        public MethodInfo NativeMethodInfo()
        {
            return _methodBuilder.GetBaseDefinition();
        }
    }

    public interface IRppParam : IRppNamedNode, IRppExpr
    {
    }

    [DebuggerDisplay("{Type.ToString()} {Name} [{RuntimeType}]")]
    public class RppParam : RppNamedNode, IRppParam
    {
        public RppType Type { get; private set; }
        public Type RuntimeType { get; private set; }

        private readonly int _index;

        public RppParam(string name, int index, RppType type) : base(name)
        {
            Type = type;
            _index = index;
        }

        public override IRppNode Analyze(RppScope scope)
        {
            RuntimeType = Type.Resolve(scope);
            return this;
        }

        public void Codegen(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldarg, _index);
        }
    }
}