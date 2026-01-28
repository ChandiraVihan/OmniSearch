using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class IndexService
{
    public async Task BuildIndex()
    {
        await Task.Run(() =>
        {
            Database.Initialize();
            var pathsToIndex = GetPathsToIndex();

            using var connection = Database.GetConnection();
            using var transaction = connection.BeginTransaction();
            
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Files";
            command.ExecuteNonQuery();

            foreach (var path in pathsToIndex)
            {
                IndexDirectory(path, command);
            }

            transaction.Commit();
        });
    }

    private void IndexDirectory(string path, SQLiteCommand command)
    {
        try
        {
            foreach (var file in Directory.GetFiles(path))
            {
                InsertFile(command, file);
            }

            foreach (var directory in Directory.GetDirectories(path))
            {
                IndexDirectory(directory, command);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error indexing '{path}': {ex.Message}");
        }
    }

    private void InsertFile(SQLiteCommand command, string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);

            command.CommandText = @"
                INSERT OR IGNORE INTO Files (Name, Path, Type, DateModified)
                VALUES (@Name, @Path, @Type, @DateModified)";

            command.Parameters.AddWithValue("@Name", Path.GetFileName(filePath));
            command.Parameters.AddWithValue("@Path", filePath);
            command.Parameters.AddWithValue("@Type", Path.GetExtension(filePath));
            command.Parameters.AddWithValue("@DateModified", fileInfo.LastWriteTimeUtc.Ticks);

            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error inserting file '{filePath}': {ex.Message}");
        }
    }

    private IEnumerable<string> GetPathsToIndex()
    {
        var paths = new List<string>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            paths.Add(drive.Name);
        }
        return paths;
    }
}
