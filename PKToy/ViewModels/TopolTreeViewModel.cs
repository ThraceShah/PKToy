using Avalonia.Controls;
using PKToy.Lib;
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
    private Node _bodies;
    private SelectionMode _selectionMode;

    public TopolTreeViewModel()
    {
        Items = [];
        SelectedItems = [];
    }

    private ObservableCollection<Node> _items;
    public ObservableCollection<Node> Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value);
    }
    private ObservableCollection<Node> _selectedItems;

    public ObservableCollection<Node> SelectedItems
    {
        get => _selectedItems;
        set => this.RaiseAndSetIfChanged(ref _selectedItems, value);
    }

    public SelectionMode SelectionMode
    {
        get => _selectionMode;
        set
        {
            SelectedItems.Clear();
            this.RaiseAndSetIfChanged(ref _selectionMode, value);
        }
    }

    public void UpdateTree(int partitionTag = 0)
    {
        Items = [];
        SelectedItems = [];
        _bodies = new Node("Bodies")
        {
            Children = []
        };
        Items.Add(_bodies);
        UpdateBodyTopolTree(partitionTag);
    }

    private void UpdateBodyTopolTree(int partitionTag = 0)
    {
        if (_bodies == null)
        {
            return;
        }
        if (partitionTag == 0)
        {
            var tables = PKSession.GetCurPartitionTopolTree();
            foreach (var body in tables)
            {
                MapTopolNode2Node(body, _bodies);
            }
        }
        else
        {
            var tables = PKSession.GetPartitionTopolTree(partitionTag);
            foreach (var body in tables)
            {
                MapTopolNode2Node(body, _bodies);
            }
        }
    }

    private static void MapTopolNode2Node(TopolTable table, Node rootParent)
    {
        if (table.Nodes.Count == 0)
        {
            return;
        }
        var relations = table.Relations;
        var nodes = new Node[table.Nodes.Count];
        for (int i = 0; i < table.Nodes.Count; i++)
        {
            var tableNode = table.Nodes[i];
            nodes[i] = new Node(tableNode);
        }
        foreach (var relation in relations)
        {
            nodes[relation.Parent].Children ??= [];
            nodes[relation.Child].Parents ??= [];
        }
        foreach (var relation in relations)
        {
            var parent = nodes[relation.Parent];
            var child = nodes[relation.Child];
            if (child.Parents.Count > 0)
            {
                var copyChild = new Node(table.Nodes[relation.Child])
                {
                    Children = child.Children,
                    Parents = child.Parents
                };
                child = copyChild;
            }
            child.ParentSense = relation.Sense;
            parent.Children.Add(child);
            child.Parents.Add(parent);
        }
        var bodyNode = nodes[0];
        rootParent.Children.Add(bodyNode);
    }
}
