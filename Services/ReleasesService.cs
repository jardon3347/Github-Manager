using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GithubManager.Models;

namespace GithubManager.Services;

public class ReleasesService
{
    private readonly GitHubClient _client;
    public ReleasesService(GitHubClient client) => _client = client;

    public async Task<(ApiResult result, ReleaseInfo? info)> CreateRelease(
        string owner, string repo, CreateReleaseRequest req, CancellationToken ct = default)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases";
        var body = new
        {
            tag_name = req.TagName,
            target_commitish = req.TargetCommitish,
            name = req.Name,
            body = req.Body,
            draft = req.Draft,
            prerelease = req.Prerelease
        };
        var (res, data) = await _client.PostAsync<JsonElement>(url, body, ct);
        if (!res.Success)
            return (ApiResult.Fail(res.StatusCode, res.ErrorCode,
                $"创建 Release 失败（{res.Message}）", res.TechnicalDetail,
                $"{owner}/{repo}@{req.TagName}", url, res.ResponseBody), null);

        data.TryGetProperty("id", out var idEl);
        data.TryGetProperty("tag_name", out var tagEl);
        data.TryGetProperty("name", out var nameEl);
        data.TryGetProperty("draft", out var draftEl);
        data.TryGetProperty("prerelease", out var preEl);
        data.TryGetProperty("html_url", out var htmlEl);
        data.TryGetProperty("upload_url", out var uploadEl);

        return (ApiResult.Ok(), new ReleaseInfo
        {
            Id = idEl.ValueKind == JsonValueKind.Number ? idEl.GetInt64() : 0,
            TagName = tagEl.GetString() ?? "",
            Name = nameEl.GetString() ?? "",
            Draft = draftEl.ValueKind == JsonValueKind.True,
            Prerelease = preEl.ValueKind == JsonValueKind.True,
            HtmlUrl = htmlEl.GetString() ?? ""
        });
    }

    /// <summary>上传资产到 release 的 upload_url（去掉 {name,label} 模板）</summary>
    public async Task<ApiResult> UploadAsset(
        string uploadUrl, string fileName, Stream stream,
        IProgress<long>? progress, CancellationToken ct = default)
    {
        var templateIdx = uploadUrl.IndexOf('?');
        var baseUrl = templateIdx > 0 ? uploadUrl[..templateIdx] : uploadUrl;
        var encodedName = Uri.EscapeDataString(fileName);
        var url = $"{baseUrl}?name={encodedName}";
        return await _client.UploadBinaryAsync(url, stream, fileName, progress, ct);
    }
}

public class ReleaseUploadContext
{
    public string Owner = "";
    public string Repo = "";
    public CreateReleaseRequest Request = new();
}
