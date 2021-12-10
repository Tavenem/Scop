namespace Scop;

public class CustomTreeViewBoolEventArgs<T>
{
    public T? ParentValue { get; set; }
    public T? NodeValue { get; set; }
    public bool Value { get; set; }
}
