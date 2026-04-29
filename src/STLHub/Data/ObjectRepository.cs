using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using STLHub.Models;

namespace STLHub.Data;

/// <summary>
/// Data access layer for all database operations. Uses Dapper for lightweight
/// ORM mapping against the SQLite database.
/// Paths are stored as relative to <paramref name="repoPath"/> and resolved to absolute on read.
/// </summary>
public class ObjectRepository
{
    private readonly string _connectionString;
    private readonly string _repoPath;

    public ObjectRepository(string dbPath, string repoPath)
    {
        _connectionString = $"Data Source={dbPath};";
        _repoPath = repoPath;
    }

    /// <summary>Converts an absolute path inside the repo to a repo-relative path.</summary>
    private string MakeRelative(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath) || !Path.IsPathRooted(absolutePath))
            return absolutePath;
        string prefix = _repoPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        + Path.DirectorySeparatorChar;
        if (absolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return absolutePath.Substring(prefix.Length);
        return absolutePath;
    }

    /// <summary>Resolves a repo-relative path to an absolute path.</summary>
    private string MakeAbsolute(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) || Path.IsPathRooted(relativePath))
            return relativePath;
        return Path.GetFullPath(Path.Combine(_repoPath, relativePath));
    }

    private void ResolvePaths(Object3D obj)
    {
        obj.RelativeFilePath = obj.MainFilePath;
        obj.MainFilePath = MakeAbsolute(obj.MainFilePath);
        if (!string.IsNullOrEmpty(obj.ThumbnailPath))
            obj.ThumbnailPath = MakeAbsolute(obj.ThumbnailPath);
    }

    private void ResolvePaths(Attachment att)
    {
        att.FilePath = MakeAbsolute(att.FilePath);
    }

    public void AddObject(Object3D obj)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = @"
            INSERT INTO Object3D (Name, Description, MainFilePath, FileType, ThumbnailPath, Hash, CategoryId, CreatedAt)
            VALUES (@Name, @Description, @MainFilePath, @FileType, @ThumbnailPath, @Hash, @CategoryId, @CreatedAt);
            SELECT last_insert_rowid();";

        obj.Id = connection.QuerySingle<int>(sql, new
        {
            obj.Name, obj.Description,
            MainFilePath = MakeRelative(obj.MainFilePath),
            obj.FileType,
            ThumbnailPath = MakeRelative(obj.ThumbnailPath),
            obj.Hash, obj.CategoryId, obj.CreatedAt
        });
    }

    public void UpdateObject(Object3D obj)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = @"
            UPDATE Object3D 
            SET Name = @Name, Description = @Description
            WHERE Id = @Id;";
        
        connection.Execute(sql, obj);
    }

    public void AddAttachment(Attachment attachment)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = @"
            INSERT INTO Attachment (ObjectId, FilePath, Type)
            VALUES (@ObjectId, @FilePath, @Type);
            SELECT last_insert_rowid();";

        attachment.Id = connection.QuerySingle<int>(sql, new
        {
            attachment.ObjectId,
            FilePath = MakeRelative(attachment.FilePath),
            attachment.Type
        });
    }

    public IEnumerable<Attachment> GetAttachments(int objectId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var results = connection.Query<Attachment>(
            "SELECT * FROM Attachment WHERE ObjectId = @ObjectId", new { ObjectId = objectId }).ToList();
        foreach (var att in results) ResolvePaths(att);
        return results;
    }

    public void DeleteAttachment(int attachmentId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute("DELETE FROM Attachment WHERE Id = @Id", new { Id = attachmentId });
    }

    public int CountAttachmentsByFilePath(string filePath)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.QuerySingle<int>(
            "SELECT COUNT(*) FROM Attachment WHERE FilePath = @FilePath",
            new { FilePath = MakeRelative(filePath) });
    }

    public Tag AddOrGetTag(string name)
    {
        using var connection = new SqliteConnection(_connectionString);
        name = name.Trim().ToLower();
        
        var existing = connection.QueryFirstOrDefault<Tag>("SELECT * FROM Tag WHERE Name = @Name", new { Name = name });
        if (existing != null) return existing;

        var sql = @"
            INSERT INTO Tag (Name) VALUES (@Name);
            SELECT last_insert_rowid();";
        int id = connection.QuerySingle<int>(sql, new { Name = name });
        return new Tag { Id = id, Name = name };
    }

    public IEnumerable<Tag> GetTagsForObject(int objectId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = @"
            SELECT t.* FROM Tag t
            INNER JOIN ObjectTag ot ON t.Id = ot.TagId
            WHERE ot.ObjectId = @ObjectId";
        return connection.Query<Tag>(sql, new { ObjectId = objectId });
    }

    public void AddTagToObject(int objectId, int tagId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = "INSERT OR IGNORE INTO ObjectTag (ObjectId, TagId) VALUES (@ObjectId, @TagId)";
        connection.Execute(sql, new { ObjectId = objectId, TagId = tagId });
    }

    public void RemoveTagFromObject(int objectId, int tagId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = "DELETE FROM ObjectTag WHERE ObjectId = @ObjectId AND TagId = @TagId";
        connection.Execute(sql, new { ObjectId = objectId, TagId = tagId });
    }

    public IEnumerable<Tag> GetAllTags()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Query<Tag>("SELECT * FROM Tag ORDER BY Name");
    }

    public void DeleteTag(int tagId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute("DELETE FROM ObjectTag WHERE TagId = @Id", new { Id = tagId });
        connection.Execute("DELETE FROM Tag WHERE Id = @Id", new { Id = tagId });
    }

    public IEnumerable<Category> GetAllCategories()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Query<Category>("SELECT * FROM Category ORDER BY SortOrder, Name");
    }

    public void AddCategory(Category category)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = @"
            INSERT INTO Category (Name, ParentCategoryId, Path, SortOrder)
            VALUES (@Name, @ParentCategoryId, @Path, @SortOrder);
            SELECT last_insert_rowid();";
        category.Id = connection.QuerySingle<int>(sql, category);
    }

    public void UpdateCategory(Category category)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = @"
            UPDATE Category 
            SET Name = @Name, ParentCategoryId = @ParentCategoryId, Path = @Path, SortOrder = @SortOrder
            WHERE Id = @Id;";
        connection.Execute(sql, category);
    }

    public int CountObjectsInCategory(int categoryId)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.QuerySingle<int>(
            "SELECT COUNT(*) FROM Object3D WHERE CategoryId = @Id", new { Id = categoryId });
    }

    public Object3D? GetObjectByHash(string hash)
    {
        using var connection = new SqliteConnection(_connectionString);
        var obj = connection.QueryFirstOrDefault<Object3D>(
            "SELECT * FROM Object3D WHERE Hash = @Hash", new { Hash = hash });
        if (obj != null) ResolvePaths(obj);
        return obj;
    }

    public void DeleteObject(int objectId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute("DELETE FROM ObjectTag WHERE ObjectId = @Id", new { Id = objectId });
        connection.Execute("DELETE FROM Attachment WHERE ObjectId = @Id", new { Id = objectId });
        connection.Execute("DELETE FROM Object3D WHERE Id = @Id", new { Id = objectId });
    }

    public void UpdateObjectThumbnail(int objectId, string thumbnailPath)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute("UPDATE Object3D SET ThumbnailPath = @ThumbnailPath WHERE Id = @Id",
            new { Id = objectId, ThumbnailPath = MakeRelative(thumbnailPath) });
    }

    public void DeleteCategory(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        DeleteCategoryRecursive(connection, transaction, id);
        transaction.Commit();
    }

    private void DeleteCategoryRecursive(SqliteConnection connection, SqliteTransaction transaction, int id)
    {
        var childIds = connection.Query<int>(
            "SELECT Id FROM Category WHERE ParentCategoryId = @Id", new { Id = id }, transaction);
        foreach (var childId in childIds)
            DeleteCategoryRecursive(connection, transaction, childId);

        connection.Execute("UPDATE Object3D SET CategoryId = NULL WHERE CategoryId = @Id", new { Id = id }, transaction);
        connection.Execute("DELETE FROM Category WHERE Id = @Id", new { Id = id }, transaction);
    }

    public void UpdateObjectCategory(int objectId, int? categoryId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute("UPDATE Object3D SET CategoryId = @CategoryId WHERE Id = @ObjectId", new { CategoryId = categoryId, ObjectId = objectId });
    }

    public IEnumerable<Object3D> GetAllObjects(int? categoryId = null, int? tagId = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        List<Object3D> results;
        if (tagId.HasValue && categoryId.HasValue)
        {
            results = connection.Query<Object3D>(
                @"SELECT o.* FROM Object3D o
                  INNER JOIN ObjectTag ot ON o.Id = ot.ObjectId
                  WHERE o.CategoryId = @CategoryId AND ot.TagId = @TagId
                  ORDER BY o.CreatedAt DESC",
                new { CategoryId = categoryId.Value, TagId = tagId.Value }).ToList();
        }
        else if (tagId.HasValue)
        {
            results = connection.Query<Object3D>(
                @"SELECT o.* FROM Object3D o
                  INNER JOIN ObjectTag ot ON o.Id = ot.ObjectId
                  WHERE ot.TagId = @TagId
                  ORDER BY o.CreatedAt DESC",
                new { TagId = tagId.Value }).ToList();
        }
        else if (categoryId.HasValue)
        {
            results = connection.Query<Object3D>(
                "SELECT * FROM Object3D WHERE CategoryId = @CategoryId ORDER BY CreatedAt DESC",
                new { CategoryId = categoryId.Value }).ToList();
        }
        else
        {
            results = connection.Query<Object3D>("SELECT * FROM Object3D ORDER BY CreatedAt DESC").ToList();
        }
        foreach (var obj in results) ResolvePaths(obj);
        return results;
    }

    public IEnumerable<Object3D> SearchObjects(string searchTerm, int? categoryId = null, int? tagId = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return GetAllObjects(categoryId, tagId);

        using var connection = new SqliteConnection(_connectionString);
        string term = "\"" + searchTerm.Replace("\"", "\"\"") + "\"*";
        List<Object3D> results;

        if (tagId.HasValue && categoryId.HasValue)
        {
            results = connection.Query<Object3D>(@"
                SELECT o.*
                FROM Object3D o
                JOIN Object3D_FTS f ON o.Id = f.rowid
                INNER JOIN ObjectTag ot ON o.Id = ot.ObjectId
                WHERE Object3D_FTS MATCH @Term AND o.CategoryId = @CategoryId AND ot.TagId = @TagId
                ORDER BY rank;",
                new { Term = term, CategoryId = categoryId.Value, TagId = tagId.Value }).ToList();
        }
        else if (tagId.HasValue)
        {
            results = connection.Query<Object3D>(@"
                SELECT o.*
                FROM Object3D o
                JOIN Object3D_FTS f ON o.Id = f.rowid
                INNER JOIN ObjectTag ot ON o.Id = ot.ObjectId
                WHERE Object3D_FTS MATCH @Term AND ot.TagId = @TagId
                ORDER BY rank;",
                new { Term = term, TagId = tagId.Value }).ToList();
        }
        else if (categoryId.HasValue)
        {
            results = connection.Query<Object3D>(@"
                SELECT o.*
                FROM Object3D o
                JOIN Object3D_FTS f ON o.Id = f.rowid
                WHERE Object3D_FTS MATCH @Term AND o.CategoryId = @CategoryId
                ORDER BY rank;",
                new { Term = term, CategoryId = categoryId.Value }).ToList();
        }
        else
        {
            results = connection.Query<Object3D>(@"
                SELECT o.*
                FROM Object3D o
                JOIN Object3D_FTS f ON o.Id = f.rowid
                WHERE Object3D_FTS MATCH @Term
                ORDER BY rank;",
                new { Term = term }).ToList();
        }
        foreach (var obj in results) ResolvePaths(obj);
        return results;
    }
}
