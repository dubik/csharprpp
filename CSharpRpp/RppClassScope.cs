using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace CSharpRpp
{
    public class RppClassScope : RppScope
    {
        private readonly RppClassScope _baseClassScope;

        public RppClassScope([CanBeNull] RppClassScope baseClassScope, [CanBeNull] RppScope parentScope) : base(parentScope)
        {
            _baseClassScope = baseClassScope;
        }

        public override IReadOnlyCollection<IRppFunc> LookupFunction(string name, bool searchParentScope = true)
        {
            var members = LookupMember(name);
            if (members.Count != 0)
            {
                return members;
            }

            return base.LookupFunction(name);
        }

        /// <summary>
        /// Looks up class members with the specified name, doesn't look in the parent scope, if not found looks in the base class
        /// </summary>
        /// <param name="name">name of the member</param>
        /// <returns>list of matching functions</returns>
        [NotNull]
        protected IReadOnlyCollection<IRppFunc> LookupMember(string name)
        {
            var members = LookupFunction(name, false);
            if (members.Count != 0)
            {
                return members;
            }

            return _baseClassScope != null ? _baseClassScope.LookupMember(name) : Collections.NoFuncsCollection;
        }
    }
}