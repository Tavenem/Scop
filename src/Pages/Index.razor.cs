using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Scop.Pages;

public partial class Index
{
    [CascadingParameter] private MudTheme? MudTheme { get; set; }
}
