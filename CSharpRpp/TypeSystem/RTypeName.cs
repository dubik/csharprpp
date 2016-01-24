using System;
using System.Collections.Generic;
using System.Linq;
using Antlr.Runtime;
using CSharpRpp.Exceptions;
using CSharpRpp.Symbols;
using JetBrains.Annotations;

namespace CSharpRpp.TypeSystem
{
    public class RTypeName
    {
        public static RTypeName Undefined = new RTypeName("Undefined");
        public static RTypeName UnitN = new RTypeName("Unit");
        public static RTypeName IntN = new RTypeName("Int");

        [NotNull]
        public string Name { get; }

        [NotNull]
        public IToken Token
        {
            get { return _token ?? new ClassicToken(0, Name); }
            set { _token = value; }
        }

        private readonly IList<RTypeName> _params = new List<RTypeName>();
        private IToken _token;

        public RTypeName([NotNull] string name)
        {
            Name = name;
        }

        public RTypeName([NotNull] IToken token) : this(token.Text)
        {
            Token = token;
        }

        public void AddGenericArgument(RTypeName genericArgument)
        {
            _params.Add(genericArgument);
        }

        public RType Resolve([NotNull] SymbolTable scope)
        {
            TypeSymbol typeSymbol = scope.LookupType(Name);

            if (typeSymbol == null)
            {
                throw SemanticExceptionFactory.TypeNotFound(Token);
            }

            RType type = typeSymbol.Type;

            if (_params.Any())
            {
                if (!type.IsGenericType)
                {
                    throw new Exception($"Non generic type '{type}' has generic arguments");
                }

                RType[] genericArguments = _params.Select(p => p.Resolve(scope)).ToArray();
                return type.MakeGenericType(genericArguments);
            }

            return type;
        }

        public override string ToString()
        {
            if (_params.Any())
            {
                var paramsString = string.Join(", ", _params.Select(p => p.ToString()));
                return $"{Name}[{paramsString}]";
            }

            return Name;
        }

        #region Equality

        protected bool Equals(RTypeName other)
        {
            // TODO need to Equals also generic params
            return string.Equals(Name, other.Name);
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
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((RTypeName) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_params?.GetHashCode() ?? 0) * 397) ^ (Name?.GetHashCode() ?? 0);
            }
        }

        #endregion
    }
}