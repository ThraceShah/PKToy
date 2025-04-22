namespace PKToy.Views;
using Avalonia.Controls.Templates;
using PKToy.Lib;
using System.Collections.Generic;
using System.Collections.ObjectModel;
public class TopolTreeView : MvuView
{
    private TreeView _treeView = null!;
    private Node? _bodies = null;
    private ObservableCollection<Node>? Items { get; set; } = null;
    private Node? _selectedNode = null;
    private Node? SelectedNode
    {
        get => _selectedNode;
        set
        {
            _selectedNode = value;
            if (_selectedNode is null)
            {
                base.UpdateState();
                return;
            }
            Console.WriteLine(_selectedNode.ParentSense);
            if (TryGetChildren(_selectedNode))
            {
                base.UpdateState();
            }
        }
    }

    protected override object Build() =>
    New<TreeView>().Ref(out _treeView).SelectionMode(SelectionMode.Toggle)
    .ItemsSource(() => Items!, v => Items = (ObservableCollection<Node>?)v)
    .ItemTemplate(new FuncTreeDataTemplate<Node>(
            (node, _) => BindNode(node),
            n => n.Children!))
    .SelectedItem(() => SelectedNode!, v => SelectedNode = (Node?)v);

    private TextBlock BindNode(Node node)
    {
        if (_treeView.ContainerFromItem(node) is TreeViewItem container)
        {
            container.Expanded += OnNodeExpanded;
        }
        return New<TextBlock>().Text(node.Header ?? string.Empty);
    }

    private void OnNodeExpanded(object? sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem item && item.DataContext is Node node)
        {
            if (TryGetChildren(node))
            {
                base.UpdateState();
            }
        }
    }

    public void UpdateTree(int partitionTag = 0)
    {
        Items = [];
        _bodies = new Node("Bodies")
        {
            Children = []
        };
        Items.Add(_bodies);
        UpdateBodyTopolTree(partitionTag);
        base.UpdateState();
    }

    // private void UpdateBodyTopolTree(int partitionTag = 0)
    // {
    //     if (_bodies == null)
    //     {
    //         return;
    //     }
    //     if (partitionTag == 0)
    //     {
    //         var tables = PKSession.GetCurPartitionTopolTree();
    //         foreach (var body in tables)
    //         {
    //             MapTopolNode2Node(body, _bodies);
    //         }
    //     }
    //     else
    //     {
    //         var tables = PKSession.GetPartitionTopolTree(partitionTag);
    //         foreach (var body in tables)
    //         {
    //             MapTopolNode2Node(body, _bodies);
    //         }
    //     }
    // }

    private void UpdateBodyTopolTree(int partition = 0)
    {
        if (_bodies == null)
        {
            return;
        }
        var nodes = partition == 0 ? PKSession.GetCurPartitionBodyNodes() : PKSession.GetPartitionBodyNodes(partition);
        foreach (var node in nodes)
        {
            _bodies.Children!.Add(new(node));
        }
    }

    private static bool TryGetChildren(Node node)
    {
        if (node.Tag == 0)
        {
            node.HasBeenExpanded = true;
            return false;
        }
        if (node.HasBeenExpanded)
        {
            return false;
        }
        if (node.Children != null)
        {
            return false;
        }
        var table = PKSession.GetEntityTable(node.Tag);
        if (table is null)
        {
            return false;
        }
        MapTopolNode2Node(table, node);
        return true;
    }


    private static void MapTopolNode2Node(TopolTable table, Node firstNode)
    {
        if (table.Nodes.Count == 0)
        {
            return;
        }
        var relations = table.Relations;
        var nodes = new Node[table.Nodes.Count];
        nodes[0] = firstNode;
        for (int i = 1; i < table.Nodes.Count; i++)
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
            if (child.Parents!.Count > 0)
            {
                var copyChild = new Node(table.Nodes[relation.Child])
                {
                    Children = child.Children,
                    Parents = child.Parents
                };
                child = copyChild;
            }
            child.ParentSense = relation.Sense;
            parent.Children!.Add(child);
            child.Parents.Add(parent);
        }
    }

}


public class Node
{
    private readonly string? _header = null;
    private readonly TopolNode? _topolNode = null;
    public string? Header => _header;
    public int Tag => _topolNode?.Tag ?? 0;
    public string? ParentSense { get; set; } = null;
    public HashSet<Node>? Parents { get; set; } = null;
    public ObservableCollection<Node>? Children { get; set; } = null;
    public bool HasBeenExpanded { get; set; } = false;

    public override string ToString() => $"{Header} ({ParentSense})";

    public Node(string header)
    {
        _header = header;
    }

    public Node(TopolNode node)
    {
        _topolNode = node;
        _header = node.Header;
    }
}
