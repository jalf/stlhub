using System;
using System.Globalization;
using Avalonia.Data.Converters;
using STLHub.ViewModels;

namespace STLHub.Converters;

public class CategoryIdToNameConverter : IValueConverter
{
    public static readonly CategoryIdToNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            Avalonia.Controls.Window? mainWindow = null;
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                mainWindow = desktop.MainWindow;
            }
            var vm = mainWindow?.DataContext as MainWindowViewModel;
            if (vm == null)
                return string.Empty;

            if (value is int categoryId)
            {
                return vm.GetCategoryName(categoryId) ?? string.Empty;
            }
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}