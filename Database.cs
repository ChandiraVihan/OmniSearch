using System;
using System.Data.SQLite;
using System.IO;

public static class Database
{
    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OmniSearch",
        "omniseach.db"
    );

    private static readonly string ConnectionString = $"Data Source={DbPath};Version=3;";

    static Database()
    {
        var dir = Path.GetDirectoryName(DbPath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public static SQLiteConnection GetConnection()
    {
        var connection = new SQLiteConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public static void Initialize()
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Files (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Path TEXT NOT NULL UNIQUE,
                Type TEXT,
                DateModified INTEGER
            );

            CREATE INDEX IF NOT EXISTS idx_files_name ON Files (Name);
        ";

        command.ExecuteNonQuery();
    }
}
