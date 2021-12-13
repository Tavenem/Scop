using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Utilities;
using System.Globalization;
using System.Windows.Input;
using MudIcons = MudBlazor.Icons;

namespace Scop.Shared;

public partial class CustomTreeViewItem<T> : MudComponentBase
{
    private readonly List<CustomTreeViewItem<T>> _childItems = new();
    private readonly Converter<T> _converter = new DefaultConverter<T>();

    private bool _disabled;
    private bool _isChecked, _isSelected, _isServerLoaded;

    [Parameter]
    public bool Activated
    {
        get => _isSelected;
        set => _ = MudTreeRoot?.UpdateSelected(this, value);
    }

    /// <summary>
    /// Called whenever the activated value changed.
    /// </summary>
    [Parameter] public EventCallback<bool> ActivatedChanged { get; set; }

    /// <summary>
    /// Custom checked icon, leave null for default.
    /// </summary>
    [Parameter] public string CheckedIcon { get; set; } = MudIcons.Material.Filled.CheckBox;

    /// <summary>
    /// Child content of component used to create sub levels.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Command executed when the user clicks on the item.
    /// </summary>
    [Parameter] public ICommand? Command { get; set; }

    /// <summary>
    /// Content of the item, placed before the text.
    /// </summary>
    [Parameter] public RenderFragment? Content { get; set; }

    [Parameter] public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    /// <summary>
    /// If true, treeviewitem will be disabled.
    /// </summary>
    [Parameter]
    public bool Disabled
    {
        get => _disabled || (MudTreeRoot?.Disabled ?? false);
        set => _disabled = value;
    }

    /// <summary>
    /// Child content of component. Rendered after the child content and the templated items, if any.
    /// </summary>
    [Parameter] public RenderFragment? EndContent { get; set; }

    /// <summary>
    /// Icon placed after the text if set.
    /// </summary>
    [Parameter] public string? EndIcon { get; set; }

    /// <summary>
    /// The color of the icon. It supports the theme colors.
    /// </summary>
    [Parameter] public Color EndIconColor { get; set; }

    /// <summary>
    /// Rendered after the text if set. Replaces endicon.
    /// </summary>
    [Parameter] public RenderFragment? EndIcons { get; set; }

    /// <summary>
    /// Tooltip for the end icon, if set.
    /// </summary>
    [Parameter] public string? EndIconTooltip { get; set; }

    /// <summary>
    /// The text at the end of the item.
    /// </summary>
    [Parameter] public string? EndText { get; set; }

    /// <summary>
    /// User class names for the endtext, separated by space.
    /// </summary>
    [Parameter] public string? EndTextClass { get; set; }

    /// <summary>
    /// Tyopography for the endtext.
    /// </summary>
    [Parameter] public Typo EndTextTypo { get; set; } = Typo.body1;

    /// <summary>
    /// Expand or collapse treeview item when it has children. Two-way bindable. Note: if you directly set this to
    /// true or false (instead of using two-way binding) it will force the item's expansion state.
    /// </summary>
    [Parameter] public bool Expanded { get; set; }

    /// <summary>
    /// Called whenever expanded changed.
    /// </summary>
    [Parameter] public EventCallback<bool> ExpandedChanged { get; set; }

    /// <summary>
    /// The expand/collapse icon.
    /// </summary>
    [Parameter] public string ExpandedIcon { get; set; } = MudIcons.Material.Filled.ChevronRight;

    /// <summary>
    /// The color of the expand/collapse button. It supports the theme colors.
    /// </summary>
    [Parameter] public Color ExpandedIconColor { get; set; }

    /// <summary>
    /// Icon placed before the text if set.
    /// </summary>
    [Parameter] public string? Icon { get; set; }

    /// <summary>
    /// Index of the icon placed before the text, if icons is set.
    /// </summary>
    [Parameter] public int IconIndex { get; set; }

    /// <summary>
    /// Called when the index of the icon placed before the text changes.
    /// </summary>
    [Parameter] public EventCallback<int> IconIndexChanged { get; set; }

    /// <summary>
    /// Called when the index of the icon placed before the text changes.
    /// </summary>
    [Parameter] public EventCallback<CustomTreeViewIntEventArgs<T>> OnIconIndexChange { get; set; }

    /// <summary>
    /// Set of possible icons placed before the text.
    /// </summary>
    [Parameter] public List<string>? Icons { get; set; }

    /// <summary>
    /// The color of the icon. It supports the theme colors.
    /// </summary>
    [Parameter] public Color IconColor { get; set; }

    [Parameter] public IList<T>? Items { get; set; }

    public bool Loading { get; set; }

    /// <summary>
    /// The loading icon.
    /// </summary>
    [Parameter] public string LoadingIcon { get; set; } = MudIcons.Material.Filled.Loop;

    /// <summary>
    /// The color of the loading. It supports the theme colors.
    /// </summary>
    [Parameter] public Color LoadingIconColor { get; set; }

    /// <summary>
    /// Tree item click event.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    [Parameter] public EventCallback<CustomTreeViewEventArgs<T>> OnDrag { get; set; }

    [Parameter] public EventCallback<CustomTreeViewEventArgs<T>> OnDrop { get; set; }

    /// <summary>
    /// Tree item end icon click event.
    /// </summary>
    [Parameter] public EventCallback<CustomTreeViewEventArgs<T>> OnEndIconClick { get; set; }

    /// <summary>
    /// Whether this item can be selected. Only applies if the tree has multiselect enabled. Default is <see langword="true"/>.
    /// </summary>
    [Parameter] public bool Selectable { get; set; } = true;

    [Parameter]
    public bool Selected
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value)
            {
                return;
            }

            _isChecked = value;
            MudTreeRoot?.UpdateSelectedItems();
            SelectedChanged.InvokeAsync(_isChecked);
            OnSelect.InvokeAsync(new()
            {
                NodeValue = Value,
                ParentValue = ParentValue,
                Value = _isChecked,
            });
        }
    }

    /// <summary>
    /// Called whenever the selected value changed.
    /// </summary>
    [Parameter] public EventCallback<bool> SelectedChanged { get; set; }

    /// <summary>
    /// Whether to show the text. Default is <see langword="true"/>.
    /// </summary>
    [Parameter] public bool ShowText { get; set; } = true;

    /// <summary>
    /// Called whenever the selected value changed.
    /// </summary>
    [Parameter] public EventCallback<CustomTreeViewBoolEventArgs<T>> OnSelect { get; set; }

    /// <summary>
    /// The text to display
    /// </summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>
    /// User class names for the text, separated by space.
    /// </summary>
    [Parameter] public string? TextClass { get; set; }

    public string TextClassname => new CssBuilder("mud-treeview-item-label")
        .AddClass(TextClass)
        .Build();

    /// <summary>
    /// User styles for the text, applied on top of the component's own classes and styles.
    /// </summary>
    [Parameter] public string? TextStyle { get; set; }

    /// <summary>
    /// Tyopography for the text.
    /// </summary>
    [Parameter] public Typo TextTypo { get; set; } = Typo.body1;

    /// <summary>
    /// Custom unchecked icon, leave null for default.
    /// </summary>
    [Parameter] public string UncheckedIcon { get; set; } = MudIcons.Material.Filled.CheckBoxOutlineBlank;

    /// <summary>
    /// Value of the treeviewitem.
    /// </summary>
    [Parameter] public T? Value { get; set; }

    protected bool ArrowExpanded
    {
        get => Expanded;
        set
        {
            if (value == Expanded)
            {
                return;
            }

            Expanded = value;
            ExpandedChanged.InvokeAsync(value);
        }
    }

    protected string Classname => new CssBuilder("mud-treeview-item")
        .AddClass(Class)
        .Build();

    protected string ContentClassname => new CssBuilder("mud-treeview-item-content")
        .AddClass("cursor-pointer", MudTreeRoot?.IsSelectable == true || (MudTreeRoot?.ExpandOnClick == true && HasChild))
        .AddClass("mud-treeview-item-selected", _isSelected)
        .Build();

    protected bool IsChecked
    {
        get => Selected;
        set => _ = SelectItem(value, this);
    }

    private bool HasChild => ChildContent is not null
        || EndContent is not null
        || HasRealChild;

    private bool HasRealChild => (MudTreeRoot is not null && Items is not null && Items.Count != 0)
        || (MudTreeRoot?.ServerData is not null && !_isServerLoaded && (Items is null || Items.Count == 0));

    private bool ShowChildren => Expanded || (Parent?.Expanded != false && HasRealChild);

    [CascadingParameter] private CustomTreeView<T>? MudTreeRoot { get; set; }

    [CascadingParameter] private CustomTreeViewItem<T>? Parent { get; set; }

    private T? ParentValue => Parent is null ? default : Parent.Value;

    protected override void OnInitialized()
    {
        if (Parent is not null)
        {
            Parent.AddChild(this);
        }
        else
        {
            MudTreeRoot?.AddChild(this);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _isSelected && MudTreeRoot is not null)
        {
            await MudTreeRoot.UpdateSelected(this, _isSelected);
        }
    }

    protected async Task OnItemClicked(MouseEventArgs ev)
    {
        if (MudTreeRoot?.IsSelectable ?? false)
        {
            await MudTreeRoot.UpdateSelected(this, !_isSelected);
        }

        if (HasChild && (MudTreeRoot?.ExpandOnClick ?? false))
        {
            Expanded = !Expanded;
            TryInvokeServerLoadFunc();
            await ExpandedChanged.InvokeAsync(Expanded);
        }

        await OnClick.InvokeAsync(ev);
        if (Command?.CanExecute(Value) ?? false)
        {
            Command.Execute(Value);
        }
    }

    protected Task OnItemExpanded(bool expanded)
    {
        if (Expanded == expanded)
        {
            return Task.CompletedTask;
        }

        Expanded = expanded;
        TryInvokeServerLoadFunc();
        return ExpandedChanged.InvokeAsync(expanded);
    }

    internal IEnumerable<CustomTreeViewItem<T>> GetSelectedItems()
    {
        if (_isChecked)
        {
            yield return this;
        }

        foreach (var treeItem in _childItems)
        {
            foreach (var selected in treeItem.GetSelectedItems())
            {
                yield return selected;
            }
        }
    }

    internal Task Select(bool value)
    {
        if (_isSelected == value)
        {
            return Task.CompletedTask;
        }

        _isSelected = value;

        StateHasChanged();

        return ActivatedChanged.InvokeAsync(_isSelected);
    }

    internal async Task SelectItem(bool value, CustomTreeViewItem<T>? source = null)
    {
        if (value == _isChecked)
        {
            return;
        }

        _isChecked = value;
        if (value)
        {
            if (Parent is not null)
            {
                await Parent.SelectItem(value, source);
            }
        }
        else
        {
            _childItems.ForEach(async c => await c.SelectItem(value, source));
        }

        StateHasChanged();

        await SelectedChanged.InvokeAsync(_isChecked);

        if (source == this)
        {
            await OnSelect.InvokeAsync(new()
            {
                NodeValue = Value,
                ParentValue = ParentValue,
                Value = _isChecked,
            });

            if (MudTreeRoot is not null)
            {
                await MudTreeRoot.UpdateSelectedItems();
            }
        }
    }

    internal async void TryInvokeServerLoadFunc()
    {
        if (Expanded && (Items is null || Items.Count == 0) && MudTreeRoot?.ServerData is not null)
        {
            Loading = true;
            StateHasChanged();

            Items = await MudTreeRoot.ServerData(Value);

            Loading = false;
            _isServerLoaded = true;

            StateHasChanged();
        }
    }

    private void AddChild(CustomTreeViewItem<T> item) => _childItems.Add(item);

    private Task OnDragStartAsync() => OnDrag.InvokeAsync(new()
    {
        NodeValue = Value,
        ParentValue = ParentValue,
    });

    private Task OnDropAsync() => OnDrop.InvokeAsync(new()
    {
        NodeValue = Value,
        ParentValue = ParentValue,
    });

    private Task OnEndIconClickAsync() => OnEndIconClick.InvokeAsync(new()
    {
        NodeValue = Value,
        ParentValue = ParentValue,
    });

    private async Task OnSelectIconIndex(int index)
    {
        IconIndex = index;
        await IconIndexChanged.InvokeAsync(IconIndex);
        await OnIconIndexChange.InvokeAsync(new()
        {
            NodeValue = Value,
            ParentValue = ParentValue,
            Value = index,
        });
    }
}
