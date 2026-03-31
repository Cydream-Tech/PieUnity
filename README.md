# Pie Unity Agent

`pie-unity` 是一个基于 [PuerTS](https://github.com/Tencent/puerts) V8 的 Unity AI Agent 包。  
它采用 **Runtime-first** 设计：核心逻辑在 Runtime 层，Editor 仅作为调试和提效外壳。

## 设计目标

- 以 Unity Runtime 为主要运行场景（例如语音驱动、NPC 行为、运行时自动化）
- 以 `Application.persistentDataPath` 作为默认首要工作目录（Player 场景）
- 在 Editor 中保留 `Pie Chat` 窗口用于开发调试
- 提供独立 `eval_js` 工具，支持以代码形式直接编排 Unity API 调用

## 功能概览

- LLM 对话与流式响应（SSE）
- 文件工具（`read` / `write` / `edit` / `ls` / `grep` / `find`）
- 运行时 JavaScript 执行工具：`eval_js`
- 任务规划工具：`manage_todo_list`
- C# <-> JS 双向事件桥接
- Runtime 和 Editor 共用同一套 Agent 核心

## 前置依赖

1. **PuerTS** (`com.tencent.puerts.core`)  
   - `pie-unity` 不会自动安装 PuerTS，必须在 Unity 项目里单独安装
   - 必须使用 **PuerTS V8 后端**
   - 需要放置与你目标平台匹配的 V8 native plugin
   - 如果缺少 PuerTS 或 V8 plugin，`PieBridge` 无法初始化，`Pie Chat` 和 Runtime 都不会正常工作

## 安装

通过 Unity Package Manager 安装：

- 源码 monorepo 本地开发：`Add package from disk...` 选择 `products/pie-unity/package.json`
- 独立发布仓库：`Add package from git URL...`，使用独立 `pie-unity` 仓库的 tag
- Samples：导入后可在 Package Manager 的 Samples 区域导入 `Extension Demo`

建议先确认 Unity 项目中已经存在：

- `com.tencent.puerts.core`
- 对应平台的 PuerTS V8 native plugin

## 独立发布仓库

`pie-unity` 的主开发仍在 `pie` monorepo 中进行，但对外发布建议使用一个独立 Git 仓库，其仓库根目录就是 Unity package 本身。

维护这个独立仓库时，推荐从 monorepo 根目录执行：

```bash
npm run export:pie-unity -- \
  --target /absolute/path/to/pie-unity-release \
  --repo-url https://github.com/<org>/pie-unity.git
```

这个导出流程会：

- 校验源仓库工作树是否干净
- 重新构建 `Resources/pie/core.js`
- 只导出独立发布需要的 Unity package 文件
- 为目标仓库写入适合独立发布的 `package.json`

如果目标目录已经是一个独立 Git 仓库，还可以继续追加：

```bash
--commit --tag
```

当前仓库已经使用 `Apache-2.0` 许可证。发布独立仓库时，请保持 `LICENSE` 与 `package.json` 中的许可证字段一致。

## 快速开始

### Editor 模式（调试）

1. 打开 `Tools > Pie > Pie Chat`（快捷键 `Alt+Shift+P`）
2. 在 Settings 设置 `apiKey`、`provider`、`model`
3. 开始对话

基础命令：

- `/new`：开始一个新的本地 session
- `/resume <sessionId>`：恢复已保存 session
- `/tree`：查看当前项目下的 session 树
- `/fork` 或 `/fork <sessionId>`：从当前或指定 session 创建分支
- `/session-check`：执行 session 保存/恢复自检
- `/model`：查看当前模型和可用 preset
- `/model <preset>`：使用预设模型
- `/model <provider> <modelId>`：切换到指定 provider/model
- `/skills`：列出当前可用 skills
- `/skills reload`：重新加载并刷新 skills 到 system prompt
- `/memory`：查看当前 `Assets/Pie/AGENTS.md`
- `/init`：基于当前 Unity 上下文初始化 `Assets/Pie/AGENTS.md`

时间线操作：

- `Follow`：是否自动跟随最新流式输出
- `Latest`：当关闭跟随后，跳转到最新消息

Skills 面板：

- 在窗口顶部切换 `Skills`
- 可查看 builtin/project skills
- 支持 `Reload` 和 `Refresh`
- 支持创建、编辑、删除 project skills
- 支持 `From Chat` 和 `From Memory` 生成可编辑 skill 草稿

Sessions 面板：

- 在窗口顶部切换 `Sessions`
- 可视化查看 session tree
- 支持面板内 `New`、`Resume`、`Fork`

### Runtime 模式（推荐主场景）

1. 在场景中挂载 `PieRunner` 组件
2. 如需自定义工作目录，可在 Inspector 设置 `Project Root Override`，或在游戏代码启动时调用 `Reinitialize(customPath)`
3. 在游戏代码里调用 `SetConfig(...)` 和 `SendMessage(...)`
4. 监听 `OnAssistantMessage`、`OnAssistantDelta`、`OnToolStart`、`OnToolEnd`、`OnError`

Runtime 工具边界：

- Runtime 文件工具会通过 C# 异步文件桥接执行，底层使用 `Task.Run` 包裹纯 `System.IO`
- 后台线程绝不能访问 Unity API；这里只用于文件系统 I/O
- `grep` / `find` 在大目录下仍然可能比较重，但不应再直接卡住主线程

示例（简化）：

```csharp
using System.IO;
using UnityEngine;

public class Demo : MonoBehaviour
{
    [SerializeField] private Pie.PieRunner runner;

    private void Start()
    {
        runner.Reinitialize(Path.Combine(Application.persistentDataPath, "PieWorkspace"));
        runner.SetConfig("YOUR_API_KEY", "anthropic", "claude-3-5-haiku-latest");
        runner.OnAssistantMessage += msg => Debug.Log("AI: " + msg);
        runner.SendMessage("你好，帮我创建一个 Cube 并把它放到 (0,1,0)");
    }
}
```

## 工作目录规则

- **Player / Runtime 默认**：`Application.persistentDataPath`
- **Editor 默认**：Unity 项目根目录（`Assets` 的父目录）
- 也可以通过 `PieRunner.Reinitialize(customPath)` 或 Inspector 中的 `Project Root Override` 指定自定义 `projectRoot`

## Pie Settings

`pie-unity` 现在使用可见的 Unity 资产目录来承载正式的 project skills 和 project extensions：

- 默认 settings asset：`Assets/Pie/PieSettings.asset`
- 默认 skills 目录：`Assets/Pie/Skills`
- 默认 extensions 目录：`Assets/Pie/Extensions`

如果还没有 settings asset，可以在菜单中创建：

- `Tools > Pie > Create Pie Settings Asset`

`PieSettings` 允许你配置：

- `Extension Search Paths`
- `Skill Search Paths`

这些路径按顺序扫描，**后面的路径会覆盖前面同名的 skill / extension**。

如果没有创建 `PieSettings.asset`，`pie-unity` 会自动使用上面的默认目录。

对于 Runtime / Player，如果你希望明确指定一份 settings 资产，可以直接把 `PieSettings` 赋给 `PieRunner` 组件上的 `Settings Override` 字段。

UPM samples 只作为导入入口。即使 sample 本体被 Unity 放到 `Assets/Samples/...`，`pie-unity` 里的正式项目文件也应该安装或整理到 `Assets/Pie/...`。

## Skills

`pie-unity` 支持项目级 skill 资产，并会把可用 skill 列进 system prompt，同时提供 `read_skill` 工具供 agent 按需读取完整内容。

默认目录：

- `Assets/Pie/Skills`

支持两种组织形式：

- 目录 skill：`Assets/Pie/Skills/<skill-name>/SKILL.md`
- 单文件 skill：`Assets/Pie/Skills/<skill-name>.md`

### 开发一个 skill

最小示例：

```md
---
name: scene-review
description: Review Unity scenes and gameplay scripts before making changes.
---

# scene-review

Use this skill when the task touches scene wiring, GameObject lookup, or serialized MonoBehaviour fields.

Checklist:
- Inspect the active scene hierarchy first
- Prefer minimal edits to serialized fields
- Verify Play Mode assumptions before changing runtime code
```

建议：

- 把 skill 写成“什么时候用 + 做事规则 + 检查清单”
- skill 适合放项目约定、工作流、命名规范、验证步骤
- 如果 skill 需要被 agent 深入使用，先执行 `/skills reload`

Editor 中也可以直接在 `Pie Chat` 的 `Skills` 面板里新建、编辑、删除 project skill。

## Project Extensions

`pie-unity` 现在支持从项目目录加载轻量扩展脚本：

- 默认目录：`Assets/Pie/Extensions`
- 文件：`*.js`

如果你希望把扩展分散到别的目录，可以在 `PieSettings.asset` 的 `Extension Search Paths` 里继续追加。

### 开发一个 extension

每个扩展文件需要调用：

```js
pieExtension((api) => {
  api.appendSystemPrompt("Use the hello_world tool for greeting demos.");

  api.registerTool({
    name: "hello_world",
    label: "hello_world",
    description: "Return a greeting string.",
    parameters: {
      type: "object",
      properties: {},
    },
    async execute() {
      return {
        content: [{ type: "text", text: "Hello from project extension." }],
        details: {},
      };
    },
  });
});
```

如果扩展需要调用 Unity/C# 宿主能力，可以使用：

```js
pieExtension((api) => {
  api.registerTool({
    name: "read_runtime_flag",
    label: "read_runtime_flag",
    description: "Read a runtime flag from the host app.",
    parameters: {
      type: "object",
      properties: {
        key: { type: "string" },
      },
    },
    async execute(_toolCallId, args) {
      const key = typeof args?.key === "string" && args.key.length > 0 ? args.key : "demo_status";
      const result = await api.callHost("read_runtime_flag", { key });
      return {
        content: [{ type: "text", text: String(result?.value ?? "") }],
        details: result ?? {},
      };
    },
  });
});
```

Unity/C# 侧可注册对应 handler：

```csharp
Pie.PieHostBridge.Register("read_runtime_flag", (argsJson) =>
{
    return "{\"value\":\"demo\"}";
});
```

当前支持的扩展能力：

- `api.registerTool(tool)`
- `api.registerBeforeToolCall(hook)`
- `api.registerAfterToolCall(hook)`
- `api.registerOnError(hook)`
- `api.appendSystemPrompt(text)`
- `api.callHost(name, args)`

内置命令：

- `/extensions`：查看当前已加载扩展
- `/extensions reload`：重新扫描并加载当前配置的 extension search paths

说明：

- 当前扩展加载是 **TS/JS-first** 的，面向 runtime 实时扩展
- 扩展文件运行在与 `core.js` 相同的 PuerTS JsEnv 中
- 第一版只支持同步激活：`pieExtension((api) => { ... })`

## 项目记忆

`pie-unity` 现在会自动读取 `<projectRoot>/Assets/Pie/AGENTS.md` 并将其摘要注入 system prompt。

- `/memory`：查看当前 `AGENTS.md`
- `/init`：基于当前 Unity 上下文初始化或更新 `AGENTS.md`

建议将以下内容持久化到 `AGENTS.md`：

- 当前 Render Pipeline
- 材质和 Shader 约束
- 命名规范
- 常用验证步骤
- 项目特定的安全边界

## Runtime State

运行时状态和本地会话数据不再放在项目目录里，而是统一写入：

- `Application.persistentDataPath/Pie/sessions`

目前这包括：

- sessions
- session tree 分支记录
- 每个 session 里持久化的 todo 状态

## `manage_todo_list` 工具

这是从 Pie builtin todo 扩展迁移过来的版本，用于在复杂多步骤任务中维护结构化待办列表。

常见用法：

- `read`：读取当前 todo 列表
- `write`：用完整列表覆盖当前状态

每次 `new_session` 都会自动清空当前 todo 状态。

## `eval_js` 工具

`eval_js` 是独立工具，不与文件工具耦合。  
它用于在 PuerTS V8 中执行 JS 代码，并通过全局 `CS` 访问 Unity C# API。

典型用途：

- 创建/查找/操作 GameObject
- 添加组件与调用组件方法
- 调整 Transform、材质、场景对象
- 在 Editor 场景下调用 UnityEditor API（运行时不可用）

说明：

- 返回值是“最后一个表达式”的结果
- 复杂对象建议在代码中 `JSON.stringify(...)` 后返回
- 错误会返回 `message + stack`

这类代码执行式编排与 Anthropic 提到的 Programmatic Tool Calling 思路一致，可降低上下文污染并提升复杂任务稳定性。

参考：<https://www.anthropic.com/engineering/advanced-tool-use>

## 构建 `core.js`

`Resources/pie/core.js` 是预构建产物。源码改动后请重新打包：

```bash
cd products/pie-unity
npm install
npm run build
```

## Runtime-first 架构

```text
Runtime (核心)
  PieBridge.cs      -> JsEnv 生命周期 + Tick() + SSE push
  PieRunner.cs      -> MonoBehaviour 入口 + Runtime API
  PieHttpBridge.cs  -> C# HTTP/SSE
  PieDiagnostics.cs -> 日志

Editor (外壳)
  PieChatWindow.cs  -> 调试 UI（基于 PieBridge）

JavaScript (PuerTS V8)
  src/index.ts         -> Agent 入口与事件转发
  src/unity-http-client.ts
  src/tools/eval-js.ts -> eval_js
  @pie/agent-core / @pie/ai / @pie/tools-fs / @pie/platform
```

## 注意事项

- WebGL 等平台对 `HttpClient` 和线程能力有限，需单独验证
- iOS/Android 需确认 PuerTS 与 native plugin 配置完整
- `EditorPrefs` 仅适合本机开发调试，不建议作为生产密钥存储
- 当前文件工具已接入 PuerTS runtime 路径，但还没有完整覆盖所有 Player 平台的实机回归
