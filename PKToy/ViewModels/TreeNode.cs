using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PKToy.ViewModels;
public class Node
{
    public Node(string header)
    {
        Header = header;
    }

    public string Header { get; }
    public HashSet<Node> Parents { get; } = [];
    public ObservableCollection<Node> Children { get; } = [];
    public override string ToString() => Header;

}
