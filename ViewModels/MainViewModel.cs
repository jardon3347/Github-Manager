using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using GithubManager.Services;
using GithubManager.Models;

namespace GithubManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly CredentialsService _creds;
    public MainViewModel(CredentialsService creds)
    {
        _creds = creds;
    }

    public Account? CurrentAccount => _creds.Current;
    public string CurrentLogin => CurrentAccount?.Login ?? "未登录";
    public bool HasAccount => CurrentAccount != null;

    /// <summary>给 View 用的工厂：根据当前 token 构造具体服务</summary>
    public GitHubClient? CreateClient()
    {
        var token = _creds.GetCurrentToken();
        return string.IsNullOrEmpty(token) ? null : new GitHubClient(token);
    }

    public ReposService CreateReposService()
    {
        var c = CreateClient() ?? throw new System.InvalidOperationException("未登录");
        return new ReposService(c);
    }

    public ContentsService CreateContentsService()
    {
        var c = CreateClient() ?? throw new System.InvalidOperationException("未登录");
        return new ContentsService(c);
    }

    public ReleasesService CreateReleasesService()
    {
        var c = CreateClient() ?? throw new System.InvalidOperationException("未登录");
        return new ReleasesService(c);
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(CurrentAccount));
        OnPropertyChanged(nameof(CurrentLogin));
        OnPropertyChanged(nameof(HasAccount));
    }
}
