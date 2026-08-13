using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using STLHub.Models;

namespace STLHub.Services;

/// <summary>
/// Reads project metadata embedded in <c>.3mf</c> archives.
/// Extraction is best-effort and never throws: any malformed or unreadable file
/// simply yields <c>null</c> so the Details panel can render without an error state.
/// </summary>
public static class ThreeMfMetadataReader
{
    private const string ModelEntryName = "3D/3dmodel.model";
    private const string BambuConfigEntryName = "Metadata/project_settings.config";
    private const string PrusaConfigEntryName = "Metadata/Slic3r_PE.config";

    /// <summary>
    /// Extracts project metadata from the 3MF archive at <paramref name="filePath"/>.
    /// Returns <c>null</c> when the file is missing, corrupt, or contains no recognized metadata.
    /// </summary>
    public static async Task<ThreeMfProjectInfo?> ReadAsync(string filePath, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(filePath)) return null;

            using var archive = ZipFile.OpenRead(filePath);
            ct.ThrowIfCancellationRequested();

            var info = await ReadStandardMetadataAsync(archive, ct);
            info = await ReadProfileAsync(archive, info, ct);

            return info.HasAnyMetadata ? info : null;
        }
        catch
        {
            // FR-007: malformed archives are indistinguishable from "no metadata".
            return null;
        }
    }

    /// <summary>
    /// Reads the model-level metadata block from <c>3D/3dmodel.model</c>.
    /// </summary>
    private static async Task<ThreeMfProjectInfo> ReadStandardMetadataAsync(ZipArchive archive, CancellationToken ct)
    {
        var entry = FindEntry(archive, ModelEntryName);
        if (entry is null) return new ThreeMfProjectInfo();

        await using var stream = entry.Open();
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Model-level metadata are direct children of <model>; per-object metadata
        // living deeper in the tree is deliberately ignored.
        foreach (var element in doc.Root?.Elements() ?? [])
        {
            if (!element.Name.LocalName.Equals("metadata", StringComparison.Ordinal)) continue;

            var name = element.Attribute("name")?.Value;
            if (!string.IsNullOrWhiteSpace(name) && !values.ContainsKey(name))
                values[name] = element.Value;
        }

        return new ThreeMfProjectInfo
        {
            Title = Normalize(values.GetValueOrDefault("Title")),
            Designer = Normalize(values.GetValueOrDefault("Designer")),
            Description = Normalize(values.GetValueOrDefault("Description")),
            CreationDate = Normalize(values.GetValueOrDefault("CreationDate")),
        };
    }

    /// <summary>
    /// Detects the producing slicer by probing for its configuration entry and merges the
    /// print profile fields into <paramref name="info"/>. Profile data only supplements the
    /// identity fields — the two sources never cover the same field. A broken profile file
    /// leaves the already-extracted identity metadata intact.
    /// </summary>
    private static async Task<ThreeMfProjectInfo> ReadProfileAsync(
        ZipArchive archive, ThreeMfProjectInfo info, CancellationToken ct)
    {
        try
        {
            if (FindEntry(archive, BambuConfigEntryName) is { } bambuEntry)
                return await ParseBambuJsonAsync(bambuEntry, info, ct);

            if (FindEntry(archive, PrusaConfigEntryName) is { } prusaEntry)
                return await ParsePrusaIniAsync(prusaEntry, info, ct);
        }
        catch
        {
            // Ignore: keep whatever identity metadata was already read.
        }

        return info;
    }

    /// <summary>
    /// Parses the Bambu Studio / Orca Slicer <c>project_settings.config</c> JSON profile.
    /// </summary>
    private static async Task<ThreeMfProjectInfo> ParseBambuJsonAsync(
        ZipArchiveEntry entry, ThreeMfProjectInfo info, CancellationToken ct)
    {
        await using var stream = entry.Open();
        using var doc = await JsonDocument.ParseAsync(stream, default, ct);

        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return info;

        return info with
        {
            ProfileName = GetJsonValue(root, "print_settings_id"),
            LayerHeight = FormatMillimetres(GetJsonValue(root, "layer_height")),
            WallCount = GetJsonValue(root, "wall_loops"),
            InfillPercent = FormatPercent(GetJsonValue(root, "sparse_infill_density")),
            // Neither Bambu Studio nor Orca Slicer records a profile author in this file.
        };
    }

    /// <summary>
    /// Parses the PrusaSlicer <c>Slic3r_PE.config</c> INI profile (flat <c>key = value</c> lines).
    /// </summary>
    private static async Task<ThreeMfProjectInfo> ParsePrusaIniAsync(
        ZipArchiveEntry entry, ThreeMfProjectInfo info, CancellationToken ct)
    {
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            var trimmed = line.AsSpan().Trim();
            if (trimmed.IsEmpty || trimmed[0] is '#' or ';' or '[') continue;

            var separator = trimmed.IndexOf('=');
            if (separator <= 0) continue;

            var key = trimmed[..separator].Trim().ToString();
            if (!values.ContainsKey(key))
                values[key] = trimmed[(separator + 1)..].Trim().ToString();
        }

        return info with
        {
            ProfileName = Normalize(values.GetValueOrDefault("print_settings_id")),
            LayerHeight = FormatMillimetres(Normalize(values.GetValueOrDefault("layer_height"))),
            WallCount = Normalize(values.GetValueOrDefault("perimeters")),
            InfillPercent = FormatPercent(Normalize(values.GetValueOrDefault("fill_density"))),
            // PrusaSlicer does not record a profile author in this file.
        };
    }

    /// <summary>
    /// Reads a scalar profile value from the JSON object. Orca Slicer stores some settings
    /// as single-element arrays, so the first element is unwrapped when needed.
    /// </summary>
    private static string? GetJsonValue(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var element)) return null;

        if (element.ValueKind == JsonValueKind.Array)
            element = element.GetArrayLength() > 0 ? element[0] : default;

        return element.ValueKind switch
        {
            JsonValueKind.String => Normalize(element.GetString()),
            JsonValueKind.Number => Normalize(element.GetRawText()),
            _ => null
        };
    }

    /// <summary>
    /// Locates a ZIP entry by full name, tolerating case and separator differences between producers.
    /// </summary>
    private static ZipArchiveEntry? FindEntry(ZipArchive archive, string fullName) =>
        archive.Entries.FirstOrDefault(e =>
            e.FullName.Replace('\\', '/').Equals(fullName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Trims a raw value, decodes its HTML entities and collapses blank results to <c>null</c>.
    /// Producers escape metadata to varying depths, so decoding here keeps escaped apostrophes
    /// and markup from reaching the UI as literal entity text.
    /// </summary>
    private static string? Normalize(string? value)
    {
        if (value is null) return null;

        var decoded = HtmlText.Decode(value).Trim();
        return string.IsNullOrEmpty(decoded) ? null : decoded;
    }

    /// <summary>Appends the <c>mm</c> unit unless the slicer already wrote one.</summary>
    private static string? FormatMillimetres(string? value) =>
        value is null || value.Contains("mm", StringComparison.OrdinalIgnoreCase) ? value : $"{value} mm";

    /// <summary>Appends the percent sign unless the slicer already wrote one.</summary>
    private static string? FormatPercent(string? value) =>
        value is null || value.EndsWith('%') ? value : $"{value}%";
}
