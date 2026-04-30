using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using IOPath = System.IO.Path;

namespace STLHub.Services;

/// <summary>
/// Generates or extracts thumbnails for 3D model files.
/// Supports embedded thumbnail extraction from 3MF archives and
/// software-rendered isometric previews for STL files.
/// </summary>
public static class ThumbnailGenerator
{
    private const int ThumbnailSize = 256;

    /// <summary>
    /// Generates or extracts a thumbnail for the specified 3D model file.
    /// Returns the output PNG path, or an empty string on failure.
    /// </summary>
    public static string GenerateThumbnail(string modelFilePath, string thumbnailsDir)
    {
        try
        {
            if (!File.Exists(modelFilePath)) return string.Empty;

            var ext = IOPath.GetExtension(modelFilePath).ToLower();
            var hash = IOPath.GetFileNameWithoutExtension(modelFilePath); // já é o hash
            var outPath = IOPath.Combine(thumbnailsDir, $"{hash}.png");

            if (File.Exists(outPath)) return outPath;

            if (!Directory.Exists(thumbnailsDir))
                Directory.CreateDirectory(thumbnailsDir);

            bool success = ext switch
            {
                ".3mf"              => Extract3mfThumbnail(modelFilePath, outPath),
                ".stl"              => RenderStlThumbnail(modelFilePath, outPath),
                ".step" or ".stp"   => RenderStepThumbnail(modelFilePath, outPath),
                _                   => false
            };

            if (!success)
            {
                GenerateFallbackThumbnail(ext, outPath);
            }

            return outPath;
        }
        catch
        {
            return string.Empty;
        }
    }

    // ──────────────────────────────────────────────────
    // 3MF — extracts the embedded thumbnail from the ZIP archive
    // ──────────────────────────────────────────────────
    private static bool Extract3mfThumbnail(string filePath, string outPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(filePath);

            // Prioridade por tamanho — queremos o maior thumbnail disponível.
            // Bambu Studio / Orca Slicer: Auxiliaries/.thumbnails/thumbnail_middle.png (maior resolução)
            // PrusaSlicer: Metadata/thumbnail.png
            // 3MF genérico: Thumbnails/thumbnail.png ou thumbnail.png
            var candidateOrder = new[]
            {
                "Auxiliaries/.thumbnails/thumbnail_middle.png",
                "Auxiliaries/.thumbnails/thumbnail_3mf.png",
                "Auxiliaries/.thumbnails/thumbnail_small.png",
                "Metadata/thumbnail.png",
                "Thumbnails/thumbnail.png",
                "thumbnail.png",
            };

            ZipArchiveEntry? entry = null;

            // 1. Verificar lista de candidatos conhecidos (case-insensitive)
            foreach (var candidate in candidateOrder)
            {
                entry = archive.Entries.FirstOrDefault(e =>
                    string.Equals(e.FullName, candidate, StringComparison.OrdinalIgnoreCase));
                if (entry != null) break;
            }

            // 2. Fallback: pegar o maior PNG no arquivo inteiro
            if (entry == null)
            {
                entry = archive.Entries
                    .Where(e => e.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .MaxBy(e => e.Length);
            }

            if (entry == null) return false;

            using var inputStream = entry.Open();
            // Ler para memória primeiro (ZipArchiveEntry não suporta seek)
            using var memStream = new MemoryStream();
            inputStream.CopyTo(memStream);
            memStream.Position = 0;

            using var img = Image.Load(memStream);
            img.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(ThumbnailSize, ThumbnailSize),
                Mode = ResizeMode.Pad,
                PadColor = Color.FromRgba(28, 28, 32, 255)
            }));
            img.SaveAsPng(outPath);
            return true;
        }
        catch { return false; }
    }

    // ──────────────────────────────────────────────────
    // STL — parser + simple isometric projection rendering
    // ──────────────────────────────────────────────────
    private static bool RenderStlThumbnail(string filePath, string outPath)
    {
        try
        {
            var triangles = ParseStl(filePath);
            if (triangles.Count == 0) return false;

            // Bounding box
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            foreach (var (v0, v1, v2, _) in triangles)
            {
                foreach (var v in new[] { v0, v1, v2 })
                {
                    if (v.x < minX) minX = v.x; if (v.x > maxX) maxX = v.x;
                    if (v.y < minY) minY = v.y; if (v.y > maxY) maxY = v.y;
                    if (v.z < minZ) minZ = v.z; if (v.z > maxZ) maxZ = v.z;
                }
            }

            float cx = (minX + maxX) / 2f;
            float cy = (minY + maxY) / 2f;
            float cz = (minZ + maxZ) / 2f;
            float scale = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
            if (scale < 1e-6f) return false;

            // Ângulos isométricos
            const float angleX = 0.6154797f; // ~35.26°
            const float angleY = 0.7853982f; // ~45°
            float cosY = MathF.Cos(angleY), sinY = MathF.Sin(angleY);
            float cosX = MathF.Cos(angleX), sinX = MathF.Sin(angleX);

            (float px, float py, float pz) Project(float x, float y, float z)
            {
                x -= cx; y -= cy; z -= cz;
                // Rotate Y
                float rx = x * cosY - z * sinY;
                float rz = x * sinY + z * cosY;
                // Rotate X
                float ry2 = y * cosX - rz * sinX;
                float rz2 = y * sinX + rz * cosX;
                return (rx, ry2, rz2);
            }

            int pad = 16;
            int drawSize = ThumbnailSize - pad * 2;
            float screenScale = drawSize / scale * 0.82f;

            // Projetar e ordenar (painter's algorithm — z médio)
            var projected = triangles.Select(t =>
            {
                var p0 = Project(t.v0.x, t.v0.y, t.v0.z);
                var p1 = Project(t.v1.x, t.v1.y, t.v1.z);
                var p2 = Project(t.v2.x, t.v2.y, t.v2.z);
                float avgZ = (p0.pz + p1.pz + p2.pz) / 3f;
                return (p0, p1, p2, avgZ, t.normal);
            }).OrderBy(t => t.avgZ).ToList();

            // Direção da luz
            float lx = -0.577f, ly = 0.577f, lz = -0.577f;

            using var img = new Image<Rgba32>(ThumbnailSize, ThumbnailSize);
            img.Mutate(ctx =>
            {
                ctx.Fill(Color.FromRgba(28, 28, 32, 255));

                foreach (var (p0, p1, p2, _, normal) in projected)
                {
                    float dot = normal.x * lx + normal.y * ly + normal.z * lz;
                    dot = Math.Max(0.1f, dot);
                    byte brightness = (byte)(Math.Min(255, (int)(dot * 200) + 40));
                    var color = Color.FromRgba(brightness, (byte)(brightness * 0.85f), (byte)(brightness * 0.7f), 255);

                    var pts = new PointF[]
                    {
                        new(pad + (p0.px * screenScale) + drawSize / 2f,
                            pad + (-p0.py * screenScale) + drawSize / 2f),
                        new(pad + (p1.px * screenScale) + drawSize / 2f,
                            pad + (-p1.py * screenScale) + drawSize / 2f),
                        new(pad + (p2.px * screenScale) + drawSize / 2f,
                            pad + (-p2.py * screenScale) + drawSize / 2f),
                    };

                    ctx.FillPolygon(color, pts);
                }
            });

            img.SaveAsPng(outPath);
            return true;
        }
        catch { return false; }
    }

    // ──────────────────────────────────────────────────
    // Fallback — generic icon by file extension
    // ──────────────────────────────────────────────────
    private static void GenerateFallbackThumbnail(string ext, string outPath)
    {
        using var img = new Image<Rgba32>(ThumbnailSize, ThumbnailSize);
        img.Mutate(ctx =>
        {
            ctx.Fill(Color.FromRgba(50, 50, 60, 255));

            // Rounded rect
            var rect = new RectangularPolygon(24, 24, ThumbnailSize - 48, ThumbnailSize - 48);
            ctx.Fill(Color.FromRgba(70, 70, 85, 255), rect);
        });
        img.SaveAsPng(outPath);
    }

    // ──────────────────────────────────────────────────
    // STEP/STP — parse B-rep edges, render isometric wireframe
    // ──────────────────────────────────────────────────
    private static bool RenderStepThumbnail(string filePath, string outPath)
    {
        try
        {
            var cartPoints   = new Dictionary<int, Vec3>();
            var vertexPoints = new Dictionary<int, int>();
            var edgeList     = new List<(int v1, int v2)>();

            ParseStepEntities(filePath, cartPoints, vertexPoints, edgeList);

            var segments = new List<(Vec3 a, Vec3 b)>(edgeList.Count);
            foreach (var (v1, v2) in edgeList)
            {
                if (!TryResolveStepVertex(v1, vertexPoints, cartPoints, out var p1)) continue;
                if (!TryResolveStepVertex(v2, vertexPoints, cartPoints, out var p2)) continue;
                if (p1.x == p2.x && p1.y == p2.y && p1.z == p2.z) continue;
                segments.Add((p1, p2));
            }

            if (segments.Count == 0) return false;

            // Subsample to keep rendering fast on dense models
            const int MaxSegments = 12_000;
            if (segments.Count > MaxSegments)
            {
                int step = segments.Count / MaxSegments;
                segments = segments.Where((_, i) => i % step == 0).ToList();
            }

            return RenderStepWireframe(segments, outPath);
        }
        catch { return false; }
    }

    // Matches STEP numbers in all valid forms: 1. / 1.0 / .5 / 1 / 1E-3 / -2.5E+1
    private const string StepNum = @"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?";

    private static readonly Regex _stepCartesianRx = new(
        @$"#(\d+)\s*=\s*CARTESIAN_POINT\s*\(\s*'[^']*'\s*,\s*\(\s*({StepNum})\s*,\s*({StepNum})\s*,\s*({StepNum})\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _stepVertexRx = new(
        @"#(\d+)\s*=\s*VERTEX_POINT\s*\(\s*'[^']*'\s*,\s*#(\d+)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _stepEdgeRx = new(
        @"#\d+\s*=\s*EDGE_CURVE\s*\(\s*'[^']*'\s*,\s*#(\d+)\s*,\s*#(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static void ParseStepEntities(
        string filePath,
        Dictionary<int, Vec3> cartPoints,
        Dictionary<int, int> vertexPoints,
        List<(int, int)> edgeList)
    {
        bool inData = false;
        var buf = new System.Text.StringBuilder(256);
        using var reader = new StreamReader(filePath);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var t = line.Trim();
            if (!inData)
            {
                if (t.Equals("DATA;", StringComparison.OrdinalIgnoreCase)) inData = true;
                continue;
            }
            if (t.Equals("ENDSEC;", StringComparison.OrdinalIgnoreCase)) break;

            buf.Append(t);
            if (t.EndsWith(';'))
            {
                ProcessStepEntity(buf.ToString(), cartPoints, vertexPoints, edgeList);
                buf.Clear();
            }
        }
    }

    private static void ProcessStepEntity(
        string entity,
        Dictionary<int, Vec3> cartPoints,
        Dictionary<int, int> vertexPoints,
        List<(int, int)> edgeList)
    {
        var m = _stepCartesianRx.Match(entity);
        if (m.Success)
        {
            cartPoints[int.Parse(m.Groups[1].Value)] = new Vec3(
                float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(m.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture));
            return;
        }
        m = _stepVertexRx.Match(entity);
        if (m.Success)
        {
            vertexPoints[int.Parse(m.Groups[1].Value)] = int.Parse(m.Groups[2].Value);
            return;
        }
        m = _stepEdgeRx.Match(entity);
        if (m.Success)
            edgeList.Add((int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)));
    }

    private static bool TryResolveStepVertex(
        int id,
        Dictionary<int, int> vertexPoints,
        Dictionary<int, Vec3> cartPoints,
        out Vec3 result)
    {
        if (vertexPoints.TryGetValue(id, out int ptId) && cartPoints.TryGetValue(ptId, out result))
            return true;
        // Some exporters reference CARTESIAN_POINT directly
        return cartPoints.TryGetValue(id, out result);
    }

    private static bool RenderStepWireframe(List<(Vec3 a, Vec3 b)> segments, string outPath)
    {
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
        foreach (var (a, b) in segments)
        {
            foreach (var v in new[] { a, b })
            {
                if (v.x < minX) minX = v.x; if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y; if (v.y > maxY) maxY = v.y;
                if (v.z < minZ) minZ = v.z; if (v.z > maxZ) maxZ = v.z;
            }
        }

        float cx = (minX + maxX) / 2f, cy = (minY + maxY) / 2f, cz = (minZ + maxZ) / 2f;
        float size = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
        if (size < 1e-6f) return false;

        const float angleX = 0.6154797f;
        const float angleY = 0.7853982f;
        float cosY = MathF.Cos(angleY), sinY = MathF.Sin(angleY);
        float cosX = MathF.Cos(angleX), sinX = MathF.Sin(angleX);

        (float px, float py, float pz) Project(float x, float y, float z)
        {
            x -= cx; y -= cy; z -= cz;
            float rx  = x * cosY - z * sinY;
            float rz  = x * sinY + z * cosY;
            float ry2 = y * cosX - rz * sinX;
            float rz2 = y * sinX + rz * cosX;
            return (rx, ry2, rz2);
        }

        int pad = 16;
        int drawSize = ThumbnailSize - pad * 2;
        float screenScale = drawSize / size * 0.82f;
        float MapX(float px) => pad + px * screenScale + drawSize / 2f;
        float MapY(float py) => pad + -py * screenScale + drawSize / 2f;

        var projected = segments.Select(s =>
        {
            var pa = Project(s.a.x, s.a.y, s.a.z);
            var pb = Project(s.b.x, s.b.y, s.b.z);
            return (pa, pb, avgZ: (pa.pz + pb.pz) / 2f);
        }).OrderBy(s => s.avgZ).ToList();

        float zMin   = projected[0].avgZ;
        float zMax   = projected[^1].avgZ;
        float zRange = Math.Max(zMax - zMin, 1e-6f);

        using var img = new Image<Rgba32>(ThumbnailSize, ThumbnailSize);
        img.Mutate(ctx =>
        {
            ctx.Fill(Color.FromRgba(28, 28, 32, 255));
            foreach (var (pa, pb, avgZ) in projected)
            {
                float t  = (avgZ - zMin) / zRange;
                byte  br = (byte)(80 + (int)(t * 175));
                var   col = Color.FromRgba(br, (byte)(br * 0.85f), (byte)(br * 0.7f), 255);
                ctx.DrawLine(col, 1f,
                    new PointF(MapX(pa.px), MapY(pa.py)),
                    new PointF(MapX(pb.px), MapY(pb.py)));
            }
        });

        img.SaveAsPng(outPath);
        return true;
    }

    // ──────────────────────────────────────────────────
    // STL parser (binary and ASCII formats)
    // ──────────────────────────────────────────────────
    private record struct Vec3(float x, float y, float z);

    private static List<(Vec3 v0, Vec3 v1, Vec3 v2, Vec3 normal)> ParseStl(string filePath)
    {
        var result = new List<(Vec3, Vec3, Vec3, Vec3)>();
        using var stream = File.OpenRead(filePath);

        // Detectar binário: primeiros 80 bytes são cabeçalho, depois uint32 com contagem
        byte[] header = new byte[80];
        int read = stream.Read(header, 0, 80);
        if (read < 80) return result;

        // ASCII começa com "solid" (mas binário pode também — verificar tamanho)
        bool isAscii = false;
        try
        {
            string headerStr = System.Text.Encoding.ASCII.GetString(header).TrimStart();
            if (headerStr.StartsWith("solid", StringComparison.OrdinalIgnoreCase))
            {
                // Confirma ASCII pelo tamanho esperado vs tamanho real
                uint triangleCount = 0;
                var br2 = new BinaryReader(stream);
                triangleCount = br2.ReadUInt32();
                long expectedBinary = 80 + 4 + triangleCount * 50L;
                isAscii = new FileInfo(filePath).Length != expectedBinary;
            }
        }
        catch { }

        stream.Position = 0;

        if (isAscii)
        {
            ParseAsciiStl(stream, result);
        }
        else
        {
            ParseBinaryStl(stream, result);
        }

        return result;
    }

    private static void ParseBinaryStl(Stream stream, List<(Vec3, Vec3, Vec3, Vec3)> result)
    {
        stream.Position = 80; // skip header
        using var br = new BinaryReader(stream);
        uint count = br.ReadUInt32();
        for (uint i = 0; i < count; i++)
        {
            var normal = new Vec3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            var v0    = new Vec3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            var v1    = new Vec3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            var v2    = new Vec3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            br.ReadUInt16(); // attribute byte count
            result.Add((v0, v1, v2, normal));
        }
    }

    private static void ParseAsciiStl(Stream stream, List<(Vec3, Vec3, Vec3, Vec3)> result)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream);

        Vec3 normal = default;
        var verts = new Vec3[3];
        int vi = 0;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith("facet normal"))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                normal = new Vec3(float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                                  float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture),
                                  float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture));
                vi = 0;
            }
            else if (line.StartsWith("vertex") && vi < 3)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                verts[vi++] = new Vec3(float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                                       float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                                       float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (line.StartsWith("endfacet") && vi == 3)
            {
                result.Add((verts[0], verts[1], verts[2], normal));
            }
        }
    }
}
