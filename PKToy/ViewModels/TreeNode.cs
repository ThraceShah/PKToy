using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PKToy.ViewModels;
public class Node
{
    private ObservableCollection<Node> _children;
    private int _childIndex = 10;

    public Node()
    {
        Header = "Item";
    }

    public Node(Node parent, int index)
    {
        Parent = parent;
        Header = parent.Header + ' ' + index;
    }

    public Node Parent { get; }
    public string Header { get; }
    public bool AreChildrenInitialized => _children != null;
    public ObservableCollection<Node> Children => _children ??= CreateChildren();
    public void AddItem() => Children.Add(new Node(this, _childIndex++));
    public void RemoveItem(Node child) => Children.Remove(child);
    public override string ToString() => Header;

    private ObservableCollection<Node> CreateChildren()
    {
        return new ObservableCollection<Node>(
            Enumerable.Range(0, 10).Select(i => new Node(this, i)));
    }
}
