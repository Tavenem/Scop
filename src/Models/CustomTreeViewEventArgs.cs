namespace Scop;

public class CustomTreeViewEventArgs<T>
{
    public T? ParentValue { get; set; }
    public T? NodeValue { get; set; }
}
