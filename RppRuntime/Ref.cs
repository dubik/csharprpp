public class Ref<T>
{
    public T elem;

    public Ref()
    {
    }

    public Ref(T elem)
    {
        this.elem = elem;
    }

    public override string ToString()
    {
        return elem.ToString();
    }
}