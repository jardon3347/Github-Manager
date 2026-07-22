# GitHub 管理器

Windows 桌面端 GitHub 管理工具，封装 GitHub REST API 实现：账号管理、文件上传、仓库文件浏览/编辑/删除、Release 发布。

基于 WPF (.NET 8) + MVVM (CommunityToolkit.Mvvm)。

## 功能

- **账号管理** — 多 PAT 多账号切换，Token 存 Windows 凭据管理器
- **上传文件** — 选本地文件夹 → 选仓库/分支 → TreeView 勾选 → 上传
- **管理仓库文件** — 树形浏览、在线编辑、下载/删除（支持整文件夹操作）
- **发布 Release** — 创建 Draft/Prerelease/Latest，上传二进制资产

## 运行

```bash
# 安装 .NET 8 SDK 后
dotnet run
```

## 打包

```bash
dotnet publish -c Release -r win-x64 -o publish /p:PublishSingleFile=true
```

输出 `publish/GithubManager.exe`（147MB，含 .NET 运行时）。

## Release 下载

去 [Releases](../../releases) 页面下载已打包的 exe，双击即用。
