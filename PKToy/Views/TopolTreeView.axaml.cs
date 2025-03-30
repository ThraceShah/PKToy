using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PKToy.ViewModels;

namespace PKToy.Views;

public partial class TopolTreeView : UserControl
{
    public TopolTreeView()
    {
        InitializeComponent();
        DataContext = new TopolTreeViewModel();
    }
}