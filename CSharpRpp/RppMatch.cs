using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr.Runtime;
using CSharpRpp.Exceptions;
using CSharpRpp.Expr;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using static CSharpRpp.ListExtensions;
using static CSharpRpp.Utils.AstHelper;

namespace CSharpRpp
{
    public class RppMatchingContext
    {
        private int _localVarCounter;
        private int _localOptionsCounter;

        public string CreateLocal()
        {
            return $"localVar{_localVarCounter++}";
        }

        public string CreateLocalOption()
        {
            return $"localOption{_localOptionsCounter++}";
        }
    }

    public class RppMatch : RppNode, IRppExpr
    {
        public ResolvableType Type { get; set; }

        public IRppExpr Value;
        public IEnumerable<RppCaseClause> CaseClauses;

        public RppMatch(IRppExpr value, [NotNull] IEnumerable<RppCaseClause> caseClauses)
        {
            Value = value;
            CaseClauses = caseClauses;
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            Value = (IRppExpr) Value.Analyze(scope, diagnostic);
            RppVar declIn = new RppVar(MutabilityFlag.MfVal, "<in>", Value.Type, Value);
            declIn.Analyze(scope, diagnostic);

            CaseClauses = NodeUtils.Analyze(scope, CaseClauses, diagnostic);
            CheckCommonType(CaseClauses, Token);
            Type = CaseClauses.First().Type;
            RppVar declOut = new RppVar(MutabilityFlag.MfVar, "<out>", Type, new RppDefaultExpr(Type));

            RppId declInId = new RppId("<in>", declIn);
            RppId declOutId = new RppId("<out>", declOut);

            RppMatchingContext ctx = new RppMatchingContext();
            var ifC = Create(declInId, declOutId, CaseClauses, ctx);
            var expr = new RppBlockExpr(List<IRppNode>(declIn, ifC)) {Exitable = true};

            SymbolTable matchScope = new SymbolTable(scope);
            RppBlockExpr matchBlock = new RppBlockExpr(List<IRppNode>(declOut, expr, declOutId));
            return matchBlock.Analyze(matchScope, diagnostic);
        }

        private static void CheckCommonType(IEnumerable<RppCaseClause> caseClauses, IToken token)
        {
            var returnTypes = caseClauses.Select(c => c.Type.Value).Distinct().Count();
            if (returnTypes > 1)
            {
                throw SemanticExceptionFactory.PatternMatchingCaseClausesHaveDifferentExpressionTypes(token);
            }
        }

        private static IRppExpr Create(RppMember declInId, RppMember declOutId, IEnumerable<RppCaseClause> caseClauses, RppMatchingContext ctx)
        {
            return Block(caseClauses.Select(c => c.RewriteCaseClause(declInId, declOutId, ctx)).ToList<IRppNode>());
        }
    }

    public class RppCaseClause : RppNode, IRppExpr
    {
        public ResolvableType Type => Expr.Type;

        public RppMatchPattern Pattern { get; set; }

        public IRppExpr Expr { get; set; }

        public RppCaseClause([NotNull] RppMatchPattern pattern, [NotNull] IRppExpr expr)
        {
            Pattern = pattern;
            Expr = expr;
        }

        /// <summary>
        /// So we have pattern like this:
        /// case [Pattern] => [Expr]
        /// we need to figure out type of [Expr] but it can depend on variables spawned in
        /// [Pattern], so we need to get thise variables (see RppMatchPattern.DeclareVariables())
        /// and add them to the scope and then anaylize [Expr]
        /// </summary>
        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            Pattern = (RppMatchPattern) Pattern.Analyze(scope, diagnostic);

            SymbolTable localScope = new SymbolTable(scope);
            RType inputType = GetInputType(localScope);
            IEnumerable<IRppExpr> locals = Pattern.DeclareVariables(inputType);
            NodeUtils.Analyze(localScope, locals, diagnostic);

            Expr = (IRppExpr) Expr.Analyze(localScope, diagnostic);
            return this;
        }

        public override string ToString()
        {
            return Pattern + " => " + Expr;
        }

        public IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, RppMatchingContext ctx)
        {
            return Pattern.RewriteCaseClause(inVar, outOut, Expr, ctx);
        }

        [NotNull]
        protected static RType GetInputType([NotNull] SymbolTable scope)
        {
            LocalVarSymbol entry = (LocalVarSymbol) scope.Lookup("<in>");
            Debug.Assert(entry != null, "input variable must exists");
            return entry.Type;
        }
    }

    public abstract class RppMatchPattern : RppNode
    {
        /// <summary>
        /// Creates RppVar(s) with new variables (if any), needed for semantic analysis
        /// </summary>
        /// <returns></returns>
        [NotNull]
        public virtual IEnumerable<IRppExpr> DeclareVariables([NotNull] RType inputType)
        {
            return Collections.NoExprs;
        }

        public abstract IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx);
    }

    public class RppLiteralPattern : RppMatchPattern
    {
        public IRppExpr Literal { get; set; }

        public RppLiteralPattern([NotNull] IRppExpr literal)
        {
            Literal = literal;
        }

        public override string ToString()
        {
            return Literal.ToString();
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx)
        {
            return If(BinOp("==", inVar, Literal), Block(Assign(outOut, thenExpr), Break));
        }
    }

    public class RppVariablePattern : RppMatchPattern
    {
        public string Name { get; set; }

        public RppVariablePattern()
        {
            Name = "_";
        }

        public RppVariablePattern(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override IEnumerable<IRppExpr> DeclareVariables(RType inputType)
        {
            if (Name == "_")
            {
                return Collections.NoExprs;
            }

            RppVar variable = Val(Name, inputType, new RppDefaultExpr(inputType.AsResolvable()));
            variable.Token = Token;
            return List(variable);
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx)
        {
            if (Name == "_")
            {
                return Block(Assign(outOut, thenExpr), Break);
            }

            throw new NotImplementedException();
        }
    }

    public class RppConstructorPattern : RppMatchPattern
    {
        private readonly ResolvableType _type;
        private readonly RppMatchPattern[] _patterns;
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
                else
                {
                    throw new NotImplementedException("not done yet");
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
            return Block(localOption, ifCond);
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

            return _patterns.Zip(_classParamTypes, Tuple.Create).SelectMany(pair => pair.Item1.DeclareVariables(pair.Item2));
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            _type.Resolve(scope);

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

    public class RppTypedPattern : RppMatchPattern
    {
        public string Name { get; }
        private readonly ResolvableType _resolvableType;

        public RppTypedPattern(IToken varid, RTypeName typeName)
        {
            Token = varid;
            Name = varid.Text;
            _resolvableType = new ResolvableType(typeName);
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            _resolvableType.Resolve(scope);
            return this;
        }

        public override IEnumerable<IRppExpr> DeclareVariables(RType inputType)
        {
            RppVar variable = new RppVar(MutabilityFlag.MfVal, Name, _resolvableType, new RppDefaultExpr(_resolvableType)) {Token = Token};
            return List(variable);
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx)
        {
            RppVar variable = new RppVar(MutabilityFlag.MfVal, Name, _resolvableType, new RppAsInstanceOf(inVar, _resolvableType)) {Token = Token};
            RppIf ifCond = If(BinOp("!=", Id(Name), Null), Block(Assign(outOut, thenExpr), Break), EmptyExpr);
            return Block(variable, ifCond);
        }

        public override string ToString()
        {
            return $"{Name}:{_resolvableType}";
        }
    }

    public class RppBinderPattern : RppMatchPattern
    {
        public RppBinderPattern(IToken varid, RppMatchPattern pattern)
        {
        }

        public override IRppExpr RewriteCaseClause(RppMember inVar, RppMember outOut, IRppExpr thenExpr, RppMatchingContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}