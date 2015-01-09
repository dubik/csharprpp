using System;

namespace CSharpRpp
{
    public class RppClass : RppNamedNode
    {
        public RppClass(string name) : base(name)
        {
        }

        public override void PreAnalyze(RppScope scope)
        {
            throw new NotImplementedException();
        }

        public override IRppNode Analyze(RppScope scope)
        {
            throw new NotImplementedException();
        }

        public void AddField(RppField field)
        {
        }

        public void AddFunc(RppFunc func)
        {
        }

        public void SetExtends(string name)
        {
        }
    }
}