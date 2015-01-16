namespace CSharpRpp
{
    public interface ITypeProvider
    {
        void CodegenType(RppScope scope, CodegenContext ctx);
    }
}