using Avalonia.Controls;

namespace STLHub.Views;

/// <summary>
/// Modal warning dialog with a single OK button.
/// </summary>
public partial class WarningDialog : Window
{
    public WarningDialog()
    {
        InitializeComponent();
    }

    public WarningDialog(string title, string message) : this()
    {
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        OkButton.Focus();
    }

    private void Ok_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
