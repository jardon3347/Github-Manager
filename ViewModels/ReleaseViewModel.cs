using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GithubManager.Models;
using GithubManager.Services;
using Microsoft.Win32;

namespace GithubManager.ViewModels;

public partial class ReleaseViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    public ReleaseViewModel(MainViewModel main) => _main = main;

    [ObservableProperty]
    private ObservableCollection<RepositoryItem> _repos = new();

    [ObservableProperty]
    private RepositoryItem? _selectedRepo;

    [ObservableProperty]
    private ObservableCollection<BranchItem> _branches = new();

    [ObservableProperty]
    private BranchItem? _selectedBranch;

    [ObservableProperty]
    private string _tagName = "";

    [ObservableProperty]
    private string _releaseTitle = "";

    [ObservableProperty]
    private string _releaseBody = "";

    [ObservableProperty]
    private bool _isDraft;

    [ObservableProperty]
    private bool _isPrerelease;

    [ObservableProperty]
    private ObservableCollection<AssetItem> _assets = new();

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _technicalDetail = "";

    [ObservableProperty]
    private string _repoFilter = "";

    public ObservableCollection<RepositoryItem> FilteredRepos =>
        string.IsNullOrWhiteSpace(RepoFilter)
            ? Repos
            : new ObservableCollection<RepositoryItem>(
                Repos.Where(r => r.FullName.Contains(RepoFilter,
                    StringComparison.OrdinalIgnoreCase)));

    private void ShowError(ApiResult res)
    {
        StatusMessage = res.HumanMessage();
        IsError = true;
        TechnicalDetail = $"URL: {res.RequestUrl}\nBody: {res.ResponseBody}\nTech: {res.TechnicalDetail}";
    }

    private void ShowOk(string msg)
    {
        StatusMessage = msg;
        IsError = false;
        TechnicalDetail = "";
    }

    [RelayCommand]
    private async Task RefreshRepos()
    {
        if (_main.CurrentAccount == null) return;
        IsBusy = true;
        try
        {
            var svc = _main.CreateReposService();
            var (res, list) = await svc.GetRepos();
            if (!res.Success) { ShowError(res); return; }
            Repos = new ObservableCollection<RepositoryItem>(list);
            OnPropertyChanged(nameof(FilteredRepos));
            ShowOk($"已加载 {list.Count} 个仓库");
        }
        catch (Exception ex)
        {
            ShowError(ApiResult.Fail(null, "unexpected", "获取仓库失败", ex.Message));
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LoadBranchesAsync()
    {
        if (SelectedRepo == null) return;
        IsBusy = true;
        try
        {
            var svc = _main.CreateReposService();
            var (res, list) = await svc.GetBranches(SelectedRepo.Owner, SelectedRepo.Name);
            if (!res.Success) { ShowError(res); return; }
            Branches = new ObservableCollection<BranchItem>(list);
            SelectedBranch = Branches.FirstOrDefault(b =>
                b.Name.Equals(SelectedRepo.DefaultBranch, StringComparison.OrdinalIgnoreCase))
                ?? Branches.FirstOrDefault();
            ShowOk($"已加载 {list.Count} 个分支");
        }
        catch (Exception ex)
        {
            ShowError(ApiResult.Fail(null, "unexpected", "获取分支失败", ex.Message));
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void AddAssets()
    {
        var dlg = new OpenFileDialog { Title = "选择要上传的资产", Multiselect = true };
        if (dlg.ShowDialog() == true)
        {
            foreach (var f in dlg.FileNames)
            {
                var fi = new FileInfo(f);
                Assets.Add(new AssetItem
                {
                    FullPath = f,
                    FileName = fi.Name,
                    Size = fi.Length,
                    Selected = true
                });
            }
        }
    }

    [RelayCommand]
    private void RemoveAsset(AssetItem? item)
    {
        if (item != null) Assets.Remove(item);
    }

    [RelayCommand]
    private async Task CreateReleaseAsync()
    {
        if (SelectedRepo == null) { ShowError(ApiResult.Fail(null, "no_repo", "请先选择仓库")); return; }
        if (string.IsNullOrWhiteSpace(TagName)) { ShowError(ApiResult.Fail(null, "no_tag", "请填写 tag")); return; }
        if (string.IsNullOrWhiteSpace(ReleaseTitle)) { ShowError(ApiResult.Fail(null, "no_title", "请填写 Release 标题")); return; }
        var toUpload = Assets.Where(a => a.Selected).ToList();

        IsBusy = true;
        try
        {
            var svc = _main.CreateReleasesService();
            var req = new CreateReleaseRequest
            {
                TagName = TagName.Trim(),
                TargetCommitish = SelectedBranch?.Name ?? SelectedRepo.DefaultBranch,
                Name = ReleaseTitle.Trim(),
                Body = ReleaseBody,
                Draft = IsDraft,
                Prerelease = IsPrerelease
            };
            var (res, info) = await svc.CreateRelease(
                SelectedRepo.Owner, SelectedRepo.Name, req);
            if (!res.Success || info == null) { ShowError(res); return; }

            if (toUpload.Count > 0)
            {
                // 从 html_url 反推 upload_url 模板
                var uploadUrl = $"https://uploads.github.com/repos/" +
                    $"{SelectedRepo.Owner}/{SelectedRepo.Name}/releases/{info.Id}/assets{{?name,label}}";
                for (var i = 0; i < toUpload.Count; i++)
                {
                    var a = toUpload[i];
                    a.Status = "上传中...";
                    var progress = new Progress<long>(p => ProgressValue = (int)p);
                    await using var stream = File.OpenRead(a.FullPath);
                    var upRes = await svc.UploadAsset(uploadUrl, a.FileName, stream, progress);
                    a.Status = upRes.Success ? "✓ 成功" : $"✗ {upRes.Message}";
                }
            }
            ShowOk($"Release {info.TagName} 已创建");
        }
        catch (Exception ex)
        {
            ShowError(ApiResult.Fail(null, "unexpected", "创建 Release 失败", ex.Message));
        }
        finally
        {
            IsBusy = false;
            ProgressValue = 100;
        }
    }
}

public partial class AssetItem : ObservableObject
{
    [ObservableProperty]
    private string _fullPath = "";
    [ObservableProperty]
    private string _fileName = "";
    [ObservableProperty]
    private long _size;
    [ObservableProperty]
    private bool _selected = true;
    [ObservableProperty]
    private string _status = "";

    public string SizeText => Size < 1024 ? $"{Size} B"
        : Size < 1024 * 1024 ? $"{Size / 1024.0:F1} KB"
        : $"{Size / 1024.0 / 1024.0:F1} MB";
}
