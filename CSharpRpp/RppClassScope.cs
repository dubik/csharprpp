using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace CSharpRpp
{
    // TODO don't like the way search is done, need to redo
    public class RppClassScope : RppScope
    {
        public RppClassScope BaseClassScope { get; set; }

        public RppClassScope([CanBeNull] RppScope parentScope) : base(parentScope)
        {
        }

        public override IReadOnlyCollection<IRppFunc> LookupFunction(string name, bool searchParentScope = true)
        {
            var members = LookupMember(name);
            if (members.Count != 0)
            {
                return members;
            }

            return searchParentScope && ParentScope != null ? ParentScope.LookupFunction(name) : Collections.NoFuncsCollection;
        }

        /// <summary>
        /// Looks up class members with the specified name, doesn't look in the parent scope, if not found looks in the base class
        /// </summary>
        /// <param name="name">name of the member</param>
        /// <returns>list of matching functions</returns>
        [NotNull]
        protected IReadOnlyCollection<IRppFunc> LookupMember(string name)
        {
            var current = DoLookupFunction(name, false).ToList();
            var baseMembers = BaseClassScope?.LookupMember(name) ?? Collections.NoFuncsCollection;
            current.AddRange(baseMembers);
            return current;
        }
    }
}