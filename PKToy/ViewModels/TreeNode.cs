using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PKToy.Lib;

namespace PKToy.ViewModels;
public class Node(string header)
{
    public string Header => header;
    public string ParentSense { get; set; } = null;
    public HashSet<Node> Parents { get; set; } = null;
    public ObservableCollection<Node> Children { get; set; } = null;
    public override string ToString() => $"{Header} ({ParentSense})";

}
