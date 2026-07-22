using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GithubManager.Models;
using GithubManager.Services;
using Microsoft.Win32;

namespace GithubManager.ViewModels;

public partial class UploadViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly CredentialsService _creds;

    public UploadViewModel(MainViewModel main, CredentialsService creds)
    {
        _main = main;
        _creds = creds;
        _ = RefreshReposAsync();
    }

    [ObservableProperty]
    private ObservableCollection<RepositoryItem> _repos = new();

    [ObservableProperty]
    private RepositoryItem? _selectedRepo;

    [ObservableProperty]
    private ObservableCollection<BranchItem> _branches = new();

    [ObservableProperty]
    private BranchItem? _selectedBranch;

    [ObservableProperty]
    private string _repoFilter = "";

    [ObservableProperty]
    private string _localFolder = "";

    [ObservableProperty]
    private string _targetPath = "";

    [ObservableProperty]
    private string _commitMessage = "";

    [ObservableProperty]
    private ObservableCollection<UploadTreeItem> _rootItems = new();

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
    private string _newRepoName = "";

    [ObservableProperty]
    private string _newRepoDescription = "";

    [ObservableProperty]
    private bool _newRepoPrivate = false;

    [ObservableProperty]
    private bool _newRepoAutoInit = true;

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
    private void PickFolder()
    {
        var dlg = new OpenFolderDialog { Title = "选择要上传的项目文件夹" };
        if (dlg.ShowDialog() == true)
        {
            LocalFolder = dlg.FolderName;
            LoadLocalFiles();
        }
    }

    [RelayCommand]
    private void RefreshFolder()
    {
        LoadLocalFiles();
    }

    private void LoadLocalFiles()
    {
        RootItems.Clear();
        if (!Directory.Exists(LocalFolder)) return;
        var basePath = LocalFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        // 递归构建树
        var root = new UploadTreeItem { FullPath = LocalFolder, RelativePath = "", IsDirectory = true };
        BuildTree(root, new DirectoryInfo(LocalFolder), basePath);
        foreach (var child in root.Children)
            RootItems.Add(child);

        var fileCount = root.Children.Sum(c => CountFiles(c));
        ShowOk($"已加载 {fileCount} 个文件（{RootItems.Count} 个顶层项）");
    }

    private static void BuildTree(UploadTreeItem parent, DirectoryInfo dir, string basePath)
    {
        // 子目录
        foreach (var subDir in dir.EnumerateDirectories())
        {
            var child = new UploadTreeItem
            {
                Name = subDir.Name,
                FullPath = subDir.FullName,
                RelativePath = subDir.FullName.Substring(basePath.Length),
                IsDirectory = true,
                IsChecked = false,
                IsExpanded = false,
                Parent = parent
            };
            BuildTree(child, subDir, basePath);
            parent.Children.Add(child);
        }
        // 文件
        foreach (var f in dir.EnumerateFiles())
        {
            parent.Children.Add(new UploadTreeItem
            {
                Name = f.Name,
                FullPath = f.FullName,
                RelativePath = f.FullName.Substring(basePath.Length),
                IsDirectory = false,
                IsChecked = true,
                Size = f.Length,
                Parent = parent
            });
        }
    }

    private static int CountFiles(UploadTreeItem item)
    {
        if (!item.IsDirectory) return 1;
        return item.Children.Sum(c => CountFiles(c));
    }

    [RelayCommand]
    private void ToggleAll()
    {
        if (RootItems.Count == 0) return;
        var anyChecked = RootItems.Any(i => i.IsChecked == true);
        var target = !anyChecked;
        foreach (var item in RootItems)
            item.SetChecked(target, true);
    }

    [RelayCommand]
    private void EnterFolder(UploadTreeItem? item)
    {
        if (item?.IsDirectory == true)
            item.IsExpanded = !item.IsExpanded;
    }

    [RelayCommand]
    private async Task UploadAsync()
    {
        if (SelectedRepo == null) { ShowError(ApiResult.Fail(null, "no_repo", "请先选择仓库")); return; }
        if (SelectedBranch == null) { ShowError(ApiResult.Fail(null, "no_branch", "请先选择分支")); return; }
        if (string.IsNullOrWhiteSpace(CommitMessage)) { ShowError(ApiResult.Fail(null, "no_msg", "请填写 commit message")); return; }

        // 递归收集所有勾选的文件（叶子节点）
        var toUpload = new System.Collections.Generic.List<UploadTreeItem>();
        foreach (var item in RootItems)
            CollectCheckedFiles(item, toUpload);

        if (toUpload.Count == 0) { ShowError(ApiResult.Fail(null, "no_files", "没有选中文件")); return; }

        IsBusy = true;
        var ok = 0; var fail = 0;
        var svc = _main.CreateContentsService();
        try
        {
            for (var i = 0; i < toUpload.Count; i++)
            {
                var f = toUpload[i];
                f.Status = "上传中...";
                ProgressValue = (i * 100) / toUpload.Count;
                var target = (TargetPath ?? "").Replace('\\', '/').TrimEnd('/');
                var repoPath = string.IsNullOrEmpty(target)
                    ? f.RelativePath.Replace('\\', '/')
                    : $"{target}/{f.RelativePath.Replace('\\', '/')}";

                // 检查远端是否已存在（拿 sha）
                var (existRes, existItem, _) = await svc.GetFile(
                    SelectedRepo.Owner, SelectedRepo.Name, repoPath);
                string? sha = existItem?.Sha;

                var bytes = await File.ReadAllBytesAsync(f.FullPath);
                var base64 = Convert.ToBase64String(bytes);
                var msg = $"{CommitMessage} ({f.RelativePath})";
                var res = await svc.UploadFile(SelectedRepo.Owner, SelectedRepo.Name,
                    repoPath, base64, msg, SelectedBranch.Name, sha);
                if (res.Success) { f.Status = "✓ 成功"; ok++; }
                else { f.Status = $"✗ {res.Message}"; fail++; }
            }
            ShowOk($"上传完成：{ok} 成功，{fail} 失败");
        }
        catch (Exception ex)
        {
            ShowError(ApiResult.Fail(null, "unexpected", "上传过程出错", ex.Message));
        }
        finally
        {
            IsBusy = false;
            ProgressValue = 100;
        }
    }

    [RelayCommand]
    private async Task CreateRepoAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRepoName)) { ShowError(ApiResult.Fail(null, "no_name", "请填写仓库名")); return; }
        IsBusy = true;
        try
        {
            var svc = _main.CreateReposService();
            var (res, repo) = await svc.CreateRepo(NewRepoName.Trim(), NewRepoDescription,
                NewRepoPrivate, NewRepoAutoInit);
            if (!res.Success || repo == null) { ShowError(res); return; }

            // 如果没勾选 auto_init，仓库为空无分支，自动推送初始 README 创建默认分支
            if (!NewRepoAutoInit)
            {
                var contentSvc = _main.CreateContentsService();
                var readmeBase64 = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes($"# {NewRepoName.Trim()}\n\n{NewRepoDescription}"));
                var initRes = await contentSvc.UploadFile(
                    repo.Owner, repo.Name, "README.md", readmeBase64,
                    "Initial commit", "main", null);
                if (initRes.Success)
                {
                    repo.DefaultBranch = "main";
                    ShowOk($"已创建仓库 {repo.FullName}（已自动初始化）");
                }
                else
                {
                    ShowOk($"已创建仓库 {repo.FullName}（⚠ 初始化失败：{initRes.Message}，请手动创建文件）");
                }
            }
            else
            {
                ShowOk($"已创建仓库 {repo.FullName}");
            }

            _ = RefreshReposAsync();
            SelectedRepo = Repos.FirstOrDefault(r => r.FullName == repo.FullName);
            NewRepoName = "";
            NewRepoDescription = "";
        }
        catch (Exception ex)
        {
            ShowError(ApiResult.Fail(null, "unexpected", "创建仓库失败", ex.Message));
        }
        finally { IsBusy = false; }
    }

    private static void CollectCheckedFiles(UploadTreeItem node,
        System.Collections.Generic.List<UploadTreeItem> result)
    {
        if (!node.IsDirectory)
        {
            if (node.IsChecked == true) result.Add(node);
            return;
        }
        foreach (var child in node.Children)
            CollectCheckedFiles(child, result);
    }

    private async Task RefreshReposAsync()
    {
        if (_main.CurrentAccount == null) return;
        var svc = _main.CreateReposService();
        var (res, list) = await svc.GetRepos();
        if (res.Success)
        {
            Repos = new ObservableCollection<RepositoryItem>(list);
            OnPropertyChanged(nameof(FilteredRepos));
        }
    }
}

public partial class UploadTreeItem : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _fullPath = "";

    [ObservableProperty]
    private string _relativePath = "";

    [ObservableProperty]
    private bool _isDirectory;

    [ObservableProperty]
    private bool? _isChecked = false;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private long _size;

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private ObservableCollection<UploadTreeItem> _children = new();

    /// <summary>父节点引用（用于子节点勾选时向上通知）</summary>
    public UploadTreeItem? Parent { get; set; }

    public string SizeText => IsDirectory ? ""
        : Size < 1024 ? $"{Size} B"
        : Size < 1024 * 1024 ? $"{Size / 1024.0:F1} KB"
        : $"{Size / 1024.0 / 1024.0:F1} MB";

    public string Icon => IsDirectory ? "📁" : "📄";

    /// <summary>设置勾选状态并向下递归传播</summary>
    public void SetChecked(bool? value, bool recursive)
    {
        IsChecked = value;
        if (recursive && IsDirectory)
            foreach (var child in Children)
                child.SetChecked(value, true);
    }

    /// <summary>子节点变化后刷新父节点三态</summary>
    public void RefreshParentState()
    {
        if (Parent == null || Parent.Children.Count == 0) return;
        var allChecked = Parent.Children.All(c => c.IsChecked == true);
        var noneChecked = Parent.Children.All(c => c.IsChecked == false);
        Parent.IsChecked = allChecked ? true : noneChecked ? false : null;
        Parent.RefreshParentState();
    }
}
