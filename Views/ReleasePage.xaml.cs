using System.Windows;
using System.Windows.Controls;
using GithubManager.ViewModels;

namespace GithubManager.Views;

public partial class ReleasePage : Page
{
    public ReleasePage() => InitializeComponent();

    private void Repo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ReleaseViewModel vm)
            vm.LoadBranchesCommand.Execute(null);
    }

    private void ShowTechDetail_Click(object sender, RoutedEventArgs e)
    {
        TechDetailBox.Visibility = TechDetailBox.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }
}
