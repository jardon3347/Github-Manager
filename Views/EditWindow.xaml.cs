using System.Windows;
using GithubManager.ViewModels;

namespace GithubManager.Views;

public partial class EditWindow : Window
{
    public EditWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            if (DataContext is EditFileViewModel vm)
                vm.EditContent = vm.OriginalContent;
        };
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is EditFileViewModel vm)
        {
            var ok = await vm.SubmitAsync(App.MainVm);
            if (ok) DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void ShowTechDetail_Click(object sender, RoutedEventArgs e)
    {
        TechDetailBox.Visibility = TechDetailBox.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }
}
