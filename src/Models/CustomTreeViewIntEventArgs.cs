namespace Scop;

public class CustomTreeViewIntEventArgs<T>
{
    public T? ParentValue { get; set; }
    public T? NodeValue { get; set; }
    public int Value { get; set; }
}
