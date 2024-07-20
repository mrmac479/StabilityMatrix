using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using StabilityMatrix.Avalonia.Controls;
using StabilityMatrix.Avalonia.Languages;
using StabilityMatrix.Avalonia.ViewModels.Base;
using StabilityMatrix.Avalonia.Views;
using StabilityMatrix.Core.Attributes;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;

namespace StabilityMatrix.Avalonia.ViewModels;

[View(typeof(ToolsPage))]
[Singleton]
public partial class ToolsViewModel : PageViewModelBase
{
    public override string Title => "Tools";
    public override IconSource IconSource =>
        new SymbolIconSource { Symbol = Symbol.Toolbox, IsFilled = true };

    public IReadOnlyList<TabItem> Pages { get; }

    [ObservableProperty]
    private TabItem? selectedPage;

    [ObservableProperty]
    private string? directory;

    public ToolsViewModel(
        OpenArtBrowserViewModel openArtBrowserViewModel,
        ComfyOrchastrationViewModel comfyOrchastrationViewModel
    )
    {
        Pages = new List<TabItem>(
            new List<TabViewModelBase>([comfyOrchastrationViewModel, openArtBrowserViewModel]).Select(
                vm => new TabItem { Header = vm.Header, Content = vm }
            )
        );
        SelectedPage = Pages.FirstOrDefault();
    }
}
