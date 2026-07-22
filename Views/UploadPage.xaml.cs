using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GithubManager.ViewModels;

namespace GithubManager.Views;

public partial class UploadPage : Page
{
    public UploadPage() => InitializeComponent();

    private void Repo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is UploadViewModel vm)
        {
            vm.LoadBranchesCommand.Execute(null);
            if (vm.SelectedRepo != null)
                vm.NewRepoName = vm.SelectedRepo.Name;
        }
    }

    private void ShowTechDetail_Click(object sender, RoutedEventArgs e)
    {
        TechDetailBox.Visibility = TechDetailBox.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>双击文件夹：展开/收起</summary>
    private void TreeView_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not UploadViewModel vm) return;

        // 找到被点击的 TreeViewItem
        var originalSource = e.OriginalSource as DependencyObject;
        var treeItem = FindTreeViewItem(originalSource);
        if (treeItem?.DataContext is UploadTreeItem item && item.IsDirectory)
        {
            vm.EnterFolderCommand.Execute(item);
        }
    }

    /// <summary>CheckBox 点击：向下/向上传播勾选状态</summary>
    private void TreeCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb) return;
        if (cb.DataContext is not UploadTreeItem item) return;

        var isChecked = cb.IsChecked;

        if (item.IsDirectory)
        {
            // 目录勾选 → 递归设置所有子节点
            item.SetChecked(isChecked, true);
        }

        // 文件或目录勾选 → 通知父节点刷新三态
        item.RefreshParentState();
    }

    /// <summary>沿着视觉树找到 TreeViewItem 容器</summary>
    private static TreeViewItem? FindTreeViewItem(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is TreeViewItem tvi) return tvi;
            source = System.Windows.Media.VisualTreeHelper.GetParent(source);
        }
        return null;
    }
}
