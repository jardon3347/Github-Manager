using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using GithubManager.ViewModels;

namespace GithubManager.Views;

public partial class AccountsPage : Page
{
    public AccountsPage() => InitializeComponent();

    private void TokenBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountsViewModel vm)
            vm.TokenInput = (sender as PasswordBox)?.Password ?? "";
    }

    private void OpenLink(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void ShowTechDetail_Click(object sender, RoutedEventArgs e)
    {
        TechDetailBox.Visibility = TechDetailBox.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }
}
