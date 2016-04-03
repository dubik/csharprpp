using System;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Exceptions;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppFieldSelector : RppMember
    {
        public override ResolvableType Type { get; protected set; }

        [CanBeNull]
        public RppFieldInfo Field { get; private set; }

        public RppFieldSelector([NotNull] string name) : base(name)
        {
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            if (TargetType == null)
            {
                throw new Exception("TargetType should be specified before anaylyze is called");
            }

            RType classType = TargetType;
            // TODO It's kinda weird to have resolution here and not in the scope, because similar
            // lookup is done for methods
            while (classType != null && Field == null)
            {
                Field = classType.Fields.FirstOrDefault(f => f.Name == Name);
                if (Field != null)
                {
                    break;
                }

                classType = classType.BaseType;
            }


            if (Field == null)
            {
                var functions = scope.LookupFunction(Name);
                if (functions.Any(f => f.Parameters.IsEmpty()))
                {
                    RppFuncCall funcCall = new RppFuncCall(Name, Collections.NoExprs);
                    return funcCall.Analyze(scope, diagnostic);
                }

                throw SemanticExceptionFactory.ValueIsNotMember(Token, TargetType.ToString());
            }

            Debug.Assert(classType != null, "obj != null");

            Type = new ResolvableType(Field.Type);

            return this;
        }

        /// <summary>
        /// Searches for specialized class in the hierarchy of <code>TargetType</code>.
        /// It goes through <code>BaseConstructorCall</code> because it has the info about
        /// type parameters.
        /// </summary>
        /// <param name="name">name of the base class</param>
        /// <returns>specialized base type type</returns>
        private ResolvableType FindSpecializedClassInHierarchy(string name)
        {
            /*
            IRppClass clazz = TargetType.Class;
            if (clazz.Name == name)
            {
                return TargetType;
            }

            ResolvedType baseRppType = clazz.BaseConstructorCall.BaseClassType;
            while (clazz != null && name != clazz.BaseConstructorCall.BaseClass.Name)
            {
                baseRppType = clazz.BaseConstructorCall.BaseClassType;
                clazz = clazz.BaseConstructorCall.BaseClass;
            }

            return baseRppType;
            */
            return null;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        #region Equality

        protected bool Equals(RppFieldSelector other)
        {
            return Equals(Field, other.Field);
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
            return Equals((RppFieldSelector) obj);
        }

        public override int GetHashCode()
        {
            return (Field != null ? Field.GetHashCode() : 0);
        }

        #endregion
    }
}