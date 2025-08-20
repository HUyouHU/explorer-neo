using System.Collections.ObjectModel;

namespace CustomExplorer
{
    public class FolderNode
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public ObservableCollection<FolderNode> Children { get; set; }

        public FolderNode()
        {
            Children = new ObservableCollection<FolderNode>();
        }
    }
}
