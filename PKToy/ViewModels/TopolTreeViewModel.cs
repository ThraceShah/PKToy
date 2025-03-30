using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKToy.ViewModels;

class TopolTreeViewModel : ViewModelBase
{
    private readonly Node _root;
    private SelectionMode _selectionMode;

    public TopolTreeViewModel()
    {
        _root = new Node();

        Items = _root.Children;
        SelectedItems = new ObservableCollection<Node>();
    }

    public ObservableCollection<Node> Items { get; }
    public ObservableCollection<Node> SelectedItems { get; }

    public SelectionMode SelectionMode
    {
        get => _selectionMode;
        set
        {
            SelectedItems.Clear();
            this.RaiseAndSetIfChanged(ref _selectionMode, value);
        }
    }
}
