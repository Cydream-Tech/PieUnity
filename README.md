# Pie Unity Agent

`pie-unity` is a Unity AI agent package built on top of [PuerTS](https://github.com/Tencent/puerts) V8.
It uses a runtime-first architecture: the agent core runs in Runtime, while the Editor provides a chat UI and authoring workflow.

## Highlights

- Streaming chat and tool execution inside Unity
- Runtime and Editor share the same agent core
- Project assets live under `Assets/Pie`
- Runtime state lives under `Application.persistentDataPath/Pie`
- Project skills, project extensions, and project memory are first-class package concepts

## Requirements

`pie-unity` depends on PuerTS:

- Install `com.tencent.puerts.core`
- Use the PuerTS V8 backend
- Include the native V8 plugin for your target platform

If PuerTS or the V8 runtime is missing, `PieBridge` will not initialize and neither `Pie Chat` nor `PieRunner` will work.

## Installation

Install through Unity Package Manager:

- Local monorepo development:
  `Add package from disk...` and select `products/pie-unity/package.json`
- Git release repository:
  `Add package from git URL...`
- Current public Git URL:
  `https://github.com/Cydream-Tech/PieUnity.git#0.1.1`

After installation, you can import the `Extension Demo` sample from the Package Manager Samples section.

## Quick Start

### Editor

1. Open `Tools > Pie > Pie Chat`
2. Enter your `apiKey`, `provider`, and `model`
3. Start chatting

Available local commands include:

- `/new`
- `/resume <sessionId>`
- `/tree`
- `/fork` or `/fork <sessionId>`
- `/session-check`
- `/model`
- `/skills`
- `/skills reload`
- `/memory`
- `/init`

### Runtime

1. Add `PieRunner` to a GameObject
2. Optionally assign `Settings Override`
3. Call `SetConfig(...)`
4. Call `SendMessage(...)`
5. Listen to `OnAssistantMessage`, `OnAssistantDelta`, `OnToolStart`, `OnToolEnd`, and `OnError`

Example:

```csharp
using System.IO;
using UnityEngine;

public class Demo : MonoBehaviour
{
    [SerializeField] private Pie.PieRunner runner;

    private void Start()
    {
        runner.Reinitialize(Path.Combine(Application.persistentDataPath, "PieWorkspace"));
        runner.SetConfig("YOUR_API_KEY", "openai", "gpt-4.1-mini");
        runner.OnAssistantMessage += msg => Debug.Log("AI: " + msg);
        runner.SendMessage("Create a cube at (0,1,0).");
    }
}
```

## Paths

Project assets:

- `Assets/Pie/PieSettings.asset`
- `Assets/Pie/Skills`
- `Assets/Pie/Extensions`
- `Assets/Pie/AGENTS.md`

Runtime state:

- `Application.persistentDataPath/Pie/sessions`
- `Application.persistentDataPath/Pie/...`

Imported UPM samples are only staging content. If Unity imports a sample into `Assets/Samples/...`, the actual working files for `pie-unity` should still end up under `Assets/Pie/...`.

## Pie Settings

Create a settings asset from:

- `Tools > Pie > Create Pie Settings Asset`

`PieSettings` currently exposes:

- `Extension Search Paths`
- `Skill Search Paths`

Paths are scanned in order, and later paths override earlier items with the same skill or extension name.

## Skills

Default project skill directory:

- `Assets/Pie/Skills`

Supported layouts:

- `Assets/Pie/Skills/<skill-name>/SKILL.md`
- `Assets/Pie/Skills/<skill-name>.md`

Skills are listed in the system prompt and can be read in full through the `read_skill` tool.

## Project Extensions

Default project extension directory:

- `Assets/Pie/Extensions`

Each extension is a `*.js` file and should register itself through `pieExtension((api) => { ... })`.

Minimal example:

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

## Sessions And Memory

Project memory:

- Stored at `Assets/Pie/AGENTS.md`
- Shared with the Unity project
- Intended for project conventions and persistent guidance

Session and runtime state:

- Stored under `Application.persistentDataPath/Pie`
- Intended for local session history and runtime-owned state

## Standalone Release Repository

`pie-unity` is developed inside the `pie` monorepo and exported to a standalone Git repository for distribution.

From the monorepo root:

```bash
npm run export:pie-unity -- \
  --target /absolute/path/to/pie-unity-release \
  --repo-url https://github.com/Cydream-Tech/PieUnity.git
```

This export flow:

- rebuilds `Resources/pie/core.js`
- copies only the distributable Unity package payload
- rewrites `package.json` for standalone distribution

## License

`pie-unity` is distributed under `Apache-2.0`.
