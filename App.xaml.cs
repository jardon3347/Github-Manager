using System.Windows;
using GithubManager.Services;
using GithubManager.ViewModels;
using GithubManager.Views;

namespace GithubManager;

public partial class App : Application
{
    public static CredentialsService Creds = new();
    public static MainViewModel MainVm = new(Creds);
    public static AccountsViewModel AccountsVm = new(Creds, MainVm);
    public static UploadViewModel UploadVm = new(MainVm, Creds);
    public static FilesViewModel FilesVm = new(MainVm);
    public static ReleaseViewModel ReleaseVm = new(MainVm);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var win = new MainWindow();
        win.Show();
    }
}
