using Avalonia.Controls;

namespace STLHub.Views;

/// <summary>
/// Non-blocking dialog shown when object loading from the database takes more than 2 seconds.
/// </summary>
public partial class LoadingDialog : Window
{
    public LoadingDialog()
    {
        InitializeComponent();
    }
}
