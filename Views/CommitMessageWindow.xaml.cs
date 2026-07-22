using System.Windows;

namespace GithubManager.Views;

public partial class CommitMessageWindow : Window
{
    public string CommitMessage => MessageBox.Text;

    public CommitMessageWindow() => InitializeComponent();

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
