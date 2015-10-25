using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppFieldSelector : RppMember
    {
        public override RppType Type { get; protected set; }

        public RppType ClassType { get; private set; }
        public override ResolvableType Type2 { get; protected set; }

        [CanBeNull]
        public RppFieldInfo Field { get; private set; }

        public RppFieldSelector([NotNull] string name) : base(name)
        {
        }

        public override IRppNode Analyze(RppScope scope)
        {
            if (TargetType2 == null)
            {
                throw new Exception("TargetType should be specified before anaylyze is called");
            }

            //IEnumerable<RppType> typeArgs = T.BaseConstructorCall.BaseClassTypeArgs;

            RType classType = TargetType2;
            
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
                throw new Exception($"Can't find field {Name} for type {TargetType}");
            }

            Debug.Assert(classType != null, "obj != null");

            /*
            if (Field.Type.IsGenericParameter())
            {
                // Figure out generic args from the type of the target type
                // class Option[A]
                // class Some() extends Option[Int]
                // so 'Int' will be part of RppConstructorCall or 
                // class Some[A](val k : A)
                // is TargetType (RppGenericObjectType)

                ClassType = FindSpecializedClassInHierarchy(classType.Name);
                Debug.Assert(ClassType != null, "ClassType can't be null because we found that earlier");

                RppGenericObjectType genericClassType = ClassType as RppGenericObjectType;
                Debug.Assert(genericClassType != null, "Field is generic so has to be a ClassType");

                Type fieldRuntimeType = Field.Type.Runtime;
                Type = RppNativeType.Create(genericClassType.GenericArguments.ElementAt(fieldRuntimeType.GenericParameterPosition));
            }
            else
            {
                Debug.Assert(classType != null, "obj != null");
                ClassType = new RppObjectType(classType);
                Type = Field.Type;
            }
            */

            Type2 = new ResolvableType(Field.Type);

            return this;
        }

        /// <summary>
        /// Searches for specialized class in the hierarchy of <code>TargetType</code>.
        /// It goes through <code>BaseConstructorCall</code> because it has the info about
        /// type parameters.
        /// </summary>
        /// <param name="name">name of the base class</param>
        /// <returns>specialized base type type</returns>
        private ResolvedType FindSpecializedClassInHierarchy(string name)
        {
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