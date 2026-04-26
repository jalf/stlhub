using Dapper;
using Microsoft.Data.Sqlite;

namespace STLHub.Data;

/// <summary>
/// Creates and initializes the SQLite database schema including tables,
/// FTS5 full-text search index, and triggers for FTS synchronization.
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string dbPath)
    {
        _connectionString = $"Data Source={dbPath};";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createCategoryTable = @"
            CREATE TABLE IF NOT EXISTS Category (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                ParentCategoryId INTEGER,
                Path TEXT,
                SortOrder INTEGER DEFAULT 0
            );";
        
        var createObject3DTable = @"
            CREATE TABLE IF NOT EXISTS Object3D (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                MainFilePath TEXT NOT NULL,
                FileType TEXT NOT NULL,
                ThumbnailPath TEXT,
                Hash TEXT NOT NULL,
                CategoryId INTEGER,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(CategoryId) REFERENCES Category(Id)
            );";

        var createObjectFtsTable = @"
            CREATE VIRTUAL TABLE IF NOT EXISTS Object3D_FTS USING fts5(
                Name,
                Description,
                content='Object3D',
                content_rowid='Id'
            );";

        var createTriggers = @"
            CREATE TRIGGER IF NOT EXISTS Object3D_ai AFTER INSERT ON Object3D BEGIN
                INSERT INTO Object3D_FTS(rowid, Name, Description) VALUES (new.Id, new.Name, new.Description);
            END;
            CREATE TRIGGER IF NOT EXISTS Object3D_ad AFTER DELETE ON Object3D BEGIN
                INSERT INTO Object3D_FTS(Object3D_FTS, rowid, Name, Description) VALUES('delete', old.Id, old.Name, old.Description);
            END;
            CREATE TRIGGER IF NOT EXISTS Object3D_au AFTER UPDATE ON Object3D BEGIN
                INSERT INTO Object3D_FTS(Object3D_FTS, rowid, Name, Description) VALUES('delete', old.Id, old.Name, old.Description);
                INSERT INTO Object3D_FTS(rowid, Name, Description) VALUES (new.Id, new.Name, new.Description);
            END;
        ";

        var createTagTable = @"
            CREATE TABLE IF NOT EXISTS Tag (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );";

        var createObjectTagTable = @"
            CREATE TABLE IF NOT EXISTS ObjectTag (
                ObjectId INTEGER NOT NULL,
                TagId INTEGER NOT NULL,
                PRIMARY KEY (ObjectId, TagId),
                FOREIGN KEY(ObjectId) REFERENCES Object3D(Id) ON DELETE CASCADE,
                FOREIGN KEY(TagId) REFERENCES Tag(Id) ON DELETE CASCADE
            );";

        var createAttachmentTable = @"
            CREATE TABLE IF NOT EXISTS Attachment (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ObjectId INTEGER NOT NULL,
                FilePath TEXT NOT NULL,
                Type TEXT NOT NULL,
                FOREIGN KEY(ObjectId) REFERENCES Object3D(Id) ON DELETE CASCADE
            );";

        connection.Execute(createCategoryTable);
        connection.Execute(createObject3DTable);
        connection.Execute(createObjectFtsTable);
        connection.Execute(createTriggers);
        connection.Execute(createTagTable);
        connection.Execute(createObjectTagTable);
        connection.Execute(createAttachmentTable);
    }
}
