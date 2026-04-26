using Avalonia.Controls;

namespace STLHub.Views;

/// <summary>
/// Modal confirmation dialog with Confirm/Cancel buttons.
/// Sets <see cref="Result"/> to true if the user confirmed.
/// </summary>
public partial class ConfirmationDialog : Window
{
    public bool Result { get; private set; }

    public ConfirmationDialog()
    {
        InitializeComponent();
    }

    public ConfirmationDialog(string title, string message) : this()
    {
        TitleText.Text = title;
        MessageText.Text = message;
    }

    private void Confirm_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
