using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace CustomExplorer
{
    public class MetadataService
    {
        private static string _dbPath;

        public static void InitializeDatabase()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataFolder, "CustomExplorer");
            Directory.CreateDirectory(appFolder); // Ensure the directory exists

            _dbPath = Path.Combine(appFolder, "metadata.db");

            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();

                var createMetadataTable = @"
                    CREATE TABLE IF NOT EXISTS FolderMetadata (
                        Path TEXT PRIMARY KEY,
                        Color TEXT,
                        Tags TEXT,
                        Comment TEXT
                    );";

                var createTodosTable = @"
                    CREATE TABLE IF NOT EXISTS Todos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FolderPath TEXT NOT NULL,
                        Task TEXT NOT NULL,
                        IsCompleted INTEGER NOT NULL DEFAULT 0,
                        FOREIGN KEY (FolderPath) REFERENCES FolderMetadata(Path) ON DELETE CASCADE
                    );";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = createMetadataTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createTodosTable;
                    command.ExecuteNonQuery();
                }
            }
        }

        private static SqliteConnection GetConnection() => new SqliteConnection($"Data Source={_dbPath}");

        // --- FolderMetadata Methods ---

        public static FolderMetadata GetFolderMetadata(string path)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Color, Tags, Comment FROM FolderMetadata WHERE Path = @Path";
                command.Parameters.AddWithValue("@Path", path);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new FolderMetadata
                        {
                            Path = path,
                            Color = reader.IsDBNull(0) ? null : reader.GetString(0),
                            Tags = reader.IsDBNull(1) ? null : reader.GetString(1),
                            Comment = reader.IsDBNull(2) ? null : reader.GetString(2)
                        };
                    }
                }
            }
            return null; // Not found
        }

        public static void SaveFolderMetadata(FolderMetadata metadata)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO FolderMetadata (Path, Color, Tags, Comment)
                    VALUES (@Path, @Color, @Tags, @Comment)
                    ON CONFLICT(Path) DO UPDATE SET
                        Color = excluded.Color,
                        Tags = excluded.Tags,
                        Comment = excluded.Comment;";

                command.Parameters.AddWithValue("@Path", metadata.Path);
                command.Parameters.AddWithValue("@Color", (object)metadata.Color ?? DBNull.Value);
                command.Parameters.AddWithValue("@Tags", (object)metadata.Tags ?? DBNull.Value);
                command.Parameters.AddWithValue("@Comment", (object)metadata.Comment ?? DBNull.Value);

                command.ExecuteNonQuery();
            }
        }

        // --- TodoItem Methods ---

        public static System.Collections.Generic.List<TodoItem> GetTodosForFolder(string path)
        {
            var todos = new System.Collections.Generic.List<TodoItem>();
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Task, IsCompleted FROM Todos WHERE FolderPath = @FolderPath";
                command.Parameters.AddWithValue("@FolderPath", path);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        todos.Add(new TodoItem
                        {
                            Id = reader.GetInt64(0),
                            FolderPath = path,
                            Task = reader.GetString(1),
                            IsCompleted = reader.GetInt32(2) == 1
                        });
                    }
                }
            }
            return todos;
        }

        public static void AddTodo(TodoItem todo)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Todos (FolderPath, Task, IsCompleted) VALUES (@FolderPath, @Task, @IsCompleted)";
                command.Parameters.AddWithValue("@FolderPath", todo.FolderPath);
                command.Parameters.AddWithValue("@Task", todo.Task);
                command.Parameters.AddWithValue("@IsCompleted", todo.IsCompleted ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }

        public static void UpdateTodo(TodoItem todo)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Todos SET Task = @Task, IsCompleted = @IsCompleted WHERE Id = @Id";
                command.Parameters.AddWithValue("@Task", todo.Task);
                command.Parameters.AddWithValue("@IsCompleted", todo.IsCompleted ? 1 : 0);
                command.Parameters.AddWithValue("@Id", todo.Id);
                command.ExecuteNonQuery();
            }
        }

        public static void DeleteTodo(long id)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Todos WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }
    }
}
