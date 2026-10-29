# MioKit.EverythingSearch

基于 [Voidtools Everything](https://www.voidtools.com/) 的本机极速文件搜索插件。借助 Everything 索引，在 MioKit 搜索框中按分类作用域毫秒级检索文件名，不占用全局并行搜索。

## 前置条件

- Windows
- 已安装并运行 [Everything](https://www.voidtools.com/)

## 功能

- **本机极速搜索**：调用 Everything 本地服务（内置 EverythingSearchClient），单次最多返回 100 条
- **分类作用域**：全部文件、文件夹、音频、视频、可执行文件、文档、压缩包、图片
- **双入口附着**：输入触发词后按宿主 command 触发键附着；或在搜索框输入 `/` 从作用域列表选择
- **结果即开即用**：回车在资源管理器中打开文件、打开文件夹或定位所在目录
- **优雅降级**：Everything 未运行时结果置顶提示「Everything 不可用」
- **跨插件 API**：其他插件可通过版本化方法调用共享搜索通道

## 触发词

输入触发词后按宿主配置的 command 触发键附着，再输入关键字搜索。作用域也支持前缀补全和最短匹配。

| Command | 范围 |
| --- | --- |
| `everything-search` | 全部文件与文件夹 |
| `folder-search` | 仅文件夹 |
| `audio-search` | 音频 |
| `video-search` | 视频 |
| `exe-search` | 可执行文件与脚本 |
| `doc-search` | 文档 |
| `archive-search` | 压缩包 |
| `image-search` | 图片 |

在搜索框输入 `/` 可列出全部作用域，继续输入可按名称与 command 过滤；选中即附着。

## 结果操作

- 文件：在资源管理器中打开所在文件夹并选中该文件
- 文件夹：直接在资源管理器中打开
- 文件已不存在时：回退打开其所在目录

## 供其他插件调用

注入 `IPluginCallClient`，目标插件 ID 为 `miokit.everything-search`，契约版本均为 `1`：

| 方法 | 参数 | 成功结果 |
| --- | --- | --- |
| `everything.status` | 无 | `{ "available": true/false }` |
| `everything.search` | `{ "query": "原生 Everything 查询语法", "maxResults": 1..1000 }` | `{ "status": "succeeded", "items": [{ "fullPath", "name", "directory", "isFolder" }], "reason": null }` |

`everything.search` 的 `status` 还可能是 `unavailable`、`busy`、`timedOut` 或 `failed`。参数无效时返回 `invalid_request`。本插件搜索作用域与对外 API 共用同一串行查询通道。

## 项目结构

```text
plugin/     # 插件源码、Dependencies/EverythingSearchClient.dll、plugin.json
preview/    # Avalonia 预览宿主
tests/      # 测试
pack.ps1    # 打包脚本
```

## 开发

```powershell
dotnet restore MioKit.EverythingSearch.slnx
dotnet test tests/MioKit.EverythingSearch.Tests.csproj -c Release
dotnet build MioKit.EverythingSearch.slnx -c Release
```

打包：

```powershell
.\pack.ps1 -PackageVersion 1.1.0
```

输出默认在 `nupkgs/`。推送 `v*` 标签（如 `v1.1.0`）后，GitHub Actions 会用同一脚本打包到 `artifacts/`，并把 `.nupkg` 挂到对应 GitHub Release（不自动发布到 NuGet）。
