using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using STLHub.Services;

namespace STLHub.Views;

/// <summary>
/// Modal dialog showing folder import progress with cancellation support.
/// Displays a progress bar, the current file name, and running counts of imported objects,
/// attachments and skipped duplicates. Esc cancels the import, or closes once it has ended.
/// </summary>
public partial class ImportProgressDialog : Window
{
    private readonly CancellationTokenSource _cts = new();

    public CancellationToken CancellationToken => _cts.Token;

    public ImportProgressDialog()
    {
        InitializeComponent();
    }

    public void UpdateStatus(string fileName)
    {
        Dispatcher.UIThread.Post(() => StatusText.Text = fileName);
    }

    /// <summary>Shows the running totals; may be called from any thread.</summary>
    public void UpdateCounts(FolderImportResult counts)
    {
        var text = $"{Plural.Count(counts.ObjectsImported, "objeto", "objetos")} · " +
                   $"{Plural.Count(counts.AttachmentsImported, "anexo", "anexos")}";
        if (counts.DuplicatesSkipped > 0)
            text += $" · {Plural.Count(counts.DuplicatesSkipped, "já estava", "já estavam")} na biblioteca";
        Dispatcher.UIThread.Post(() => CountText.Text = text);
    }

    /// <summary>Switches the dialog to its finished state with a summary of the import.</summary>
    public void SetFinished(string summary) =>
        ShowOutcome("Importação concluída", summary, progress: 100);

    /// <summary>Switches the dialog to its cancelled state, keeping what was imported so far.</summary>
    public void SetCancelled(string summary) =>
        ShowOutcome("Importação cancelada", summary, progress: 0);

    /// <summary>Switches the dialog to its error state: what went wrong, and what was imported before it.</summary>
    public void SetError(string reason, string importedSoFar) =>
        ShowOutcome("A importação parou", $"{reason}\n\n{importedSoFar}", progress: 0);

    private void ShowOutcome(string title, string message, double progress)
    {
        Dispatcher.UIThread.Post(() =>
        {
            TitleText.Text = title;
            StatusText.Text = message;
            StatusText.TextTrimming = Avalonia.Media.TextTrimming.None;
            StatusText.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
            CountText.IsVisible = false;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = progress;
            CancelButton.Content = "Fechar";
            CancelButton.IsEnabled = true;
            CancelButton.IsDefault = true;
            CancelButton.Focus();
        });
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        if (!_cts.IsCancellationRequested && ProgressBar.IsIndeterminate)
        {
            _cts.Cancel();
            TitleText.Text = "Cancelando...";
            CancelButton.IsEnabled = false;
        }
        else
        {
            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _cts.Cancel();
        _cts.Dispose();
        base.OnClosed(e);
    }
}
