namespace CSharpRpp
{
    public class RppBreak : RppNode
    {
        public static RppBreak Instance = new RppBreak();

        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}