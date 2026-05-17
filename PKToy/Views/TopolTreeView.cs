namespace PKToy.Views;
using Avalonia.Controls.Templates;
using PKToy.Lib;
using System.Collections.Generic;
using System.Collections.ObjectModel;
public class TopolTreeView : ViewBase
{
    private Node? _bodies = null;
    private ObservableCollection<Node>? Items { get; set; } = null;
    private Node? _selectedNode = null;
    private Node? SelectedNode
    {
        get => _selectedNode;
        set
        {
            _selectedNode = value;
            Console.WriteLine(_selectedNode?.ParentSense);
        }
    }

    protected override object Build()
    {
        var tree = New<TreeView>().SelectionMode(SelectionMode.Toggle)
            .ItemTemplate(new FuncTreeDataTemplate<Node>(
                (node, _) => New<TextBlock>().Text(node.Header ?? string.Empty),
                n => n.Children!));

        tree.ItemsSource = Items;
        tree.SelectedItem = SelectedNode;
        tree.SelectionChanged += (_, _) => SelectedNode = tree.SelectedItem as Node;
        return tree;
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
        Reload();
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
        var bodyNode = nodes[0];
        rootParent.Children!.Add(bodyNode);
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
