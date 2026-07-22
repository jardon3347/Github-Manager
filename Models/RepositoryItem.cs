namespace GithubManager.Models;

public class RepositoryItem
{
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Owner { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsPrivate { get; set; }
    public bool IsFork { get; set; }
    public string DefaultBranch { get; set; } = "main";
    public bool HasReleasesScope => true; // PAT 如包含 repo 则默认有 releases 权限
}

public class BranchItem
{
    public string Name { get; set; } = "";
}

public class ContentItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Type { get; set; } = ""; // "file" | "dir" | "symlink" | "submodule"
    public long Size { get; set; }
    public string Sha { get; set; } = "";
    public string DownloadUrl { get; set; } = "";

    public bool IsFile => Type == "file";
    public bool IsDir => Type == "dir";
}

public class ReleaseInfo
{
    public long Id { get; set; }
    public string TagName { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Draft { get; set; }
    public bool Prerelease { get; set; }
    public string HtmlUrl { get; set; } = "";
}

public class CreateReleaseRequest
{
    public string TagName { get; set; } = "";
    public string TargetCommitish { get; set; } = "";
    public string Name { get; set; } = "";
    public string Body { get; set; } = "";
    public bool Draft { get; set; }
    public bool Prerelease { get; set; }
}
