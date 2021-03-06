﻿using System;
using System.Collections.Generic;
using System.Linq;
using Antlr.Runtime;
using CSharpRpp.Exceptions;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    internal class QualifiedId
    {
        [UsedImplicitly] private string _text;

        public QualifiedId(string text)
        {
            _text = text;
        }
    }

    public enum ObjectModifier
    {
        OmNone,
        OmPrivate,
        OmProtected,
        OmOverride,
        OmFinal,
        OmSealed,
        OmImplicit,
        OmLazy,
        OmAbstract,
        OmCase
    }

    internal class UnexpectedTokenException : Exception
    {
        public IToken Actual { get; set; }
        public string Expected { get; set; }

        public UnexpectedTokenException(string message, IToken actual, string expected) : base(message)
        {
            Actual = actual;
            Expected = expected;
        }
    }

    internal class SyntaxException : Exception
    {
        public IToken BadToken { get; private set; }

        public SyntaxException(string message, IToken badToken) : base(message)
        {
            BadToken = badToken;
        }
    }

    public partial class RppParser
    {
        private readonly ITokenStream _stream;

        private IToken _lastToken;

        public RppParser(ITokenStream stream)
        {
            _stream = stream;
        }

        [ContractAnnotation("=> halt")]
        private void RaiseSyntaxError(string message)
        {
            throw SemanticExceptionFactory.SyntaxError(_lastToken, message);
        }

        private bool Require(int token)
        {
            IToken nextToken = _stream.LT(1);
            if (nextToken.Type == token)
            {
                _lastToken = nextToken;
                _stream.Consume();
                return true;
            }

            return false;
        }

        private void Expect(int token)
        {
            if (!Require(token))
            {
                IToken actual = _stream.LT(1);
                string expected = RppLexer.TokenToString(token);
                throw new UnexpectedTokenException("Unexpected token", actual, expected);
            }
        }

        private int Peek()
        {
            return _stream.LA(1);
        }

        private IToken PeekToken()
        {
            return _stream.LT(1);
        }

        private void Consume()
        {
            _lastToken = _stream.LT(1);
            _stream.Consume();
        }

        private bool Peek(int token)
        {
            return _stream.LA(1) == token;
        }

        private bool ParseSemi()
        {
            if (Require(RppLexer.OP_Semi) || Require(RppLexer.EOF))
            {
                return true;
            }

            bool ok = false;
            while (Require(RppLexer.NewLine))
            {
                ok = true;
            }

            return ok;
        }

        private void SkipNewLines()
        {
            while (Require(RppLexer.NewLine))
            {
            }
        }

        private void ExpectSemi()
        {
            if (!ParseSemi())
            {
                RaiseSyntaxError("Expected ; or a new line but got: ");
            }
        }

        public RppProgram CompilationUnit(RppProgram program)
        {
            if (Require(RppLexer.KW_Package))
            {
                // ReSharper disable once UnusedVariable
                QualifiedId id = ParseQualifiedId();
                ExpectSemi();
            }

            SkipNewLines();

            ParseTopStatSeq(program);

            SkipNewLines();

            Expect(RppLexer.EOF);

            return program;
        }

        private QualifiedId ParseQualifiedId()
        {
            Expect(RppLexer.Id);

            return new QualifiedId(_lastToken.Text);
        }

        private void ParseTopStatSeq(RppProgram program)
        {
            while (ParseTopStat(program))
            {
                ParseSemi();
            }
        }

        private bool ParseTopStat(RppProgram program)
        {
            var modifiers = ParseObjectModifier();
            return ParseTmplDef(modifiers, program);
        }

        private bool ParseTmplDef(HashSet<ObjectModifier> modifiers, RppProgram program)
        {
            if (Require(RppLexer.KW_Trait))
            {
                RaiseSyntaxError("Traits are not implemented");
            }

            if (Require(RppLexer.KW_Class))
            {
                RppClass obj = ParseClassDef(modifiers);
                program.Add(obj);
                return true;
            }

            if (Require(RppLexer.KW_Object))
            {
                RppClass obj = ParseObjectDef(modifiers);
                program.Add(obj);
                return true;
            }

            return false;
        }

        // ClassDef ::= id [TypeParamClause] {Annotation} [AccessModifier] ClassParamClauses ClassTemplateOpt
        public RppClass ParseClassDef(HashSet<ObjectModifier> modifiers)
        {
            if (Require(RppLexer.Id))
            {
                string name = _lastToken.Text;
                IList<RppVariantTypeParam> typeParams = ParseTypeParams();
                IList<RppField> classParams = ParseClassParamClause();
                IList<IRppExpr> baseClassArgs;
                RTypeName baseClassTypeName;
                IList<IRppNode> nodes = ParseClassTemplateOpt(out baseClassTypeName, out baseClassArgs);

                ResolvableType baseClass = ResolvableType.AnyTy;
                if (baseClassTypeName != null)
                {
                    baseClass = new ResolvableType(baseClassTypeName);
                }

                return new RppClass(ClassKind.Class, modifiers, name, classParams, nodes, typeParams,
                    new RppBaseConstructorCall(baseClass, baseClassArgs));
            }

            RaiseSyntaxError("Expected identifier but got : ");
            return null;
        }

        // ClassParamClause ::= '(' [ClassParams] ')'
        public IList<RppField> ParseClassParamClause()
        {
            var classParams = new List<RppField>();
            if (Require(RppLexer.OP_LParen))
            {
                if (!Require(RppLexer.OP_RParen))
                {
                    while (true)
                    {
                        RppField classParam;
                        if (!ParseClassParam(out classParam))
                        {
                            RaiseSyntaxError("DeclaringType param was expected");
                        }

                        classParams.Add(classParam);

                        if (!Peek(RppLexer.OP_Comma))
                        {
                            Expect(RppLexer.OP_RParen);
                            break;
                        }

                        Consume(); // Comma
                    }
                }
            }

            return classParams;
        }

        // ClassParams ::= {Annotation} [{Modifier} (‘val’ | ‘var’)] id [‘:’ ParamType] [‘=’ Expr]
        public bool ParseClassParam(out RppField classParam)
        {
            classParam = null;

            MutabilityFlag mutability = MutabilityFlag.MfUnspecified;
            if (Require(RppLexer.KW_Var))
            {
                mutability = MutabilityFlag.MfVar;
            }
            else if (Require(RppLexer.KW_Val))
            {
                mutability = MutabilityFlag.MfVal;
            }

            if (!Require(RppLexer.Id))
            {
                return false;
            }

            string name = _lastToken.Text;
            IToken nameToken = _lastToken;
            Expect(RppLexer.OP_Colon);
            RTypeName paramType;
            if (!ParseType(out paramType))
            {
                RaiseSyntaxError("Type is expected");
            }

            classParam = new RppField(mutability, name, Collections.NoModifiers, new ResolvableType(paramType)) {Token = nameToken, IsClassParam = true};
            return true;
        }

        // [T, N]
        [NotNull]
        private IList<RTypeName> ParseTypeParamClause()
        {
            IList<RTypeName> typeParams = new List<RTypeName>();
            if (Require(RppLexer.OP_LBracket))
            {
                RTypeName type;

                while (ParseType(out type))
                {
                    typeParams.Add(type);
                    if (Require(RppLexer.OP_RBracket))
                    {
                        break;
                    }

                    if (!Require(RppLexer.OP_Comma))
                    {
                        RaiseSyntaxError("Expected , or ]");
                    }
                }
            }

            return typeParams;
        }

        private IList<RppVariantTypeParam> ParseTypeParams()
        {
            IList<RppVariantTypeParam> typeParams = Collections.NoVariantTypeParams;
            if (Require(RppLexer.OP_LBracket))
            {
                typeParams = new List<RppVariantTypeParam>();
                RppVariantTypeParam typeParam;
                while (ParseVariantTypeParam(out typeParam))
                {
                    typeParams.Add(typeParam);
                    if (Require(RppLexer.OP_RBracket))
                    {
                        break;
                    }

                    if (!Require(RppLexer.OP_Comma))
                    {
                        RaiseSyntaxError("Expected , or ]");
                    }
                }
            }

            return typeParams;
        }

        private bool ParseVariantTypeParam(out RppVariantTypeParam typeParam)
        {
            TypeVariant variant = TypeVariant.Invariant; // "A"
            bool requireId = false;
            if (Require(RppLexer.OP_Unary))
            {
                if (_lastToken.Text == "+") // "+A"
                {
                    variant = TypeVariant.Covariant;
                }
                else if (_lastToken.Text == "-") // "-A"
                {
                    variant = TypeVariant.Contravariant;
                }
                else
                {
                    RaiseSyntaxError("Expected '+' or '-'");
                }

                requireId = true;
            }

            if (Require(RppLexer.Id))
            {
                string typeParamName = _lastToken.Text;
                RTypeName constraint = null;
                if (Require(RppLexer.OP_Upper))
                {
                    Expect(RppLexer.Id);
                    constraint = new RTypeName(_lastToken);
                }

                typeParam = new RppVariantTypeParam(typeParamName, variant, constraint);
                return true;
            }

            if (requireId)
            {
                RaiseSyntaxError("Expected identifier");
            }

            typeParam = null;
            return false;
        }

        public IList<IRppNode> ParseClassTemplateOpt(out RTypeName baseClassType, out IList<IRppExpr> constrArgs)
        {
            baseClassType = null;
            constrArgs = Collections.NoExprs;
            if (Require(RppLexer.KW_Extends))
            {
                if (Require(RppLexer.Id))
                {
                    baseClassType = new RTypeName(_lastToken);
                }
                else
                {
                    RaiseSyntaxError("Expected identifier");
                }

                IList<RTypeName> typeArgs = ParseTypeParamClause();
                typeArgs.ForEach(baseClassType.AddGenericArgument);
                var args = ParseArgsOpt();
                constrArgs = args;
            }

            return ParseTemplateBody();
        }

        public IList<IRppNode> ParseTemplateBody()
        {
            Require(RppLexer.NewLine);

            List<IRppNode> stats = new List<IRppNode>();
            if (Require(RppLexer.OP_LBrace))
            {
                IRppNode stat;
                while (ParseSemi() && (stat = ParseTemplateStat()) != null)
                {
                    stats.Add(stat);
                }

                Expect(RppLexer.OP_RBrace);
            }

            return stats;
        }

        public IRppNode ParseTemplateStat()
        {
            SkipNewLines();
            HashSet<ObjectModifier> modifiers = ParseObjectModifier();

            IRppNode stat = ParseDef(modifiers);
            if (stat != null)
            {
                return stat;
            }

            stat = ParseDcl();
            if (stat != null)
            {
                return stat;
            }

            stat = ParseExpr1();
            return stat;
        }

        private static IRppNode ParseDcl()
        {
            return null;
        }

        private IRppNode ParseDef(HashSet<ObjectModifier> modifiers)
        {
            if (Require(RppLexer.KW_Val))
            {
                return ParsePatDef(MutabilityFlag.MfVal, modifiers);
            }

            if (Require(RppLexer.KW_Var))
            {
                return ParsePatDef(MutabilityFlag.MfVar, modifiers);
            }

            if (Require(RppLexer.KW_Def))
            {
                return ParseFunDef(modifiers);
            }

            if (Require(RppLexer.KW_Class) || Require(RppLexer.KW_Object))
            {
                return ParseClassDef(modifiers);
            }

            return null;
        }

        // FunSig [‘:’ Type] ‘=’ Expr
        // FunSig ::= id [FunTypeParamClause] ParamClauses
        private bool _isInsideFunction;

        private RppFunc ParseFunDef(HashSet<ObjectModifier> modifiers)
        {
            try
            {
                _isInsideFunction = true;

                Expect(RppLexer.Id);
                string name = _lastToken.Text;
                IList<RppVariantTypeParam> typeParams = Collections.NoVariantTypeParams;
                if (name != "this")
                {
                    typeParams = ParseTypeParams();
                }
                IEnumerable<IRppParam> funcParams = ParseParamClauses();
                RTypeName funcReturnType = RTypeName.UnitN;
                if (name != "this")
                {
                    Expect(RppLexer.OP_Colon);
                    if (!ParseType(out funcReturnType))
                    {
                        RaiseSyntaxError("Type is expected");
                    }
                }

                if (Require(RppLexer.OP_Eq))
                {
                    SkipNewLines();

                    IRppExpr expr = ParseExpr();
                    return new RppFunc(name, funcParams, new ResolvableType(funcReturnType), expr) {Modifiers = modifiers, TypeParams = typeParams};
                }

                return new RppFunc(name, funcParams, new ResolvableType(funcReturnType)) {Modifiers = modifiers, TypeParams = typeParams};
            }
            finally
            {
                _isInsideFunction = false;
            }
        }


        public IEnumerable<IRppParam> ParseParamClauses()
        {
            if (Require(RppLexer.OP_LParen))
            {
                var res = ParseParams();
                Expect(RppLexer.OP_RParen);
                return res;
            }

            return RppFunc.EmptyParams;
        }

        // param {, param}
        public IEnumerable<IRppParam> ParseParams()
        {
            IList<IRppParam> funcParams = new List<IRppParam>();
            while (true)
            {
                RppParam funcParam;
                if (ParseParam(out funcParam))
                {
                    funcParams.Add(funcParam);
                }
                if (!Require(RppLexer.OP_Comma))
                {
                    break;
                }
            }

            return funcParams;
        }

        public bool ParseBindings([CanBeNull] out IEnumerable<IRppParam> bindings)
        {
            bindings = null;

            var list = new List<IRppParam>();

            if (!Require(RppLexer.OP_LParen))
            {
                RppParam funcParam;
                if (ParseBinding(out funcParam))
                {
                    list.Add(funcParam);
                }
                else
                {
                    return false;
                }

                bindings = list;
                return true;
            }

            while (true)
            {
                RppParam funcParam;
                if (ParseBinding(out funcParam))
                {
                    list.Add(funcParam);
                }
                else
                {
                    break;
                }

                if (!Require(RppLexer.OP_Comma))
                {
                    break;
                }
            }

            if (!Require(RppLexer.OP_RParen))
            {
                return false;
            }

            bindings = list;
            return true;
        }

        private bool ParseBinding(out RppParam binding)
        {
            binding = null;
            if (!Require(RppLexer.Id))
            {
                return false;
            }

            string name = _lastToken.Text;

            ResolvableType type = ResolvableType.UndefinedTy;
            if (Require(RppLexer.OP_Colon))
            {
                RTypeName typeName;
                if (!ParseType(out typeName))
                {
                    RaiseSyntaxError("Type is expected");
                }

                type = new ResolvableType(typeName);
            }

            binding = new RppParam(name, type) {IsClosureBinding = true};
            return true;
        }

        //param ::= {Annotation} id [‘:’ ParamType] [‘=’ Expr]
        private bool ParseParam(out RppParam funcParam)
        {
            funcParam = null;
            if (!Require(RppLexer.Id))
            {
                return false;
            }
            string name = _lastToken.Text;
            Expect(RppLexer.OP_Colon);
            RTypeName type;
            if (!ParseType(out type))
            {
                RaiseSyntaxError("Type is expected");
            }

            bool variadic = Require(RppLexer.OP_Star);

            var a = new RppParam(name, new ResolvableType(type), variadic);
            funcParam = a;
            return true;
        }

        // PatDef ::= Pattern2 {',' Pattern2} [':' Type] ['=' Expr]
        public RppVar ParsePatDef(MutabilityFlag mutabilityFlag, HashSet<ObjectModifier> modifiers)
        {
            Expect(RppLexer.Id);
            IToken varIdToken = _lastToken;

            RTypeName type = RTypeName.Undefined;
            if (Require(RppLexer.OP_Colon))
            {
                if (!ParseType(out type))
                {
                    RaiseSyntaxError("Type is expected after ':'");
                }
            }

            IRppExpr expr = RppEmptyExpr.Instance;
            if (Require(RppLexer.OP_Eq))
            {
                expr = ParseExpr();
            }

            if (_isInsideFunction)
            {
                return new RppVar(mutabilityFlag, varIdToken.Text, new ResolvableType(type), expr) {Token = varIdToken};
            }
            else
            {
                return new RppField(mutabilityFlag, varIdToken.Text, modifiers, new ResolvableType(type), expr) {Token = varIdToken};
            }
        }

        public bool ParseModifier()
        {
            return ParseLocalModifier() && ParseAccessModifier();
        }

        private bool ParseAccessModifier()
        {
            return false;
        }

        private bool ParseLocalModifier()
        {
            return false;
        }

        // Consume follow is needed to handle pattern matching case:
        // case x: Foo => x.length
        // without consumeFollow false it will parse and closure type (Foo => x) which is incorrect
        public bool ParseType(out RTypeName type, bool consumeFollow = true)
        {
            if (Require(RppLexer.Id))
            {
                IToken typeNameToken = _lastToken;
                if (Require(RppLexer.OP_LBracket))
                {
                    RTypeName genericType = new RTypeName(typeNameToken);
                    type = genericType;

                    RTypeName subType;
                    if (!ParseType(out subType))
                    {
                        RaiseSyntaxError("Type is expected");
                    }

                    genericType.AddGenericArgument(subType);

                    while (true)
                    {
                        if (Require(RppLexer.OP_RBracket))
                        {
                            break;
                        }

                        if (Require(RppLexer.OP_Comma))
                        {
                            if (!ParseType(out subType))
                            {
                                RaiseSyntaxError("Type is expected");
                            }

                            genericType.AddGenericArgument(subType);
                        }
                        else
                        {
                            RaiseSyntaxError("Expected comma");
                        }
                    }

                    return true;
                }

                // A => B
                if (consumeFollow && Require(RppLexer.OP_Follow))
                {
                    RTypeName returnType;
                    if (!ParseType(out returnType))
                    {
                        RaiseSyntaxError("Type is expected");
                    }

                    type = CreateClosureTypeName(new List<RTypeName> {new RTypeName(typeNameToken)}, returnType);
                    return true;
                }

                type = new RTypeName(typeNameToken);
                return true;
            }

            // (A, B) => C
            // (A => B)
            if (Require(RppLexer.OP_LParen))
            {
                bool closingParenRequired = true;
                RTypeName returnType;
                IList<RTypeName> paramTypes = new List<RTypeName>();
                while (true)
                {
                    if (Require(RppLexer.OP_RParen))
                    {
                        closingParenRequired = false;
                        break;
                    }

                    if (Peek(RppLexer.OP_Follow))
                    {
                        break;
                    }

                    if (paramTypes.Count > 0)
                    {
                        Expect(RppLexer.OP_Comma);
                    }

                    RTypeName paramType;
                    if (ParseType(out paramType))
                    {
                        paramTypes.Add(paramType);
                    }
                    else
                    {
                        RaiseSyntaxError("Type is expected");
                    }
                }

                if (!Require(RppLexer.OP_Follow))
                {
                    // (A => A)
                    if (paramTypes.Count == 1)
                    {
                        type = paramTypes[0];
                        return true;
                    }

                    // (A, B, C)
                    type = CreateTupleTypeName(paramTypes);
                    return true;
                }

                if (!ParseType(out returnType))
                {
                    RaiseSyntaxError("Type is expected");
                }

                if (closingParenRequired)
                {
                    Expect(RppLexer.OP_RParen);
                }

                type = CreateClosureTypeName(paramTypes, returnType);
                return true;
            }

            type = null;
            return false;
        }

        /// <summary>
        /// Creates Function[paramCount] type name if return type is not Unit.
        /// If it's Unit then it creates Action[paramCount].
        /// </summary>
        private static RTypeName CreateClosureTypeName(ICollection<RTypeName> paramTypes, RTypeName returnType)
        {
            bool isAction = IsAction(returnType);
            string baseTypeName = isAction ? "Action" : "Function";
            RTypeName closureType = new RTypeName(baseTypeName + paramTypes.Count);
            paramTypes.ForEach(closureType.AddGenericArgument);
            if (!isAction)
            {
                closureType.AddGenericArgument(returnType);
            }
            return closureType;
        }

        private static bool IsAction(RTypeName returnType)
        {
            return returnType.Name.Equals("Unit");
        }

        private static RTypeName CreateTupleTypeName(ICollection<RTypeName> paramTypes)
        {
            RTypeName tupleType = new RTypeName(CreateTupleClassName(paramTypes.Count));
            paramTypes.ForEach(tupleType.AddGenericArgument);
            return tupleType;
        }

        private static string CreateTupleClassName(int paramsCount)
        {
            return "Tuple" + paramsCount;
        }

        private RppClass ParseObjectDef(HashSet<ObjectModifier> modifiers)
        {
            Expect(RppLexer.Id);
            string objectName = _lastToken.Text;

            RTypeName baseClassTypeName;
            IList<IRppExpr> baseClassArgs;
            IList<IRppNode> stats = ParseClassTemplateOpt(out baseClassTypeName, out baseClassArgs);

            ResolvableType baseClass = ResolvableType.AnyTy;
            if (baseClassTypeName != null)
            {
                baseClass = new ResolvableType(baseClassTypeName);
            }

            return new RppClass(ClassKind.Object, modifiers, objectName, Collections.NoFields, stats, Collections.NoVariantTypeParams,
                new RppBaseConstructorCall(baseClass, Collections.NoExprs));
        }

        private static readonly Dictionary<int, ObjectModifier> TokenToObjectModifierMap = new Dictionary<int, ObjectModifier>
        {
            {RppLexer.KW_Lazy, ObjectModifier.OmLazy},
            {RppLexer.KW_Abstract, ObjectModifier.OmAbstract},
            {RppLexer.KW_Final, ObjectModifier.OmFinal},
            {RppLexer.KW_Implicit, ObjectModifier.OmImplicit},
            {RppLexer.KW_Override, ObjectModifier.OmOverride},
            {RppLexer.KW_Private, ObjectModifier.OmPrivate},
            {RppLexer.KW_Protected, ObjectModifier.OmProtected},
            {RppLexer.KW_Sealed, ObjectModifier.OmSealed},
            {RppLexer.KW_Case, ObjectModifier.OmCase}
        };

        private HashSet<ObjectModifier> ParseObjectModifier()
        {
            HashSet<ObjectModifier> res = new HashSet<ObjectModifier>();

            int nextToken = Peek();
            ObjectModifier modifier;
            while (TokenToObjectModifierMap.TryGetValue(nextToken, out modifier))
            {
                if (res.Contains(modifier))
                {
                    throw new SyntaxException("repeated modifier", PeekToken());
                }

                res.Add(modifier);
                Consume();
                nextToken = Peek();
            }

            return res;
        }
    }
}