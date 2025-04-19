using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PKToy.Lib;
using ReactiveUI;

namespace PKToy.ViewModels;
public class Node
{
    private readonly string _header = null;
    private readonly TopolNode _topolNode = null;
    public string Header => _header;
    public int Tag => _topolNode?.Tag ?? 0;
    public string ParentSense { get; set; } = null;
    public HashSet<Node> Parents { get; set; } = null;
    public ObservableCollection<Node> Children { get; set; } = null;
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
