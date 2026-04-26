using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace STLHub.Views;

/// <summary>
/// Modal dialog showing folder import progress with cancellation support.
/// Displays a progress bar, current file name, and object/attachment counts.
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

    public void UpdateCounts(int objects, int attachments)
    {
        Dispatcher.UIThread.Post(() =>
            CountText.Text = $"{objects} objeto(s), {attachments} anexo(s)");
    }

    public void SetFinished(int objects, int attachments)
    {
        Dispatcher.UIThread.Post(() =>
        {
            TitleText.Text = "Importação concluída";
            StatusText.Text = $"{objects} objeto(s) e {attachments} anexo(s) importados.";
            CountText.IsVisible = false;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 100;
            CancelButton.Content = "Fechar";
        });
    }

    public void SetError(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            TitleText.Text = "Erro na importação";
            StatusText.Text = message;
            CountText.IsVisible = false;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 0;
            CancelButton.Content = "Fechar";
        });
    }

    public void SetCancelled(int objects, int attachments)
    {
        Dispatcher.UIThread.Post(() =>
        {
            TitleText.Text = "Importação cancelada";
            StatusText.Text = $"{objects} objeto(s) e {attachments} anexo(s) importados antes do cancelamento.";
            CountText.IsVisible = false;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 0;
            CancelButton.Content = "Fechar";
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
