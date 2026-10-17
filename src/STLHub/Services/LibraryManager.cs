using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using STLHub.Data;
using STLHub.Models;

namespace STLHub.Services;

/// <summary>
/// Core service for managing the 3D object library. Handles importing files and folders,
/// managing attachments, deleting objects, and thumbnail regeneration.
/// </summary>
public partial class LibraryManager
{
    /// <summary>File extensions treated as importable 3D models.</summary>
    public static readonly HashSet<string> Object3DExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".stl", ".3mf", ".obj", ".step", ".stp" };

    private readonly string _libraryPath;
    private readonly ObjectRepository _repository;
    private readonly string _thumbnailsPath;
    private readonly string _attachmentsPath;

    public LibraryManager(string libraryPath, ObjectRepository repository)
    {
        _libraryPath = libraryPath;
        _repository = repository;
        _thumbnailsPath = Path.Combine(libraryPath, "Thumbnails");
        _attachmentsPath = Path.Combine(libraryPath, "Attachments");

        Directory.CreateDirectory(_libraryPath);
        Directory.CreateDirectory(_thumbnailsPath);
    }

    /// <summary>
    /// Imports a single 3D file into the library. A file whose hash is already catalogued is
    /// not imported again; the result then carries the existing object and
    /// <see cref="FileImportResult.IsDuplicate"/> set.
    /// </summary>
    public FileImportResult ImportFile(string sourceFilePath, int? categoryId = null)
    {
        if (!File.Exists(sourceFilePath)) return new FileImportResult(null, false);

        string hash = CalculateHash(sourceFilePath);
        var existing = _repository.GetObjectByHash(hash);
        if (existing != null) return new FileImportResult(existing, true);

        return new FileImportResult(ImportNewFile(sourceFilePath, hash, categoryId), false);
    }

    /// <summary>Copies a file already known not to be catalogued and registers it.</summary>
    private Object3D ImportNewFile(string sourceFilePath, string hash, int? categoryId)
    {
        var fileInfo = new FileInfo(sourceFilePath);

        string destinationFileName = $"{hash}{fileInfo.Extension}";
        string destinationFilePath = Path.Combine(_libraryPath, destinationFileName);

        if (!File.Exists(destinationFilePath))
        {
            File.Copy(sourceFilePath, destinationFilePath);
        }

        string thumbnailPath = ThumbnailGenerator.GenerateThumbnail(destinationFilePath, _thumbnailsPath);

        var newObject = new Object3D
        {
            Name = SanitizeName(Path.GetFileNameWithoutExtension(fileInfo.Name)),
            MainFilePath = destinationFilePath,
            FileType = fileInfo.Extension.ToLower(),
            ThumbnailPath = thumbnailPath,
            Hash = hash,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };

        _repository.AddObject(newObject);
        return newObject;
    }

    /// <summary>
    /// Imports a folder recursively: sub-folders become categories, 3D files become objects,
    /// and remaining files are attached to the 3D objects in the same folder. Files already in
    /// the library are skipped and counted, and a folder whose 3D files are all duplicates
    /// creates no category.
    /// </summary>
    public FolderImportResult ImportFolder(
        string folderPath, int? parentCategoryId,
        Action<string>? onProgress = null,
        Action<FolderImportResult>? onCounts = null,
        CancellationToken cancellationToken = default)
    {
        var counts = new FolderImportCounter();
        ImportFolderRecursive(folderPath, parentCategoryId, onProgress, onCounts, counts, cancellationToken);
        return counts.ToResult();
    }

    /// <summary>Mutable running totals shared across the recursive folder walk.</summary>
    private sealed class FolderImportCounter
    {
        public int Objects;
        public int Attachments;
        public int Duplicates;
        public int? FirstCreatedCategoryId;

        public FolderImportResult ToResult() => new(Objects, Attachments, Duplicates, FirstCreatedCategoryId);
    }

    private void ImportFolderRecursive(string folderPath, int? parentCategoryId,
        Action<string>? onProgress, Action<FolderImportResult>? onCounts,
        FolderImportCounter counts,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Separate files into 3D objects and potential attachments (single pass)
        var allFiles = Directory.GetFiles(folderPath);
        var object3DFiles = new List<string>(allFiles.Length);
        var otherFiles = new List<string>(allFiles.Length);
        foreach (var f in allFiles)
            (Object3DExtensions.Contains(Path.GetExtension(f)) ? object3DFiles : otherFiles).Add(f);

        // The folder's category is created on its first new 3D file, so a folder that holds
        // only duplicates (or no 3D files at all) never leaves an empty category behind.
        int? categoryId = null;
        var importedObjects = new List<Object3D>();
        foreach (var file in object3DFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onProgress?.Invoke(Path.GetFileName(file));

            string hash = CalculateHash(file);
            if (_repository.GetObjectByHash(hash) != null)
            {
                counts.Duplicates++;
                onCounts?.Invoke(counts.ToResult());
                continue;
            }

            if (categoryId == null)
            {
                var category = new Category
                {
                    Name = SanitizeName(Path.GetFileName(folderPath)),
                    ParentCategoryId = parentCategoryId
                };
                _repository.AddCategory(category);
                categoryId = category.Id;
                counts.FirstCreatedCategoryId ??= category.Id;
            }

            importedObjects.Add(ImportNewFile(file, hash, categoryId));
            counts.Objects++;
            onCounts?.Invoke(counts.ToResult());
        }

        // Attach other files only to the objects imported now; duplicates already have theirs.
        if (importedObjects.Count > 0 && otherFiles.Count > 0)
        {
            foreach (var file in otherFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                onProgress?.Invoke(Path.GetFileName(file));
                foreach (var obj in importedObjects)
                {
                    ImportAttachment(obj.Id, file);
                }
                counts.Attachments++;
                onCounts?.Invoke(counts.ToResult());
            }
        }

        // Recurse into sub-folders (use created category or keep parent)
        foreach (var subDir in Directory.GetDirectories(folderPath))
        {
            ImportFolderRecursive(subDir, categoryId ?? parentCategoryId, onProgress, onCounts,
                counts, cancellationToken);
        }
    }

    public Attachment? ImportAttachment(int objectId, string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath)) return null;

        Directory.CreateDirectory(_attachmentsPath);

        var fileInfo = new FileInfo(sourceFilePath);
        string hash = CalculateHash(sourceFilePath);

        string destinationFileName = $"{hash}{fileInfo.Extension}";
        string destinationFilePath = Path.Combine(_attachmentsPath, destinationFileName);

        if (!File.Exists(destinationFilePath))
        {
            File.Copy(sourceFilePath, destinationFilePath);
        }

        var newAttachment = new Attachment
        {
            ObjectId = objectId,
            FilePath = destinationFilePath,
            Type = fileInfo.Extension.ToLower()
        };

        _repository.AddAttachment(newAttachment);
        return newAttachment;
    }

    public void DeleteAttachment(Attachment attachment)
    {
        _repository.DeleteAttachment(attachment.Id);

        // Only delete the physical file if no other attachment references it
        if (_repository.CountAttachmentsByFilePath(attachment.FilePath) == 0)
        {
            try
            {
                if (File.Exists(attachment.FilePath))
                {
                    File.Delete(attachment.FilePath);
                }
            }
            catch { /* ignored */ }
        }
    }

    public void DeleteObject(Object3D obj)
    {
        // Collect attachment file paths before removing from DB
        var attachments = _repository.GetAttachments(obj.Id).ToList();

        // Delete from database first (removes attachment records via cascade)
        _repository.DeleteObject(obj.Id);

        // Delete attachment files only if no other record references them
        foreach (var att in attachments)
        {
            if (_repository.CountAttachmentsByFilePath(att.FilePath) == 0)
            {
                try { if (File.Exists(att.FilePath)) File.Delete(att.FilePath); } catch { }
            }
        }

        // Delete thumbnail
        try { if (File.Exists(obj.ThumbnailPath)) File.Delete(obj.ThumbnailPath); } catch { }

        // Delete main file
        try { if (File.Exists(obj.MainFilePath)) File.Delete(obj.MainFilePath); } catch { }
    }

    public string RegenerateThumbnail(Object3D obj)
    {
        // Delete existing thumbnail
        try { if (File.Exists(obj.ThumbnailPath)) File.Delete(obj.ThumbnailPath); } catch { }

        // Generate new thumbnail
        string newPath = ThumbnailGenerator.GenerateThumbnail(obj.MainFilePath, _thumbnailsPath);
        _repository.UpdateObjectThumbnail(obj.Id, newPath);
        return newPath;
    }

    private static string CalculateHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    /// <summary>
    /// Turns a file or folder name into a display name: separators become spaces,
    /// camelCase is split, and each word is capitalized.
    /// </summary>
    private static string SanitizeName(string name)
    {
        name = SeparatorsRegex().Replace(name, " ");
        name = CamelCaseBoundaryRegex().Replace(name, " ");
        name = ExtraSpacesRegex().Replace(name, " ").Trim();
        return WordStartRegex().Replace(name, m => m.Value.ToUpper());
    }

    [GeneratedRegex(@"[_\-\.]+")]
    private static partial Regex SeparatorsRegex();

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])")]
    private static partial Regex CamelCaseBoundaryRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex ExtraSpacesRegex();

    [GeneratedRegex(@"\b(\w)")]
    private static partial Regex WordStartRegex();
}
