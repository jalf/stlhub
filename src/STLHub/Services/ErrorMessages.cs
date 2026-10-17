using System;
using System.ComponentModel;
using System.IO;
using Microsoft.Data.Sqlite;

namespace STLHub.Services;

/// <summary>
/// Turns exceptions into short, plain-language explanations for the UI, so users see what
/// went wrong and what they can do about it instead of a raw framework message.
/// </summary>
public static class ErrorMessages
{
    /// <summary>Returns a user-facing explanation of <paramref name="ex"/>.</summary>
    public static string Describe(Exception ex) => ex switch
    {
        UnauthorizedAccessException =>
            "O STLHub não tem permissão para acessar esse arquivo ou pasta.",
        DirectoryNotFoundException =>
            "A pasta não foi encontrada. Ela pode ter sido movida, renomeada ou estar em um disco desconectado.",
        FileNotFoundException =>
            "O arquivo não foi encontrado. Ele pode ter sido movido ou apagado fora do STLHub.",
        PathTooLongException =>
            "O caminho do arquivo é longo demais para o sistema.",
        IOException =>
            "O arquivo está em uso por outro programa, ou o disco não está acessível ou está cheio.",
        SqliteException =>
            "O banco de dados da biblioteca não pôde ser lido. Ele pode estar em uso por outra janela do STLHub ou danificado.",
        Win32Exception =>
            "Nenhum aplicativo está associado a esse tipo de arquivo.",
        _ => $"Erro inesperado: {ex.Message}",
    };
}

/// <summary>
/// Count phrasing kept in one place so UI strings never fall back to "objeto(s)" and can be
/// replaced by a localization layer later.
/// </summary>
public static class Plural
{
    /// <summary>Formats a count with the singular or plural noun, e.g. "1 objeto", "3 objetos".</summary>
    public static string Count(int count, string singular, string plural) =>
        $"{count} {(count == 1 ? singular : plural)}";

    /// <summary>"1 objeto" / "N objetos".</summary>
    public static string Objects(int count) => Count(count, "objeto", "objetos");
}
