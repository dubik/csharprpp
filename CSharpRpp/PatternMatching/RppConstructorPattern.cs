using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr.Runtime;
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
            return MatchInstance(inVar, outOut, Assign(outOut, thenExpr), ctx);
        }

        private IRppExpr MatchInstance(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx)
        {
            // If type of input variable do not match pattern type, we need to cast it
            if (!inVar.Type.Equals(_type))
            {
                string localVar = ctx.CreateLocal(_type.Name.Name);
                var castedVariable = Val(localVar, _type.Value, new RppAsInstanceOf(inVar, _type));

                return If(BinOp("!=", new RppAsInstanceOf(inVar, _type), Null), Block(castedVariable, ProcessMatchExpr(Id(localVar), outOut, thenExpr, ctx)));
            }

            return ProcessMatchExpr(inVar, outOut, thenExpr, ctx);
        }

        private IRppExpr ProcessMatchExpr(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx)
        {
            string localOptionVar = ctx.CreateLocalOption();
            RppVar localOption = Val(localOptionVar, _unapplyMethod.ReturnType, CallMethod(_type.Value.Name, _unapplyMethod.Name, inVar));
            return Block(localOption, If(GetIsValidExpression(localOption), ProcessCases(inVar, outOut, thenExpr, ctx, localOptionVar, 0)));
        }

        private IRppExpr ProcessCases(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx, string localOptionVar, int patternIndex)
        {
            if (patternIndex >= _patterns.Length)
            {
                List<IRppNode> nodes = new List<IRppNode>();

                // Binding to a variable if it exists varid@Foo...
                if (BindedVariableToken != null)
                {
                    var varId = Val(BindedVariableToken, _type.Value, inVar);
                    nodes.Add(varId);
                }

                nodes.Add(thenExpr);
                nodes.Add(Break);
                return Block(nodes);
            }

            IRppExpr classParamValue = GetClassParam(localOptionVar, patternIndex, _patterns.Length);

            RppMatchPattern pattern = _patterns[patternIndex];
            int nextPatternIndex = patternIndex + 1;

            if (pattern is RppLiteralPattern)
            {
                RppLiteralPattern literalPattern = (RppLiteralPattern) pattern;
                return If(BinOp("==", literalPattern.Literal, classParamValue), ProcessCases(inVar, outOut, thenExpr, ctx, localOptionVar, nextPatternIndex));
            }

            RType classParamType = _classParamTypes[patternIndex];

            if (pattern is RppVariablePattern)
            {
                RType patternType = classParamType;
                RppVar var = Val(pattern.Token.Text, patternType, classParamValue);
                var.Token = pattern.Token;
                return Block(var, ProcessCases(inVar, outOut, thenExpr, ctx, localOptionVar, nextPatternIndex));
            }

            if (pattern is RppConstructorPattern)
            {
                RppConstructorPattern constructorPattern = (RppConstructorPattern) pattern;
                string classParamArg = ctx.CreateLocal(classParamType.Name);
                RppVar classParamArgVar = Val(classParamArg, classParamType, classParamValue);
                RppId classParamInput = StaticId(classParamArgVar);

                IRppExpr nextPattern = ProcessCases(inVar, outOut, thenExpr, ctx, localOptionVar, nextPatternIndex);
                return Block(classParamArgVar, constructorPattern.MatchInstance(classParamInput, outOut, nextPattern, ctx));
            }

            return ProcessCases(inVar, outOut, thenExpr, ctx, localOptionVar, nextPatternIndex);
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
                return classParams.Concat(bindedVariable).ToList();
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

        /// <summary>
        /// Extract class param types from unapply return type. E.g. unapply returns
        /// Option[Tuple[Int, String]] => [Int, String] or Option[Int] => [Int]
        /// </summary>
        /// <param name="unapplyRetType">unapply return type</param>
        /// <returns>list of types</returns>
        [NotNull]
        private static IEnumerable<RType> ExtractTypes([NotNull] RType unapplyRetType)
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
        private static RppMethodInfo FindUnapply([NotNull] RType companionType)
        {
            return companionType.Methods.FirstOrDefault(m => m.Name == "unapply");
        }

        public override string ToString()
        {
            return $"{_type}({string.Join(", ", _patterns.Select(p => p.ToString()))})";
        }
    }
}