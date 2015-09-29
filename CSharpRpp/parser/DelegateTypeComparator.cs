namespace CSharpRpp.Parser
{
    /// <summary>
    /// Implementation of <code>ITypesComparator</code> which delegates types comparision to delegates.
    /// Just an auxilary class in case there is no need to create another class and implement
    /// <code>ITypesComparator</code> interface
    /// </summary>
    /// <typeparam name="T">type of the source 'type'</typeparam>
    class DelegateTypeComparator<T> : ITypesComparator<T>
    {
        private readonly OverloadQuery.TypesComparator<T> _typesComparator;
        private readonly OverloadQuery.CanCast<T> _canCast;

        public DelegateTypeComparator(OverloadQuery.TypesComparator<T> typesComparator, OverloadQuery.CanCast<T> canCast)
        {
            _typesComparator = typesComparator;
            _canCast = canCast;
        }

        public bool CanCast(T source, RppType target)
        {
            return _canCast(source, target);
        }

        public bool Compare(T source, RppType target)
        {
            return _typesComparator(source, target);
        }
    }
}