using System.Collections.Generic;
using Dapper;
using Microsoft.Data.Sqlite;
using STLHub.Models;

namespace STLHub.Data;

/// <summary>
/// Data access layer for all database operations. Uses Dapper for lightweight
/// ORM mapping against the SQLite database.
/// </summary>
public class ObjectRepository
{
    private readonly string _connectionString;

    public ObjectRepository(string dbPath)
    {
        _connectionString = $"Data Source={dbPath};";
    }

    public void AddObject(Object3D obj)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = @"
            INSERT INTO Object3D (Name, Description, MainFilePath, FileType, ThumbnailPath, Hash, CategoryId, CreatedAt)
            VALUES (@Name, @Description, @MainFilePath, @FileType, @ThumbnailPath, @Hash, @CategoryId, @CreatedAt);
            SELECT last_insert_rowid();";
        
        obj.Id = connection.QuerySingle<int>(sql, obj);
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
        
        attachment.Id = connection.QuerySingle<int>(sql, attachment);
    }

    public IEnumerable<Attachment> GetAttachments(int objectId)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Query<Attachment>("SELECT * FROM Attachment WHERE ObjectId = @ObjectId", new { ObjectId = objectId });
    }

    public void DeleteAttachment(int attachmentId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute("DELETE FROM Attachment WHERE Id = @Id", new { Id = attachmentId });
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
        return connection.QueryFirstOrDefault<Object3D>(
            "SELECT * FROM Object3D WHERE Hash = @Hash", new { Hash = hash });
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
            new { Id = objectId, ThumbnailPath = thumbnailPath });
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
        if (tagId.HasValue && categoryId.HasValue)
        {
            return connection.Query<Object3D>(
                @"SELECT o.* FROM Object3D o
                  INNER JOIN ObjectTag ot ON o.Id = ot.ObjectId
                  WHERE o.CategoryId = @CategoryId AND ot.TagId = @TagId
                  ORDER BY o.CreatedAt DESC",
                new { CategoryId = categoryId.Value, TagId = tagId.Value });
        }
        if (tagId.HasValue)
        {
            return connection.Query<Object3D>(
                @"SELECT o.* FROM Object3D o
                  INNER JOIN ObjectTag ot ON o.Id = ot.ObjectId
                  WHERE ot.TagId = @TagId
                  ORDER BY o.CreatedAt DESC",
                new { TagId = tagId.Value });
        }
        if (categoryId.HasValue)
        {
            return connection.Query<Object3D>("SELECT * FROM Object3D WHERE CategoryId = @CategoryId ORDER BY CreatedAt DESC", new { CategoryId = categoryId.Value });
        }
        return connection.Query<Object3D>("SELECT * FROM Object3D ORDER BY CreatedAt DESC");
    }

    public IEnumerable<Object3D> SearchObjects(string searchTerm, int? categoryId = null, int? tagId = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return GetAllObjects(categoryId, tagId);

        using var connection = new SqliteConnection(_connectionString);
        string term = "\"" + searchTerm.Replace("\"", "\"\"") + "\"*";

        if (tagId.HasValue && categoryId.HasValue)
        {
            var sql = @"
                SELECT o.* 
                FROM Object3D o
                JOIN Object3D_FTS f ON o.Id = f.rowid
                INNER JOIN ObjectTag ot ON o.Id = ot.ObjectId
                WHERE Object3D_FTS MATCH @Term AND o.CategoryId = @CategoryId AND ot.TagId = @TagId
                ORDER BY rank;";
            return connection.Query<Object3D>(sql, new { Term = term, CategoryId = categoryId.Value, TagId = tagId.Value });
        }
        if (tagId.HasValue)
        {
            var sql = @"
                SELECT o.* 
                FROM Object3D o
                JOIN Object3D_FTS f ON o.Id = f.rowid
                INNER JOIN ObjectTag ot ON o.Id = ot.ObjectId
                WHERE Object3D_FTS MATCH @Term AND ot.TagId = @TagId
                ORDER BY rank;";
            return connection.Query<Object3D>(sql, new { Term = term, TagId = tagId.Value });
        }
        if (categoryId.HasValue)
        {
            var sql = @"
                SELECT o.* 
                FROM Object3D o
                JOIN Object3D_FTS f ON o.Id = f.rowid
                WHERE Object3D_FTS MATCH @Term AND o.CategoryId = @CategoryId
                ORDER BY rank;";
            return connection.Query<Object3D>(sql, new { Term = term, CategoryId = categoryId.Value });
        }
        else
        {
            var sql = @"
                SELECT o.* 
                FROM Object3D o
                JOIN Object3D_FTS f ON o.Id = f.rowid
                WHERE Object3D_FTS MATCH @Term
                ORDER BY rank;";
            return connection.Query<Object3D>(sql, new { Term = term });
        }
    }
}
