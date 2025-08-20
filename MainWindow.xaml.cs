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

        private FileSystemItem _selectedFileSystemItem;
        private FolderMetadata _currentMetadata;

        public MainWindow()
        {
            this.InitializeComponent();
            Title = "Custom Explorer";

            FileSystemItems = new ObservableCollection<FileSystemItem>();
            FolderTodos = new ObservableCollection<TodoItem>();

            FileListView.ItemsSource = FileSystemItems;
            TodoListView.ItemsSource = FolderTodos;

            FileListView.SelectionChanged += FileListView_SelectionChanged;
            SaveChangesButton.Click += SaveChangesButton_Click;
            AddTodoButton.Click += AddTodoButton_Click;

            LoadFileSystemEntries(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        private void LoadFileSystemEntries(string path)
        {
            FileSystemItems.Clear();
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    var metadata = MetadataService.GetFolderMetadata(directory.FullName);
                    FileSystemItems.Add(new FileSystemItem
                    {
                        Name = directory.Name,
                        DateModified = directory.LastWriteTime.ToString("g"),
                        Type = "File folder",
                        FullPath = directory.FullName,
                        Color = metadata?.Color
                    });
                }
                foreach (var file in directoryInfo.GetFiles())
                {
                    // Files don't have metadata in our current design
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
            if (_currentMetadata == null)
            {
                _currentMetadata = new FolderMetadata { Path = _selectedFileSystemItem.FullPath };
            }

            ColorTextBox.Text = _currentMetadata.Color ?? "";
            TagsTextBox.Text = _currentMetadata.Tags ?? "";
            CommentTextBox.Text = _currentMetadata.Comment ?? "";

            LoadTodosForFolder(_selectedFileSystemItem.FullPath);
        }

        private void LoadTodosForFolder(string path)
        {
            FolderTodos.Clear();
            var todos = MetadataService.GetTodosForFolder(path);
            foreach (var todo in todos)
            {
                FolderTodos.Add(todo);
            }
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMetadata == null) return;

            _currentMetadata.Color = ColorTextBox.Text;
            _currentMetadata.Tags = TagsTextBox.Text;
            _currentMetadata.Comment = CommentTextBox.Text;

            MetadataService.SaveFolderMetadata(_currentMetadata);

            // Update the item in the list to reflect the color change immediately
            if (_selectedFileSystemItem != null)
            {
                _selectedFileSystemItem.Color = _currentMetadata.Color;

                // This is a bit of a hack to force the UI to refresh the item.
                // A better way is to have FileSystemItem implement INotifyPropertyChanged.
                var index = FileSystemItems.IndexOf(_selectedFileSystemItem);
                if (index != -1)
                {
                    FileSystemItems[index] = _selectedFileSystemItem;
                }
            }
        }

        private void AddTodoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFileSystemItem == null || string.IsNullOrWhiteSpace(NewTodoTextBox.Text)) return;

            var newTodo = new TodoItem
            {
                FolderPath = _selectedFileSystemItem.FullPath,
                Task = NewTodoTextBox.Text,
                IsCompleted = false
            };
            MetadataService.AddTodo(newTodo);
            NewTodoTextBox.Text = "";
            LoadTodosForFolder(_selectedFileSystemItem.FullPath);
        }

        private void TodoCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            long todoId = (long)checkBox.Tag;
            var todo = FolderTodos.FirstOrDefault(t => t.Id == todoId);
            if (todo != null)
            {
                todo.IsCompleted = checkBox.IsChecked ?? false;
                MetadataService.UpdateTodo(todo);
            }
        }

        private void TodoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            long todoId = (long)textBox.Tag;
            var todo = FolderTodos.FirstOrDefault(t => t.Id == todoId);
            if (todo != null && todo.Task != textBox.Text)
            {
                todo.Task = textBox.Text;
                MetadataService.UpdateTodo(todo);
            }
        }

        private void DeleteTodoButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            long todoId = (long)button.Tag;
            MetadataService.DeleteTodo(todoId);
            LoadTodosForFolder(_selectedFileSystemItem.FullPath);
        }
    }
}
