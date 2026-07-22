using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GithubManager.Models;
using GithubManager.Services;

namespace GithubManager.ViewModels;

public partial class AccountsViewModel : ObservableObject
{
    private readonly CredentialsService _creds;
    private readonly MainViewModel _main;

    public AccountsViewModel(CredentialsService creds, MainViewModel main)
    {
        _creds = creds;
        _main = main;
        RefreshAccounts();
    }

    [ObservableProperty]
    private ObservableCollection<Account> _accounts = new();

    [ObservableProperty]
    private Account? _selectedAccount;

    [ObservableProperty]
    private string _loginInput = "";

    [ObservableProperty]
    private string _tokenInput = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _technicalDetail = "";

    public void RefreshAccounts()
    {
        Accounts = new ObservableCollection<Account>(_creds.Accounts);
        SelectedAccount = _creds.Current;
    }

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
    private async Task AddAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(TokenInput))
        {
            ShowError(ApiResult.Fail(null, "input_required", "Token 不能为空"));
            return;
        }
        if (string.IsNullOrWhiteSpace(LoginInput))
            LoginInput = "unknown";

        IsBusy = true;
        try
        {
            using var client = new GitHubClient(TokenInput.Trim());
            var svc = new ReposService(client);
            var (res, acc) = await svc.ValidateToken(LoginInput.Trim());
            if (!res.Success || acc == null)
            {
                ShowError(res);
                return;
            }

            _creds.SaveToken(acc.Login, TokenInput.Trim());
            _creds.AddAccount(acc);
            RefreshAccounts();
            _main.Refresh();
            LoginInput = "";
            TokenInput = "";
            ShowOk($"账号 @{acc.Login} 已添加");
        }
        catch (Exception ex)
        {
            ShowError(ApiResult.Fail(null, "unexpected", "发生意外错误", ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SwitchAccount(Account? acc)
    {
        if (acc == null) return;
        _creds.Current = acc;
        SelectedAccount = acc;
        _main.Refresh();
        ShowOk($"已切换到 @{acc.Login}");
    }

    [RelayCommand]
    private void DeleteAccount(Account? acc)
    {
        if (acc == null) return;
        if (MessageBox.Show($"确定删除账号 @{acc.Login}？这会清除本地凭据。", "确认",
            MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        _creds.RemoveAccount(acc);
        RefreshAccounts();
        _main.Refresh();
        ShowOk($"已删除 @{acc.Login}");
    }
}
