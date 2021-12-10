using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Utilities;

namespace Scop.Shared;

public partial class CustomTreeView<T> : MudComponentBase
{
    private readonly List<CustomTreeViewItem<T>> _childItems = new();

    private CustomTreeViewItem<T>? _selectedValue;
    private HashSet<CustomTreeViewItem<T>>? _selectedValues;

    /// <summary>
    /// The color of the selected treeviewitem.
    /// </summary>
    [Parameter] public Color Color { get; set; } = Color.Primary;

    /// <summary>
    /// Check box color if multiselection is used.
    /// </summary>
    [Parameter] public Color CheckBoxColor { get; set; }

    /// <summary>
    /// Child content of component. Rendered before the templated items, if any.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// If true, compact vertical padding will be applied to all treeview items.
    /// </summary>
    [Parameter] public bool Dense { get; set; }

    /// <summary>
    /// If true, treeview will be disabled and all its childitems.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Child content of component. Rendered after the child content and the templated items, if any.
    /// </summary>
    [Parameter] public RenderFragment? EndContent { get; set; }

    /// <summary>
    /// If true, clicking anywhere on the item will expand it, if it has childs.
    /// </summary>
    [Parameter] public bool ExpandOnClick { get; set; }

    /// <summary>
    /// Setting a height will allow to scroll the treeview. If not set, it will try to grow in height.
    /// You can set this to any CSS value that the attribute 'height' accepts, i.e. 500px.
    /// </summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>
    /// Hover effect for item's on mouse-over.
    /// </summary>
    [Parameter] public bool Hover { get; set; }

    [Parameter] public IEnumerable<T>? Items { get; set; }

    /// <summary>
    /// ItemTemplate for rendering children.
    /// </summary>
    [Parameter] public RenderFragment<T>? ItemTemplate { get; set; }

    /// <summary>
    /// Setting a maximum height will allow to scroll the treeview. If not set, it will try to grow in height.
    /// You can set this to any CSS value that the attribute 'height' accepts, i.e. 500px.
    /// </summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>
    /// if true, multiple values can be selected via checkboxes which are automatically shown in the tree view.
    /// </summary>
    [Parameter] public bool MultiSelection { get; set; }

    /// <summary>
    /// Called whenever the selected value changed.
    /// </summary>
    [Parameter] public EventCallback<T> SelectedValueChanged { get; set; }

    /// <summary>
    /// Called whenever the selectedvalues changed.
    /// </summary>
    [Parameter] public EventCallback<HashSet<T>> SelectedValuesChanged { get; set; }

    [Parameter]
    public Func<T?, Task<IList<T>>>? ServerData { get; set; }

    /// <summary>
    /// Whether the tree is currently visible.
    /// </summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>
    /// Called whenever the visibility changed.
    /// </summary>
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }

    /// <summary>
    /// Setting a width the treeview. You can set this to any CSS value that the attribute 'height' accepts, i.e. 500px.
    /// </summary>
    [Parameter] public string? Width { get; set; }

    protected string Classname => new CssBuilder("mud-treeview")
        .AddClass("mud-treeview-dense", Dense)
        .AddClass("mud-treeview-hover", Hover)
        .AddClass($"mud-treeview-selected-{Color.ToDescriptionString()}")
        .AddClass($"mud-treeview-checked-{CheckBoxColor.ToDescriptionString()}")
        .AddClass(Class)
        .Build();

    protected string Stylename => new StyleBuilder()
        .AddStyle("flex-shrink", "0")
        .AddStyle("width", Width, !string.IsNullOrWhiteSpace(Width))
        .AddStyle("height", Height, !string.IsNullOrWhiteSpace(Height))
        .AddStyle("max-height", MaxHeight, !string.IsNullOrWhiteSpace(MaxHeight))
        .AddStyle(Style)
        .Build();

    internal bool IsSelectable { get; private set; }

    [CascadingParameter] private CustomTreeView<T> MudTreeRoot { get; set; }

    public CustomTreeView() => MudTreeRoot = this;

    protected override void OnInitialized() => IsSelectable = SelectedValueChanged.HasDelegate;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && MudTreeRoot == this)
        {
            await UpdateSelectedItems();
        }
    }

    internal void AddChild(CustomTreeViewItem<T> item) => _childItems.Add(item);

    internal async Task UpdateSelected(CustomTreeViewItem<T> item, bool requestedValue)
    {
        if ((_selectedValue == item && requestedValue)
            || (_selectedValue != item && !requestedValue))
        {
            return;
        }

        if (_selectedValue == item && !requestedValue)
        {
            _selectedValue = default;
            await item.Select(requestedValue);
            await SelectedValueChanged.InvokeAsync(default);
            return;
        }

        if (_selectedValue != null)
        {
            await _selectedValue.Select(false);
        }

        _selectedValue = item;
        await item.Select(requestedValue);
        await SelectedValueChanged.InvokeAsync(item.Value);
    }

    internal Task UpdateSelectedItems()
    {
        _selectedValues ??= new HashSet<CustomTreeViewItem<T>>();

        //collect selected items
        _selectedValues.Clear();
        foreach (var item in _childItems)
        {
            foreach (var selectedItem in item.GetSelectedItems())
            {
                _selectedValues.Add(selectedItem);
            }
        }

        return SelectedValuesChanged.InvokeAsync(new HashSet<T>(_selectedValues
            .Where(i => i is not null)
            .Select(i => i.Value!)));
    }
}
