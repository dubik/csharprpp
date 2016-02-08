using System.Collections.Generic;
using System.Linq;
using Antlr.Runtime;
using CSharpRpp.Expr;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;

namespace CSharpRpp.Utils
{
    internal static class AstHelper
    {
        public static RppParam Param(string name, string type)
        {
            return new RppParam(name, ResolvableType(type));
        }

        public static RppParam Param(string name, RTypeName typeName)
        {
            return new RppParam(name, new ResolvableType(typeName));
        }

        public static RppBinOp NotNull(IRppExpr expr)
        {
            return BinOp("!=", expr, Null);
        }

        public static RppId Id(string name)
        {
            return new RppId(name);
        }

        /// <summary>
        /// Creates reference to variable and resolves to it's type
        /// </summary>
        public static RppId StaticId(RppVar rppVar)
        {
            RppId classParamInput = Id(rppVar.Name);
            SymbolTable symbolTable = new SymbolTable();
            symbolTable.AddLocalVar(rppVar.Name, rppVar.Type.Value, rppVar);
            classParamInput.Analyze(symbolTable, new Diagnostic());
            return classParamInput;
        }

        public static RppNew New(string typeNameString, IEnumerable<IRppExpr> args)
        {
            return New(ResolvableType(typeNameString), args);
        }

        public static RppNew New(ResolvableType type, IEnumerable<IRppExpr> args)
        {
            return new RppNew(type, args);
        }

        public static RppBinOp BinOp(string op, IRppExpr left, IRppExpr right)
        {
            return RppBinOp.Create(op, left, right);
        }

        public static RTypeName TypeName(string name)
        {
            return new RTypeName(name);
        }

        public static ResolvableType ResolvableType(string name)
        {
            return new ResolvableType(TypeName(name));
        }

        public static ResolvableType ResolvableType(RType type)
        {
            return new ResolvableType(type);
        }

        public static RppSelector FieldSelect(string target, string fieldName)
        {
            return Selector(Id(target), Id(fieldName));
        }

        public static RppSelector CallMethod(string target, string methodName, IList<IRppExpr> args)
        {
            return new RppSelector(Id(target), Call(methodName, args));
        }

        public static RppSelector CallMethod(string target, string methodName, params IRppExpr[] args)
        {
            return new RppSelector(Id(target), Call(methodName, args));
        }

        public static RppSelector Selector(IRppExpr target, RppMember path)
        {
            return new RppSelector(target, path);
        }

        public static RppIf If(IRppExpr condition, IRppExpr thenExpr)
        {
            return If(condition, thenExpr, RppEmptyExpr.Instance);
        }

        public static RppIf If(IRppExpr condition, IRppExpr thenExpr, IRppExpr elseExpr)
        {
            return new RppIf(condition, thenExpr, elseExpr);
        }

        public static RppAssignOp Assign(IRppExpr left, IRppExpr right)
        {
            return new RppAssignOp(left, right);
        }

        public static RppFuncCall Call(string name, IList<IRppExpr> args)
        {
            return new RppFuncCall(name, args);
        }

        public static RppFuncCall Call(string name, IList<IRppExpr> args, IList<ResolvableType> typeList)
        {
            return new RppFuncCall(name, args, typeList);
        }

        public static RppBlockExpr Block(params IRppNode[] exprs)
        {
            return new RppBlockExpr(exprs.ToList());
        }

        public static RppBlockExpr Block(IList<IRppNode> exprs)
        {
            return new RppBlockExpr(exprs);
        }

        public static RppVar Val(string name, RType type, IRppExpr initExpr)
        {
            return new RppVar(MutabilityFlag.MfVal, name, type.AsResolvable(), initExpr);
        }

        public static RppVar Val(IToken nameToken, RType type, IRppExpr initExpr)
        {
            return new RppVar(MutabilityFlag.MfVal, nameToken.Text, type.AsResolvable(), initExpr) {Token = nameToken};
        }

        public static RppNull Null = RppNull.Instance;
        public static RppBreak Break = RppBreak.Instance;
        public static RppEmptyExpr EmptyExpr = RppEmptyExpr.Instance;
    }
}