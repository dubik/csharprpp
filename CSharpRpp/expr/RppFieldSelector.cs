using System;
using System.Linq;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppFieldSelector : RppMember
    {
        public override RppType Type { get; protected set; }

        [CanBeNull]
        public RppField Field { get; private set; }

        public RppFieldSelector([NotNull] string name) : base(name)
        {
        }

        public override IRppNode Analyze(RppScope scope)
        {
            if (TargetType == null)
            {
                throw new Exception("TargetType should be specified before anaylyze is called");
            }

            IRppClass obj = TargetType.Class;
            while (obj != null && Field == null)
            {
                Field = obj.Fields.FirstOrDefault(f => f.Name == Name);
                obj = obj.BaseClass;
            }

            if (Field == null)
            {
                throw new Exception(string.Format("Can't find field {0} for type {1}", Name, TargetType));
            }

            Type = Field.Type;

            return this;
        }

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Accept(this);
        }

        #region Equality

        protected bool Equals(RppFieldSelector other)
        {
            return Equals(Field, other.Field);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RppFieldSelector) obj);
        }

        public override int GetHashCode()
        {
            return (Field != null ? Field.GetHashCode() : 0);
        }

        #endregion
    }
}