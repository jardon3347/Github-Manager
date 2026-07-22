using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using AdysTech.CredentialManager;
using GithubManager.Models;

namespace GithubManager.Services;

public class CredentialsService
{
    private readonly string _accountsFile;
    private List<Account> _accounts = new();
    private Account? _current;

    public CredentialsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GithubManager");
        Directory.CreateDirectory(dir);
        _accountsFile = Path.Combine(dir, "accounts.json");
        Load();
    }

    public IReadOnlyList<Account> Accounts => _accounts;
    public Account? Current
    {
        get => _current;
        set
        {
            _current = value;
            Save();
        }
    }

    public string TokenKey(string login) => $"GithubManager:{login}";

    public string? GetToken(string login)
    {
        try
        {
            var cred = CredentialManager.GetCredentials(TokenKey(login));
            return cred?.Password;
        }
        catch
        {
            return null;
        }
    }

    public string? GetCurrentToken() =>
        _current == null ? null : GetToken(_current.Login);

    public void SaveToken(string login, string token)
    {
        var cred = new NetworkCredential(login, token);
        CredentialManager.SaveCredentials(TokenKey(login), cred, CredentialType.Generic);
    }

    public void RemoveToken(string login)
    {
        CredentialManager.RemoveCredentials(TokenKey(login), CredentialType.Generic);
    }

    public void AddAccount(Account account)
    {
        var existing = _accounts.FirstOrDefault(a =>
            string.Equals(a.Login, account.Login, StringComparison.OrdinalIgnoreCase));
        if (existing != null) _accounts.Remove(existing);
        _accounts.Add(account);
        _current = account;
        Save();
    }

    public void RemoveAccount(Account account)
    {
        RemoveToken(account.Login);
        _accounts.RemoveAll(a =>
            string.Equals(a.Login, account.Login, StringComparison.OrdinalIgnoreCase));
        if (_current != null && string.Equals(_current.Login, account.Login,
            StringComparison.OrdinalIgnoreCase))
            _current = _accounts.FirstOrDefault();
        Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_accountsFile))
            {
                var json = File.ReadAllText(_accountsFile);
                _accounts = JsonSerializer.Deserialize<List<Account>>(json) ?? new();
                _current = _accounts.FirstOrDefault();
            }
        }
        catch
        {
            _accounts = new();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_accounts, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_accountsFile, json);
        }
        catch { /* 忽略保存错误，不影响主流程 */ }
    }
}
