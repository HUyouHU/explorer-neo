using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CustomExplorer
{
    public sealed partial class MainWindow : Window
    {
        public ObservableCollection<FileSystemItem> FileSystemItems { get; set; }
        public ObservableCollection<TodoItem> FolderTodos { get; set; }
        public ObservableCollection<FolderNode> RootFolderNodes { get; set; }

        private FileSystemItem _selectedFileSystemItem;
        private FolderMetadata _currentMetadata;

        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = this; // Set DataContext for TreeView binding
            Title = "Custom Explorer";

            FileSystemItems = new ObservableCollection<FileSystemItem>();
            FolderTodos = new ObservableCollection<TodoItem>();
            RootFolderNodes = new ObservableCollection<FolderNode>();

            FileListView.ItemsSource = FileSystemItems;
            TodoListView.ItemsSource = FolderTodos;

            // Event handlers remain the same
            SaveChangesButton.Click += SaveChangesButton_Click;
            AddTodoButton.Click += AddTodoButton_Click;

            LoadFileSystemEntries(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            InitializeFolderTree();
        }

        private void InitializeFolderTree()
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.IsReady)
                {
                    var node = new FolderNode { Name = drive.Name, Path = drive.RootDirectory.FullName };
                    node.Children.Add(new FolderNode()); // Add dummy child for expansion
                    RootFolderNodes.Add(node);
                }
            }
        }

        private void FolderTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            var expandingNode = args.Node.Content as FolderNode;

            // If the node has the dummy child, remove it and load the real children.
            if (expandingNode != null && expandingNode.Children.Count == 1 && expandingNode.Children[0].Name == null)
            {
                expandingNode.Children.Clear();
                try
                {
                    var subDirectories = Directory.GetDirectories(expandingNode.Path);
                    foreach (var dirPath in subDirectories)
                    {
                        var dirName = new DirectoryInfo(dirPath).Name;
                        var childNode = new FolderNode { Name = dirName, Path = dirPath };

                        // Add dummy child to the new node if it has subdirectories
                        try
                        {
                            if (Directory.EnumerateDirectories(dirPath).Any())
                            {
                                childNode.Children.Add(new FolderNode()); // Dummy child
                            }
                        }
                        catch (UnauthorizedAccessException) { /* Ignore folders we can't access */ }

                        expandingNode.Children.Add(childNode);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Can't access this folder, so no children to add.
                }
            }
        }

        private void FolderTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var invokedNode = args.InvokedItem as FolderNode;
            if (invokedNode != null)
            {
                LoadFileSystemEntries(invokedNode.Path);
            }
        }

        // --- All other methods like LoadFileSystemEntries, SelectionChanged, etc. remain below ---
        private void LoadFileSystemEntries(string path)
        {
            if (!Directory.Exists(path)) { return; }
            AddressBar.Text = path;
            FileSystemItems.Clear();
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    var metadata = MetadataService.GetFolderMetadata(directory.FullName);
                    FileSystemItems.Add(new FileSystemItem { Name = directory.Name, DateModified = directory.LastWriteTime.ToString("g"), Type = "File folder", FullPath = directory.FullName, Color = metadata?.Color });
                }
                foreach (var file in directoryInfo.GetFiles())
                {
                    FileSystemItems.Add(new FileSystemItem { Name = file.Name, DateModified = file.LastWriteTime.ToString("g"), Type = file.Extension + " File", FullPath = file.FullName });
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error loading file system entries: {ex.Message}"); }
        }

        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedFileSystemItem = FileListView.SelectedItem as FileSystemItem;
            if (_selectedFileSystemItem == null || _selectedFileSystemItem.Type != "File folder")
            {
                DetailsPanel.Visibility = Visibility.Collapsed;
                _currentMetadata = null;
                FolderTodos.Clear();
                return;
            }
            DetailsPanel.Visibility = Visibility.Visible;
            _currentMetadata = MetadataService.GetFolderMetadata(_selectedFileSystemItem.FullPath);
            if (_currentMetadata == null) { _currentMetadata = new FolderMetadata { Path = _selectedFileSystemItem.FullPath }; }
            ColorTextBox.Text = _currentMetadata.Color ?? "";
            TagsTextBox.Text = _currentMetadata.Tags ?? "";
            CommentTextBox.Text = _currentMetadata.Comment ?? "";
            LoadTodosForFolder(_selectedFileSystemItem.FullPath);
        }

        private void LoadTodosForFolder(string path)
        {
            FolderTodos.Clear();
            var todos = MetadataService.GetTodosForFolder(path);
            foreach (var todo in todos) { FolderTodos.Add(todo); }
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMetadata == null) return;
            _currentMetadata.Color = ColorTextBox.Text;
            _currentMetadata.Tags = TagsTextBox.Text;
            _currentMetadata.Comment = CommentTextBox.Text;
            MetadataService.SaveFolderMetadata(_currentMetadata);
            if (_selectedFileSystemItem != null)
            {
                _selectedFileSystemItem.Color = _currentMetadata.Color;
                var index = FileSystemItems.IndexOf(_selectedFileSystemItem);
                if (index != -1) { FileSystemItems[index] = _selectedFileSystemItem; }
            }
        }

        private void AddTodoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFileSystemItem == null || string.IsNullOrWhiteSpace(NewTodoTextBox.Text)) return;
            var newTodo = new TodoItem { FolderPath = _selectedFileSystemItem.FullPath, Task = NewTodoTextBox.Text, IsCompleted = false };
            MetadataService.AddTodo(newTodo);
            NewTodoTextBox.Text = "";
            LoadTodosForFolder(_selectedFileSystemItem.FullPath);
        }

        private void TodoCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            long todoId = (long)checkBox.Tag;
            var todo = FolderTodos.FirstOrDefault(t => t.Id == todoId);
            if (todo != null) { todo.IsCompleted = checkBox.IsChecked ?? false; MetadataService.UpdateTodo(todo); }
        }

        private void TodoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            long todoId = (long)textBox.Tag;
            var todo = FolderTodos.FirstOrDefault(t => t.Id == todoId);
            if (todo != null && todo.Task != textBox.Text) { todo.Task = textBox.Text; MetadataService.UpdateTodo(todo); }
        }

        private void DeleteTodoButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            long todoId = (long)button.Tag;
            MetadataService.DeleteTodo(todoId);
            LoadTodosForFolder(_selectedFileSystemItem.FullPath);
        }

        private void FileListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            var clickedItem = (e.OriginalSource as FrameworkElement)?.DataContext as FileSystemItem;
            if (clickedItem != null && clickedItem.Type == "File folder") { LoadFileSystemEntries(clickedItem.FullPath); }
        }
    }
}
