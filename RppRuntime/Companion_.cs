namespace RppRuntime
{
    public sealed class Companion_
    {
        public static Companion_ Instance = new Companion_();

        public Companion_ Apply()
        {
            return null;
        }
    }

    class Companion
    {
        public void doSomething()
        {
            Companion_ c = Companion_.Instance.Apply();
        }
    }
}