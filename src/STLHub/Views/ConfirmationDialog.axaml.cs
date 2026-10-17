using System;
using Avalonia.Controls;
using STLHub.ViewModels;

namespace STLHub.Views;

/// <summary>
/// Modal confirmation dialog. The confirming button names the action; a destructive request
/// colors it with the danger brush and makes Cancel the default, so Enter never destroys data.
/// Esc always cancels. Sets <see cref="Result"/> to true if the user confirmed.
/// </summary>
public partial class ConfirmationDialog : Window
{
    public bool Result { get; private set; }

    public ConfirmationDialog()
    {
        InitializeComponent();
    }

    public ConfirmationDialog(ConfirmRequest request) : this()
    {
        Title = request.Title;
        TitleText.Text = request.Title;
        MessageText.Text = request.Message;
        ConfirmButton.Content = request.ConfirmLabel;

        if (request.IsDestructive)
        {
            CancelButton.IsDefault = true;
            ConfirmButton.Classes.Add("danger");
        }
        else
        {
            ConfirmButton.IsDefault = true;
            ConfirmButton.Classes.Add("primary");
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        (ConfirmButton.IsDefault ? ConfirmButton : CancelButton).Focus();
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
