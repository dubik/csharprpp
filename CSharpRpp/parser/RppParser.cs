using System;
using System.Collections.Generic;
using Antlr.Runtime;
using Mono.Collections.Generic;

namespace CSharpRpp
{
    internal class QualifiedId
    {
        private string _text;

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
        OmAbstract
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
                throw new Exception("Expected ; or a new line but got: " + _lastToken.Text);
            }
        }

        public RppProgram CompilationUnit()
        {
            RppProgram program = new RppProgram();

            if (Require(RppLexer.KW_Package))
            {
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
                throw new Exception("Traits are not implemented");
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
                string baseClassName;
                IList<IRppExpr> baseClassArgs;
                IList<IRppNode> nodes = ParseClassTemplateOpt(out baseClassName, out baseClassArgs);
                return new RppClass(ClassKind.Class, modifiers, name, classParams, nodes, new RppBaseConstructorCall(baseClassName, baseClassArgs))
                {
                    TypeParams = typeParams
                };
            }

            throw new Exception("Expected identifier but got : " + _lastToken.Text);
        }

        // ClassParamClause ::= '(' [ClassParams] ')'
        public IList<RppField> ParseClassParamClause()
        {
            var classParams = new List<RppField>();
            if (Require(RppLexer.OP_LParen))
            {
                while (!Require(RppLexer.OP_RParen))
                {
                    RppField classParam;
                    if (!ParseClassParam(out classParam))
                    {
                        throw new Exception("Class param was expected but got " + _lastToken.Text);
                    }

                    classParams.Add(classParam);
                }
            }

            return classParams;
        }

        // ClassParams ::= {Annotation} [{Modifier} (‘val’ | ‘var’)] id [‘:’ ParamType] [‘=’ Expr]
        public bool ParseClassParam(out RppField classParam)
        {
            classParam = null;

            MutabilityFlag mutability = MutabilityFlag.MF_Unspecified;
            if (Require(RppLexer.KW_Var))
            {
                mutability = MutabilityFlag.MF_Var;
            }
            else if (Require(RppLexer.KW_Val))
            {
                mutability = MutabilityFlag.MF_Val;
            }

            if (!Require(RppLexer.Id))
            {
                return false;
            }

            string name = _lastToken.Text;
            Expect(RppLexer.OP_Colon);
            RppType paramType;
            if (!ParseType(out paramType))
            {
                throw new Exception("Expected type but found: " + _lastToken.Text);
            }

            classParam = new RppField(mutability, name, null, paramType);
            return true;
        }

        // [T, N]
        private IList<RppType> ParseTypeParamClause()
        {
            IList<RppType> typeParams = new List<RppType>();
            if (Require(RppLexer.OP_LBracket))
            {
                RppType type;

                while (ParseType(out type))
                {
                    typeParams.Add(type);
                    if (Require(RppLexer.OP_RBracket))
                    {
                        break;
                    }

                    if (!Require(RppLexer.OP_Comma))
                    {
                        throw new Exception("Expected , or ] but got " + _lastToken.Text);
                    }
                }
            }

            return typeParams;
        }

        private IList<RppVariantTypeParam> ParseTypeParams()
        {
            IList<RppVariantTypeParam> typeParams = ReadOnlyCollection<RppVariantTypeParam>.Empty;
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
                        throw new Exception("Expected , or ] but got " + _lastToken.Text);
                    }
                }
            }

            return typeParams;
        }

        private bool ParseVariantTypeParam(out RppVariantTypeParam typeParam)
        {
            TypeVariant variant = TypeVariant.Contravariant; // -T
            bool requireId = false;
            if (Require(RppLexer.OP_Ops))
            {
                if (_lastToken.Text == "+")
                {
                    variant = TypeVariant.Covariant;
                }
                else if (_lastToken.Text != "-")
                {
                    throw new Exception("Expected '+' or '-' but got " + _lastToken.Text);
                }

                requireId = true;
            }

            if (Require(RppLexer.Id))
            {
                typeParam = new RppVariantTypeParam(_lastToken.Text, variant);
                return true;
            }

            if (requireId)
            {
                throw new Exception("Expected identifier but got " + _lastToken.Text);
            }

            typeParam = null;
            return false;
        }

        public IList<IRppNode> ParseClassTemplateOpt(out string baseClassName, out IList<IRppExpr> baseClassArgs)
        {
            baseClassName = null;
            baseClassArgs = ReadOnlyCollection<IRppExpr>.Empty;
            if (Require(RppLexer.KW_Extends))
            {
                if (Require(RppLexer.Id))
                {
                    baseClassName = _lastToken.Text;
                }
                else
                {
                    throw new Exception("Expected identifier but got : " + _lastToken.Text);
                }

                var args = ParseArgsOpt();
                baseClassArgs = args;
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
                return ParsePatDef(MutabilityFlag.MF_Val);
            }

            if (Require(RppLexer.KW_Var))
            {
                return ParsePatDef(MutabilityFlag.MF_Var);
            }

            if (Require(RppLexer.KW_Def))
            {
                return ParseFunDef(modifiers);
            }

            return null;
        }

        // FunSig [‘:’ Type] ‘=’ Expr
        // FunSig ::= id [FunTypeParamClause] ParamClauses

        private RppFunc ParseFunDef(HashSet<ObjectModifier> modifiers)
        {
            Expect(RppLexer.Id);
            string name = _lastToken.Text;
            IList<RppType> typeParams = ParseTypeParamClause();
            IEnumerable<IRppParam> funcParams = ParseParamClauses();
            Expect(RppLexer.OP_Colon);
            RppType funcReturnType;
            if (!ParseType(out funcReturnType))
            {
                throw new Exception("Expecting type but got " + _lastToken);
            }

            if (Require(RppLexer.OP_Eq))
            {
                SkipNewLines();

                IRppExpr expr = ParseExpr();
                return new RppFunc(name, funcParams, funcReturnType, expr) {Modifiers = modifiers};
            }

            return new RppFunc(name, funcParams, funcReturnType) {Modifiers = modifiers};
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
            RppType type;
            if (!ParseType(out type))
            {
                throw new Exception("Expected type but got " + _lastToken.Text);
            }

            bool variadic = Require(RppLexer.OP_Star);

            funcParam = new RppParam(name, type, variadic);
            return true;
        }

        // PatDef ::= Pattern2 {',' Pattern2} [':' Type] '=' Expr
        public RppVar ParsePatDef(MutabilityFlag mutabilityFlag)
        {
            Expect(RppLexer.Id);
            string varId = _lastToken.Text;

            Expect(RppLexer.OP_Colon);
            RppType type;
            if (!ParseType(out type))
            {
                throw new Exception("Expected type after ':' but got " + _lastToken.Text);
            }

            Expect(RppLexer.OP_Eq);

            IRppExpr expr = ParseExpr();
            return new RppVar(mutabilityFlag, varId, type, expr);
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

        public bool ParseType(out RppType type)
        {
            if (Require(RppLexer.Id))
            {
                string typeName = _lastToken.Text;
                if (Require(RppLexer.OP_LBracket))
                {
                    RppGenericType genericType = new RppGenericType(typeName);
                    type = genericType;

                    RppType subType;
                    if (!ParseType(out subType))
                    {
                        throw new Exception("Expected type but got " + _lastToken.Text);
                    }

                    genericType.AddParam(subType);

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
                                throw new Exception("Expected type but got " + _lastToken.Text);
                            }

                            genericType.AddParam(subType);
                        }
                        else
                        {
                            throw new Exception("Expected comma but got " + _lastToken.Text);
                        }
                    }

                    return true;
                }

                type = new RppTypeName(typeName);
                return true;
            }

            type = null;
            return false;
        }

        private RppClass ParseObjectDef(HashSet<ObjectModifier> modifiers)
        {
            Expect(RppLexer.Id);
            string objectName = _lastToken.Text;

            string baseClassName;
            IList<IRppExpr> baseClassArgs;
            IList<IRppNode> stats = ParseClassTemplateOpt(out baseClassName, out baseClassArgs);
            return new RppClass(ClassKind.Object, modifiers, objectName, Collections.NoFields, stats,
                new RppBaseConstructorCall(baseClassName, Collections.NoExprs));
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