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

    public void UpdateTree()
    {
        Items.Clear();
        SelectedItems.Clear();
        _bodies = new Node("Bodies");
        Items.Add(_bodies);
        UpdateBodyTopolTree();
    }

    private void UpdateBodyTopolTree()
    {
        if (_bodies == null)
        {
            return;
        }
        var topolTree = PKSession.GetCurPartitionTopolTree();
        foreach (var body in topolTree)
        {
            MapTopolNode2Node(body, _bodies);
        }
    }

    private static void MapTopolNode2Node(TopolTreeNode topolNode, Node rootParent)
    {
        var map = new Dictionary<TopolTreeNode, Node>();
        var queue = new Queue<TopolTreeNode>();
        var root = new Node(topolNode.Headr);
        rootParent.Children.Add(root);
        root.Parents.Add(root);
        map[topolNode] = root;
        foreach (var child in topolNode.Children)
        {
            queue.Enqueue(child);
        }
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (map.TryGetValue(current, out var node) == false)
            {
                node = new Node(current.Headr);
                map[current] = node;
                foreach (var child in current.Children)
                {
                    queue.Enqueue(child);
                }
            }
            foreach (var parent in current.Parents)
            {
                if (map.TryGetValue(parent, out var parentNode))
                {
                    if (node.Parents.Contains(parentNode))
                    {
                        continue;
                    }
                    parentNode.Children.Add(node);
                    node.Parents.Add(parentNode);
                }
            }
        }
    }

}
