using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr.Runtime;
using CSharpRpp.Expr;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using static CSharpRpp.Utils.AstHelper;

namespace CSharpRpp
{
    public class RppConstructorPattern : RppMatchPattern
    {
        public IToken BindedVariableToken { get; set; }
        private readonly ResolvableType _type;
        private RppMatchPattern[] _patterns;
        private RppMethodInfo _unapplyMethod;
        private RType[] _classParamTypes;

        public RppConstructorPattern(ResolvableType typeName, IEnumerable<RppMatchPattern> patterns)
        {
            _type = typeName;
            _patterns = patterns.ToArray();
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx)
        {
            string localOptionVar = ctx.CreateLocalOption();

            RppVar localOption = Val(localOptionVar, _unapplyMethod.ReturnType, CallMethod(_type.Value.Name, _unapplyMethod.Name, Id(inVar.Name)));

            List<IRppNode> nodes = new List<IRppNode>();
            List<IRppExpr> conds = new List<IRppExpr>();
            for (int i = 0; i < _patterns.Length; i++)
            {
                RppMatchPattern pattern = _patterns[i];
                IRppExpr classParamValue = GetClassParam(localOptionVar, i, _patterns.Length);

                if (pattern is RppLiteralPattern)
                {
                    RppLiteralPattern literalPattern = (RppLiteralPattern) pattern;
                    RppBinOp cond = BinOp("==", literalPattern.Literal, classParamValue);
                    conds.Add(cond);
                }
                else if (pattern is RppVariablePattern)
                {
                    var classParamType = _classParamTypes[i];
                    RType patternType = classParamType;

                    RppVar var = Val(pattern.Token.Text, patternType, classParamValue);
                    var.Token = pattern.Token;
                    nodes.Add(var);
                }
                else if (pattern is RppConstructorPattern)
                {
                    string classParamArg = ctx.CreateLocal();
                    RppVar classParamArgVar = Val(classParamArg, _classParamTypes[i], classParamValue);
                    nodes.Add(pattern.RewriteCaseClause(classParamArgVar, outOut, thenExpr, ctx));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            RppAssignOp assign = Assign(outOut, thenExpr);

            if (conds.Any())
            {
                IRppExpr cond = conds.Aggregate((left, right) => BinOp("&&", left, right));
                RppIf specCond = If(cond, Block(assign, Break));
                nodes.Add(specCond);
            }
            else
            {
                nodes.Add(assign);
                nodes.Add(Break);
            }

            RppIf ifCond = If(GetIsValidExpression(localOption), Block(nodes));

            List<IRppNode> wholethingy = new List<IRppNode>();

            // Binding to a variable if it exists varid@Foo...
            if (BindedVariableToken != null)
            {
                var bindedVal = Val(BindedVariableToken, _type.Value, inVar);
                wholethingy.Add(bindedVal);
            }

            wholethingy.Add(localOption);
            wholethingy.Add(ifCond);

            return Block(wholethingy);
        }

        /// <summary>
        /// For Option we call isDefined method, for bools, just return the variable content itself
        /// </summary>
        /// <param name="localOption">variable which has bool or Option</param>
        private static IRppExpr GetIsValidExpression(RppVar localOption)
        {
            string localOptionVar = localOption.Name;
            if (Equals(localOption.Type.Value, RppTypeSystem.BooleanTy))
            {
                return Id(localOptionVar);
            }

            return CallMethod(localOptionVar, "isDefined", Collections.NoExprs);
        }

        /// <summary>
        /// If there are more then one class param it is going to be in a tuple, so we need to extract it from there,
        /// other return just option's value.
        /// Options value is <code>localOptionVar.get()</code> and for tuple: <code>localOptionVar.get()._&lt;ParamIndex&gt;</code>
        /// </summary>
        /// <param name="localOptionVar">name of variable which contains results of unapply</param>
        /// <param name="classParamIndex">current index of class param</param>
        /// <param name="classParamsCount">how many class params there are</param>
        /// <returns>expression which returns value of class param at specified index</returns>
        private static IRppExpr GetClassParam(string localOptionVar, int classParamIndex, int classParamsCount)
        {
            IRppExpr optionValue = CallMethod(localOptionVar, "get");
            if (classParamsCount > 1)
            {
                return new RppSelector(optionValue, Id($"_{classParamIndex + 1}"));
            }

            return optionValue;
        }

        public override IEnumerable<IRppExpr> DeclareVariables(RType inputType)
        {
            // Check inputType with _type
            RType retType = _unapplyMethod.ReturnType;
            Debug.Assert(retType != null, "retType != null");

            // Happens when case class doesn't have class params (so unapply returns bool)
            if (Equals(retType, RppTypeSystem.BooleanTy))
            {
                return Collections.NoExprs;
            }

            _classParamTypes = ExtractTypes(retType).ToArray();
            if (_classParamTypes.Length != _patterns.Length)
            {
                throw new Exception($"Class ${inputType.Name} doesn't contain the same amount of class params as specified in case ({_classParamTypes.Count()})");
            }

            IEnumerable<IRppExpr> classParams = _patterns.Zip(_classParamTypes, Tuple.Create).SelectMany(pair => pair.Item1.DeclareVariables(pair.Item2));

            if (BindedVariableToken != null)
            {
                var bindedVariable = Val(BindedVariableToken.Text, inputType, new RppDefaultExpr(inputType.AsResolvable()));
                return classParams.Concat(bindedVariable);
            }

            return classParams;
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            _type.Resolve(scope);

            _patterns = NodeUtils.Analyze(scope, _patterns, diagnostic).ToArray();

            TypeSymbol companionObjectSymbol = scope.LookupObject(_type.Name.Name);
            if (companionObjectSymbol != null)
            {
                RppMethodInfo unapplyMethod = FindUnapply(companionObjectSymbol.Type);
                if (unapplyMethod == null)
                {
                    throw new Exception("Can't find unapply method or amount of parameters is wrong");
                }
                _unapplyMethod = unapplyMethod;
            }
            else
            {
                throw new Exception("Can't find companion object!");
            }

            return this;
        }

        [NotNull]
        private IEnumerable<RType> ExtractTypes([NotNull] RType unapplyRetType)
        {
            if (Equals(unapplyRetType, RppTypeSystem.BooleanTy))
            {
                yield return RppTypeSystem.BooleanTy;
            }
            else
            {
                RType firstType = unapplyRetType.GenericArguments.First();

                // TODO this is not very reliable, because class can be TupleSomething, and it is not std tuple, but let it be for now
                if (firstType.Name.StartsWith("Tuple"))
                {
                    foreach (RType genericArgument in firstType.GenericArguments)
                    {
                        yield return genericArgument;
                    }
                }
                else
                {
                    yield return firstType;
                }
            }
        }

        [CanBeNull]
        private RppMethodInfo FindUnapply([NotNull] RType companionType)
        {
            return companionType.Methods.FirstOrDefault(m => m.Name == "unapply");
        }

        public override string ToString()
        {
            return $"{_type}({string.Join(", ", _patterns.Select(p => p.ToString()))})";
        }
    }
}