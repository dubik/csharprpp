using System;
using System.Collections.Generic;
using Antlr.Runtime;

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

    internal enum ObjectModifier
    {
        OmNone,
        OmPrivate,
        OmProtected,
        OmOverride,
        OmFinal,
        OmSealed,
        OmImplicit,
        OmLazy
    }

    public class RppParser
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
                throw new Exception("Expected token : " + token + " but got " + _stream.LA(1));
            }
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

        private void ExpectSemi()
        {
            if (!ParseSemi())
            {
                throw new Exception("Expected ; or a new line but got: " + _lastToken.Text);
            }
        }

        public RppProgram CompilationUnit()
        {
            if (Require(RppLexer.KW_Package))
            {
                QualifiedId id = ParseQualifiedId();
                ExpectSemi();
            }

            ParseTopStatSeq();

            Expect(RppLexer.EOF);

            RppProgram program = new RppProgram();
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
            
        }

        private HashSet<ObjectModifier> ParseObjectModifier()
        {
            HashSet<ObjectModifier> modifiers = new HashSet<ObjectModifier>();
            if (Require(RppLexer.KW_Lazy))
            {
                modifiers.Add(ObjectModifier.OmLazy);
            }
            return modifiers;
        }
    }
}