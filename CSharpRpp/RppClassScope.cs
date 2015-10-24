using System.Collections.Generic;
using System.Linq;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;

namespace CSharpRpp
{
    // TODO don't like the way search is done, need to redo
    public class RppClassScope : RppScope
    {
        public RppClassScope BaseClassScope { get; set; }

        public RType Type2 { get; }

        public RppClassScope([CanBeNull] RppScope parentScope, RType type2) : base(parentScope)
        {
            Type2 = type2;
        }

        public override IReadOnlyCollection<RppMethodInfo> LookupFunction(string name, bool searchParentScope = true)
        {
            var members = LookupMember(name);
            if (members.Count != 0)
            {
                return members;
            }

            return searchParentScope && ParentScope != null ? ParentScope.LookupFunction(name) : Collections.NoRFuncsCollection;
        }

        /// <summary>
        /// Looks up class members with the specified name, doesn't look in the parent scope, if not found looks in the base class
        /// </summary>
        /// <param name="name">name of the member</param>
        /// <returns>list of matching functions</returns>
        [NotNull]
        protected IReadOnlyCollection<RppMethodInfo> LookupMember(string name)
        {
            var current = FindMethods(name).ToList();
            var baseMembers = BaseClassScope?.LookupMember(name) ?? Collections.NoRFuncsCollection;
            current.AddRange(baseMembers);
            return current;
        }

        private IEnumerable<RppMethodInfo> FindMethods(string name)
        {
            if (name == "this")
            {
                return Type2.Constructors;
            }

            return Type2.Methods.Where(m => m.Name == name);
        }
    }
}