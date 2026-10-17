using STLHub.Models;

namespace STLHub.Services;

/// <summary>
/// Outcome of importing one 3D file.
/// </summary>
/// <param name="Object">The new object, the already-catalogued object when <paramref name="IsDuplicate"/> is set, or <c>null</c> when the source file no longer exists.</param>
/// <param name="IsDuplicate">True when a file with the same hash was already in the library and nothing was imported.</param>
public sealed record FileImportResult(Object3D? Object, bool IsDuplicate);

/// <summary>
/// Running or final totals of a recursive folder import.
/// </summary>
/// <param name="ObjectsImported">New 3D objects added to the library.</param>
/// <param name="AttachmentsImported">Non-3D files attached to the new objects.</param>
/// <param name="DuplicatesSkipped">3D files skipped because their hash was already catalogued.</param>
/// <param name="CreatedCategoryId">The first category the import created, used to select it afterwards.</param>
public sealed record FolderImportResult(int ObjectsImported, int AttachmentsImported, int DuplicatesSkipped, int? CreatedCategoryId);
