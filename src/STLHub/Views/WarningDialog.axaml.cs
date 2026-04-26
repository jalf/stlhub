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
        TitleText.Text = title;
        MessageText.Text = message;
    }

    private void Ok_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
