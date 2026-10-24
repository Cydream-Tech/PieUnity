# com.pie.agent

`com.pie.agent` is the Pie Unity host package built on top of [PuerTS](https://github.com/Tencent/puerts) V8.
It uses a runtime-first architecture: the agent core runs in Runtime, while the Editor provides a chat UI and authoring workflow.

## Highlights

- Streaming chat and tool execution inside Unity
- Runtime and Editor share the same agent core
- Project assets live under `Assets/Pie`
- Runtime state lives under `Application.persistentDataPath/Pie`
- Project skills, project extensions, and project AGENTS.md context are first-class package concepts

## Attention consumer caveat

Unity forwards shared `attention_requested` / `attention_resolved` events through `PieBridge.OnJsEvent`. This is a semantic bridge, **not a native attention presenter**: consumers must supply any in-game bubbles, badges, audio or OS notifications. Ordinary completion is explicitly **OFF** in production, with no supported host toggle. The low-level `user-initiated` coordinator policy requires caller-owned accepted-delivery bookkeeping not supplied by the production controller. Generic confirmation is input (explicit `false` is an answer); timeout, skipped, unavailable and missing responses cancel attention consistently with CLI. Only the interaction owner resolves requests; suppressing presentation must not resolve them. The monorepo's `docs/public-api/unity-host-adapter.md#attention-events-experimental` records the underlying host contract.

## Unity RPC Host

`com.pie.agent` exposes a local RPC/REST host for agent-driven Unity work.

Stable HTTP entrypoints:

- `GET /health`
- `GET /instances`
- `GET /manifest`
- `POST /tool/{name}`
- `POST /rpc/{method}`

Recommended host tools:

- `unity_scene_query`
- `unity_scene_object_inspect`
- `unity_scene_object_edit`
- `unity_refresh`
- `unity_log_read`
- `unity_script_run`

Tool naming rule:

- all model-visible tool names are lowercase
- only `a-z`, `0-9`, and `_` are used
- do not assume dot-delimited names like `unity.query`

This keeps provider compatibility predictable across OpenAI-compatible gateways and other hosts.

### Tool taxonomy / MECE boundary

PieChat's default agent surface should use one primary tool per concept:

- Files: `read_file`, `write_file`, `edit_file`, `list_dir`
- Search: `find_files`, `grep_text`
- Skills: prompt-indexed files consumed through `read_file`, `grep_text`, `find_files`, and `bash`
- Unity project/runtime context: `unity_project_inspect`
- Unity scene reads: `unity_scene_query`, `unity_scene_object_inspect`
- Unity scene object edits: `unity_scene_object_edit`
- Unity script tasks: `unity_script_run`
- Unity diagnostics: `unity_log_read`
- Interaction/state: `ask_user_multi`, `manage_todo_list`

Dev RPC no longer exposes duplicate project-file, target/result, skill, or editor convenience tools as primary manifest entries. External helper scripts should use local filesystem access for Unity project files and call `unity_refresh` when Unity must import changed assets; inside PieChat, prefer the normal file tools with project-root resolution.

`unity_scene_object_edit` is a scene object edit patch tool, including common component lifecycle/property patches such as add/remove/enable/set public field or property. Reads belong to `unity_scene_query` / `unity_scene_object_inspect`; logs belong to `unity_log_read`; short multi-frame Unity composition belongs to `unity_script_run`.

Script waits and `ctx.frame` advance once per Unity frame in Play Mode, and once
per host update outside Play Mode. `maxFrames` includes time spent waiting in
`waitFrames` and `waitSeconds`; it is not a count of completed waits. Cancellation
stops subsequent script edits but preserves changes already applied. Use explicit
`ctx.rollback()` to remove objects created by the task; it is not a general undo
for changes to pre-existing objects.

## Editor chat formatting

Pie Chat renders user messages and assistant replies with headings, paragraphs, bold/italic text,
inline code, fenced code blocks, lists/task lists, quotes, dividers, and simple
pipe tables. Links show their label and URL; images/HTML and full CommonMark
nesting are not supported. Unclosed code fences remain readable while streaming.

Tool results and thinking are collapsed by default. Expand a tool to inspect its
arguments and full result. Message Copy preserves the original Markdown; code
blocks have their own Copy button. Text wraps to the panel width and uses measured
font layout, including Chinese text. This presentation layer is Editor-only and
does not alter model output or persisted session content.

Editor session sync restores tool arguments and details directly from the full
session messages, independently of the runtime timeline's 50-turn/500-item limit.
Search tools use the same effective-argument normalization during live execution
and history restoration; the sync payload also retains the original arguments.

For repeatable real-Unity script coverage, see the monorepo's
`scripts/fixtures/pie-unity-script-game/README.md` (Three Sparks fixture).
It covers Editor, Play Mode, and a standalone macOS IL2CPP Development Player.

## Host Responsibility

`ARCHITECTURE.md` is the shared architecture source of truth for Pie.

`com.pie.agent` is responsible for mapping shared Pie semantics into Unity-native product experiences:

- shared commands become Unity buttons, menus, or runtime actions
- shared tool and status semantics become Unity panels, logs, or widgets
- shared interaction requests become Unity dialogs, selections, or custom editor/runtime flows

Unity can and should provide host-specific UX and richer workflows, but it should not redefine shared protocol meaning or push Unity-shaped UI APIs back into the shared layer.

Example:

- Positive: a shared `command` can be rendered as a toolbar action or menu item in Unity
- Negative: Unity-specific dialog or widget APIs should not become the shared cross-host contract

### PieChat reload / resume lifecycle guardrails

PieChat reload recovery is intentionally interrupt-and-resume, not transparent network-stream resume.

- Before Unity assembly reload, abort the active turn, cancel C# HTTP/file bridge work, close the Dev RPC listener, and wait only for a bounded time.
- After reload, create a fresh bridge and load the interrupted session state. Do not continue the old streaming response or reuse old AppDomain worker tasks.
- `SESSION_RECOVERY_WAS_STREAMING=true` means "the previous turn was interrupted by reload"; it does not mean "resume the old HTTP stream".
- `GET /health` must stay a readiness probe. It may check bounded main-thread responsiveness and request counts, but it must not call into PuerTS/JS or run agent/session code. Calling PuerTS from health previously caused native crash/reload hangs.
- During reload, connection refused, transient unavailable, or bounded `main_thread_timeout` health responses are acceptable. A socket that accepts requests but blocks indefinitely is not acceptable.
- PieChat rejects a second `send` while a turn is already busy; do not queue overlapping streaming turns unless a real queue is designed.
- Future `AgentSessionController` reuse must go through a Unity adapter. The adapter has to keep Unity's pending-user recovery as the source of truth before delegating to controller persistence, otherwise reload recovery can duplicate the interrupted user message.

### Embedding PuerTS JavaScript: IL2CPP pitfalls

Hosts that run their own game/mod JavaScript on the embedded PuerTS VM
(as PieRunner and mod hosts such as PieArcade do) should know these four
pitfalls. All were observed live while debugging IL2CPP players; only the
linker stripping in pitfall 2 is build-specific — the rest are general
PuerTS/rendering traps that can bite in the editor too:

1. **PuerTS has no JavaScript operator overloading.** `vectorA - vectorB`
   does not throw — it is plain JS coercion and silently produces `NaN`.
   In the observed case the NaN flowed into `transform.eulerAngles` and
   the rotation simply never changed, with no error anywhere. Use the
   bound static operators (`UnityEngine.Vector3.op_Subtraction(a, b)`)
   or component-wise math (`new UnityEngine.Vector3(a.x - b.x, ...)`), and
   treat any unexplained frozen transform as a NaN suspect first.
2. **Reflection-called engine overloads are stripped by IL2CPP.** Host C#
   almost never references every overload of e.g. `Quaternion.LookRotation`,
   so the linker removes the ones only JS calls. `Runtime/link.xml` ships a
   `UnityEngine.CoreModule` preservation list for the math/helper surface
   (Vector/Quaternion/Matrix/Mathf/Color/ColorUtility/Rect/Bounds/Random/Time);
   extend your own `link.xml` with the same pattern when your JS reflects
   over further engine APIs.
3. **Linear color space changes what code-authored colors mean.** A
   `Color` is an untagged numeric value; consumers that expect linear
   light (shaders, rendering in a `PlayerSettings.colorSpace = Linear`
   project) will render an intended sRGB `0.05` as ~`0.26`, washing the
   whole UI out. Convert with `.linear` at the boundary where a
   linear-expecting consumer receives the color — and only there, so
   consumers that already convert are not double-corrected. Captures had
   the same class of bug: `unity_screenshot` PNGs from a linear project
   carried bytes/metadata that disagreed with the display-referred sRGB a
   PNG must hold, and framebuffer alpha of 0 made image viewers composite
   captures onto white — two independent causes of the same washed-out
   look. The tool now encodes opaque sRGB PNGs in both color spaces.
4. **Player logs are reachable over RPC.** When a player build misbehaves,
   call `unity_log_read` (`source: "player"` or `"active"`) instead of
   hunting `Player.log` by hand; `contains` + `tailLines` cover the usual
   triage. For scene captures that must exclude chat/HUD overlay canvases,
   call `unity_screenshot` with `includeUI: false`.

### Static bindings and reusable mod hosting

See [PUERTS-BINDINGS.md](PUERTS-BINDINGS.md) for the Standalone xIl2cpp preset,
install-time generation, freshness stamp and strict CI build gate. Other player
targets are not supported by this new generation pipeline yet.

`PieModHost<TApi>` runs trusted plain-script mods on a caller-owned `JsEnv`.
It caches `game_init(api)`, optional `game_update(api, dt)`,
`game_onKey(api, key)` and `game_dispose()` delegates. Install guards with
`InstallStepGuardsFromResources()` and check its result **before loading mods**.
The bundled `pie/mod-guards` resource uses the same Acorn loop injector as
`unity_script_run`; hosts no longer need to ship their own copy.

- Budgets cover top-level evaluation/init (30s), updates (2s), keys (2s) and
  cleanup (2s). Hosts can tune the public budget fields and update budget via
  `PerfTool`. These are cooperative limits, **not a sandbox or hard VM timeout**:
  native calls, recursion, dynamic code or deliberately caught errors can evade
  termination. Only use trusted mods; a missing guard installation is reported
  but does not itself prohibit `LoadMod`.
- Loading unloads the previous hooks; failed initialization also gets best-effort
  cleanup. Hook globals are reset between loads, but arbitrary mod globals share
  the VM. Use top-level `var`, not redeclarable `let`/`const`, for reloadable state.
- `ModFailed` fires after teardown, with a phase and error. The caller owns scene/UI
  cleanup, `JsEnv.Tick()` and disposal. Unload the mod before disposing its VM.
- Telemetry exposes a 600-sample rolling percentile window plus total frame/jank
  and budget-abort counters since reset. The first two update ticks execute but
  are excluded as warmup; a loading overlay can wait for `IsWarmingUp == false`.
  `PerfTool({"reset":true})` returns the old snapshot **before** resetting it.
- Agent and mod work share Unity's main thread: budgets detect runaway code, not
  schedule independent CPU quotas. Batch/ArrayBuffer channels remain unshipped;
  the dogfood's 600-entity update measured p95 0.12ms without them.

## Requirements

`com.pie.agent` depends on PuerTS Unity 2.2.2 with the V8 backend:

- Install `com.tencent.puerts.core` version `2.2.2`.
- Use the PuerTS V8 backend/native plugin for your target platform.
- Do not install or depend on `PuerTS_Agent`, `Puerts.AI`, or `com.tencent.puerts.agent`; pie-unity owns the agent host layer.
- Do not use remote `https://*.tar.gz` dependencies or git tag/path dependencies for PuerTS in release validation.

Recommended `manifest.json` dependency shape:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.tencent.puerts.core"]
    }
  ],
  "dependencies": {
    "com.tencent.puerts.core": "2.2.2"
  }
}
```

The exported `com.pie.agent` package manifest also declares `com.tencent.puerts.core`
`2.2.2` as a Unity Package Manager dependency.

If PuerTS 2.2.2 or its V8 runtime is missing, `PieBridge` will not initialize and neither `Pie Chat` nor `PieRunner` will work.

## First Run Validation

After installation:

1. Open `Tools > Pie > Pie Chat`.
2. Run `/self-check` and confirm the model, tool route, web search, and file-understanding sections are healthy.
3. Send a short message and verify a response appears in PieChat.
4. Run a Unity scene query through PieChat.
5. Trigger an assembly reload and confirm PieChat reconnects without duplicating the interrupted prompt.

Optional local package checks (no Unity Editor or live RPC required):

```bash
npm run verify:unity:package-release
```

## Generated Runtime Bundle Policy

The shipped runtime bridge is a tracked Unity `TextAsset` contract, not an ad-hoc local file.

- Canonical runtime asset: `Resources/pie/core.bytes`
- Required tracked generated artifacts: `core.bytes`, `core.bytes.meta`, `typescript-compiler.bytes`, `typescript-compiler.bytes.meta`
- Source maps are not shipped or tracked in release artifacts
- `core.js` is not a supported shipped runtime asset and must not appear in source or exported packages
- Run `npm run build:unity` after changing `src/` or the Unity bundle build pipeline
- Run `npm run check:unity-bundle-drift` to confirm the tracked generated bundle stays stable after rebuild
- `npm run verify:unity:package-release` is an optional local package check: it builds, checks bundle policy/drift, exports a standalone package, and checks static payload/dependency contracts. It does not launch Unity or resolve UPM dependencies.

`unity_script_run` is a cooperative step-bounded runner: define `export async function run(ctx, args)`, await frame boundaries and host calls, and expect long synchronous loops to fail with `STEP_TIMEOUT` after a guard check rather than being arbitrarily preempted.

Runtime debugging is zero-registration by default. When a PlayMode/runtime host is alive, discoverability prefers the runtime owner, and `unity_script_run` exposes `ctx.runtime` for controlled same-`JsEnv` inspection and path-based calls such as `ctx.runtime.call("IJsEnvironment.Environment.ReloadAllMods", [])`. Prefer `ctx.runtime` over assuming raw globals like `VX`, `CS`, or `puer` are part of the public contract.

## Behavior Trees

`com.pie.agent` includes a runtime behavior-tree capability backed by a vendored source copy of NPBehave. Agents create controlled JSON behavior-tree specs, games write runtime context into typed blackboard entries, and leaf `action` nodes execute only game-side receivers registered through `PieBehaviorTreeActionRegistry`.

Manifest namespace:

- `behavior_tree`

Stable tools:

- `behavior_tree_create`
- `behavior_tree_start`
- `behavior_tree_stop`
- `behavior_tree_destroy`
- `behavior_tree_status`
- `behavior_tree_set_blackboard`
- `behavior_tree_get_blackboard`

Use the packaged `pie-unity-behavior-tree` skill for agent workflow guidance. In PieChat, call these tools through `unity_tool_call` after inspecting the manifest. For smoke tests, the built-in action receiver `pie.debug.record_action` records execution in status/log output and updates the tree blackboard key `lastDebugAction`.

Long-running strategies should use the JSON `repeat` node with an explicit `intervalSeconds`, and fast leaf actions can set `minIntervalSeconds` to prevent accidental action spam. `condition.stopsOnChange` defaults to `none`; set `immediate_restart` only for branches that must interrupt lower-priority work.

Runtime health exposes both `runtimeActive` and `playModeActive`. `runtimeActive` means a Pie runtime owner is registered; it is not the same as Unity Play Mode. Use `playModeActive` and `behaviorTreeActiveCount` when validating Play Mode behavior-tree dogfood. External scripts should call through the packaged RPC helper, or implement the same one-shot token refresh on `RPC_UNAUTHORIZED` after domain reload or Play Mode transitions.

## Installation

Install through Unity Package Manager:

- Local monorepo development:
  `Add package from disk...` and select `products/pie-unity/package.json`
- Git release repository:
  `Add package from git URL...`
- Current public Git URL:
  `https://github.com/Cydream-Tech/PieUnity.git`

After installation, you can import the `Extension Demo` sample from the Package Manager Samples section.

## Quick Start

### Editor

1. Open `Tools > Pie > Pie Chat`
2. Enter your `apiKey`, `provider`, `model`, and optional `baseUrl`
3. Start chatting

For CLI or agent-driven Unity control, use the package-contained RPC skill from
the current Unity project's resolved `com.pie.agent` package. Prefer the copy at
`Packages/com.pie.agent/Skills/pie-unity-rpc`, or the matching
`Library/PackageCache/com.pie.agent*/Skills/pie-unity-rpc` entry when Unity
resolves the package into the cache. The CLI no longer ships or installs a
global `pie-unity-rpc` copy; `com.pie.agent` is the canonical source.

Then run:

```bash
node <resolved-com.pie.agent>/Skills/pie-unity-rpc/pie-unity-rpc.js health --project "/abs/path/to/project"
node <resolved-com.pie.agent>/Skills/pie-unity-rpc/pie-unity-rpc.js manifest --project "/abs/path/to/project" --namespace unity
node <resolved-com.pie.agent>/Skills/pie-unity-rpc/pie-unity-rpc.js tool --tool chat_send --data '{"text":"respond with exactly pong"}'
node <resolved-com.pie.agent>/Skills/pie-unity-rpc/pie-unity-rpc.js tool --tool chat_get_state
```

Agents should inspect `/manifest` first and choose a host by the capability
needed for the user's task. When a runtime host is alive, pie-unity exposes the
runtime owner as the primary discoverable instance so runtime debugging does not
get preempted by editor heartbeats. Prefer project namespaces such as `voxmod`
over generic Unity editing tools when they are available, but zero-registration
runtime inspection and calls are available through `unity_script_run` via
`ctx.runtime`. Do not edit
prefab YAML or serialized object references directly when an RPC tool exists.

Available local commands include:

- `/new`
- `/resume <sessionId>`
- `/tree`
- `/fork` or `/fork <sessionId>`
- `/self-check`
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
4. Call `SendChatMessage(...)`
5. Listen to `OnAssistantMessage`, `OnAssistantDelta`, `OnToolStart`, `OnToolEnd`, and `OnError`

Runtime chat UIs can use the additive structured API without parsing those
legacy string events:

```csharp
runner.OnTimelineItemChanged += item => Debug.Log($"{item.kind}: {item.summary}");
runner.OnSessionChanged += snapshot => Debug.Log($"Session: {snapshot.id}");
runner.OnStatusChanged += status => Debug.Log(status.phase);
runner.OnTurnFinished += result => Debug.Log(result.outcome);
runner.OnRuntimeError += error => Debug.LogError($"{error.code}: {error.message}");

runner.SendChatMessage(new Pie.PieChatRequest
{
    content = "Full model-facing runtime contract and prompt",
    displayContent = "Create a voxel item",
    clientTurnId = "operation-42",
});

// Restores the persisted messages and emits a complete PieSessionSnapshot.
runner.ResumeSession("sess_xxx");
```

`displayContent` is persisted with the user message and is used for the
player-facing timeline and session title. `content` remains the model input.
The `send_message` wire action rejects an overlapping accepted turn with
`SESSION_TURN_ACTIVE` before starting another one; terminal finalization is
fenced to the accepted `clientTurnId`. Use an explicit host queue until a
public queue/steering contract exists.
One session snapshot contains at most the latest 50 turns / 500 timeline
items; thinking blocks are never projected into the runtime timeline.

`ResumeSession` / `NewSession` return **dispatch success, not binding completion**.
For per-mod/workspace binding, subscribe to `OnSessionBindingResult` and pass a
fresh request id via `ResumeSession(id, false, requestId)` or `NewSession(requestId)`.
Only a matching successful result authorizes saving its `sessionId` or sending on
that binding; an arbitrary `OnSessionChanged` snapshot does not. The monorepo's
`docs/maintenance/unity-bridge.md#correlated-runtime-session-binding` documents
result fields, supersession and retry semantics. Legacy overloads remain supported.

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
        runner.SetConfig("YOUR_API_KEY", "openai", "gpt-4.1-mini", "https://api.openai.com/v1");
        runner.OnAssistantMessage += msg => Debug.Log("AI: " + msg);
        runner.SendChatMessage("Create a cube at (0,1,0).");
    }
}
```

Runtime voice input is exposed as a static Begin/End API. It does not require
adding a voice component to the scene:

For macOS Player builds, set **Player Settings > Other Settings > Microphone Usage
Description** to your application's explanation of microphone use. Pie preserves
its runtime assembly for PuerTS/IL2CPP, which also retains the voice API; Unity
requires this description even when the build's scene does not start a recording.

```csharp
Pie.PieVoice.RequestMicrophonePermission(granted =>
{
    if (!granted) return;

    var recording = Pie.PieVoice.BeginRecording(new Pie.PieVoiceRequest
    {
        VirtualKey = voiceVirtualKey,
        Mode = Pie.PieVoiceMode.Structure,
        ContextHint = Pie.PieVoiceContextHint.Task,
        Tone = Pie.PieVoiceTone.Concise,
    }, result =>
    {
        if (result.Ok)
            Pie.PieRunner.ActiveRunner?.SendChatMessage(result.PolishedText);
    });

    // Call recording.End() when the player releases the talk button.
});
```

Runtime TypeScript generation is supported through a lazy compiler bundle. The
first version is intentionally single-file: it transpiles TypeScript to
JavaScript/MJS without Node.js, npm resolution, or runtime imports.

```csharp
var result = Pie.PieTypeScript.CompileFile(new Pie.PieTypeScriptCompileRequest
{
    SourcePath = Path.Combine(Application.dataPath, "Pie/Generated/demo.ts"),
    OutputPath = Path.Combine(Application.dataPath, "Pie/Extensions/demo.mjs"),
    Module = Pie.PieTypeScriptModule.ESNext,
    Target = Pie.PieTypeScriptTarget.ES2022,
});

if (!result.Ok)
    Debug.LogError(result.ErrorMessage);
```

Files under configured Pie extension paths may use `.js`, `.mjs`, or `.ts`.
TypeScript extensions are compiled in memory before activation. Runtime imports
such as `import lodash from "lodash"` are rejected; generate a single-file
module or inline dependencies for now.

`pie-unity` now reads models only from `~/.pie/models.json`. The file must use top-level `profiles`, not `providers`, and each profile must explicitly declare:

- `api`
- `baseUrl`
- `apiKey` or `apiKeyEnv`
- `models`

Responses profiles may use `api: "openai-responses"` with optional profile/model
`transport: "sse" | "websocket" | "auto"` (default SSE). Enable
`compat.responsesContinuation: true` for verified same-connection WS continuation.
Model transport overrides the profile. Editor and native Player both use
`PieWebSocketBridge` / `UnityWebSocketClient`; WebGL is unsupported. Runtime
pause and bridge disposal cancel native connections. Unknown native handshake
failures are surfaced because ClientWebSocket does not reliably expose an HTTP
status for safe auto fallback. The monorepo's
`docs/maintenance/responses-transport.md` records tested platforms, isolated
smoke commands and live-test limitations.

If no valid profile is configured, Unity starts in an unconfigured model state and asks you to configure `~/.pie/models.json` before sending messages.

Android runtime discovery and Pie logs are also stored below `Application.persistentDataPath`; Android's `UserProfile` location is not assumed to be writable. For Player builds, the package's Editor linker processor explicitly supplies `Runtime/link.xml` to UnityLinker, preserving the PuerTS-visible bridge surface that embedded JavaScript calls through `CS.Pie.*` on IL2CPP.

Use Pie's [PuerTS xIl2cpp binding workflow](PUERTS-BINDINGS.md), shipped with the
package. Generate full-wrapper bindings through **Tools > Pie > PuerTS Bindings >
Generate (xIl2cpp, full wrapper)**, or explicitly in batch mode:

```bash
Unity -batchmode -projectPath <project> -buildTarget StandaloneOSX \
  -executeMethod Pie.Editor.PiePuerTSBindingGenerator.GenerateAllBatch -quit
```

Use the same active Standalone target for generation and the separate Player
build. Exit 0 means ready; exit 2 requires a new Unity process to compile and
repeat generation; exit 1 is failure. Strict CI (`PIE_PUERTS_GEN_STRICT=1`)
never silently regenerates stale bindings. Pie manages
`PUERTS_DISABLE_IL2CPP_OPTIMIZATION`; do not set it manually or substitute raw
PuerTS generator commands, which do not produce Pie's validated stamp.

The productized IL2CPP pipeline currently supports active Standalone targets
(macOS validated; Windows/Linux need platform validation). iOS, WebGL and other
non-Standalone IL2CPP targets are unsupported; do not infer support from the
upstream PuerTS defaults. For the isolated PieSmoke player, explicitly run
`Pie.Editor.PiePlayerSmokeBuild.PrepareSmokeBindingsBatch` before building; see
the [Smoke workflow](PUERTS-BINDINGS.md#isolated-smoke-player).

## Paths

Project assets:

- `Assets/Pie/PieSettings.asset`
- `Assets/Pie/Skills`
- `Assets/Pie/Extensions`
- `AGENTS.md`

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
- `File Tool Roots`
- `Default File Tool Root Editor`
- `Default File Tool Root Runtime`

File tool roots let you expose named filesystem roots to `read_file`, `write_file`, `edit_file`, `list_dir`, `grep_text`, and `find_files`.

Built-in roots:

- `persistent`: maps to `Application.persistentDataPath`
- `project`: maps to the Unity project root and is Editor-only

Each configured root can define:

- `Name`
- `Description`
- `Relative Path From Project Root` or `Absolute Path`
- `Available In Editor`
- `Available In Runtime`
- `Default Prefixes`

Paths are scanned in order, and later paths override earlier items with the same skill or extension name.

Connection settings:

- `apiKey` authenticates with the selected provider
- `provider` selects the built-in API family
- `model` selects the model ID
- `baseUrl` is optional; when set, it overrides the built-in provider endpoint

When changing config through `pie_chat.set_config` from the CLI helper, the helper waits briefly for the Unity-side bridge to apply the new settings before returning.

## Skills

Default project skill directory:

- `Assets/Pie/Skills`

Supported layouts:

- `Assets/Pie/Skills/<skill-name>/SKILL.md`
- `Assets/Pie/Skills/<skill-name>.md`

Skills are listed in the system prompt with ordinary `entry` and `baseDir` paths. Read the entry with `read_file`; resolve skill-local relative paths against `baseDir` and use ordinary file or bash tools.

### Player builds

Player builds bake skills into `Assets/Resources/Pie/SkillManifest.asset` at
build time (`pie-delivery:` frontmatter selects all / player-only /
editor-only). The runtime inlines the SKILL.md body into the system prompt
and, when the skill directory carries companion files (scripts,
references — caps: 32 files, 96KB each, 512KB per skill), serves them as
virtual files under `pie-skill/<name>/<relative-path>`; `read_file` and the
other file tools resolve those paths normally.

Skills can also be installed at runtime into the player-writable layer
(`<persistentDataPath>/.agents/skills/<name>/SKILL.md`) and picked up with
`reload_skills` — via the dev-RPC tools `skills_install` /
`skills_reload` (tool kind: `pie-unity-rpc.js tool --tool skills_install
--data '{"name":...,"content":...}'`), or `CS.Pie.PieRunner.ActiveRunner.InstallSkill`
from C#. These register only when a runtime host (PieRunner) is present.
Filesystem entries win over baked manifest entries with the same name, so a
hot-installed skill overrides the baked one.

Lint skills before shipping them to a build:

```bash
node products/pie-unity/Skills/pie-unity-rpc/pie-unity-rpc.js skill-lint \
  --file Assets/Pie/Skills/my-skill/SKILL.md            # static checks
node products/pie-unity/Skills/pie-unity-rpc/pie-unity-rpc.js skill-lint \
  --file Assets/Pie/Skills/my-skill/SKILL.md --probe --project .  # + live runtime path check
```

The baker also warns (build log) when a skill calls `runtime.call` without
the required `CS.` prefix.

### Player model configuration

Player builds resolve their model endpoint, left to right by priority:

1. `SetConfig(apiKey, provider, model, baseUrl)` on the active runner — C#
   method; call it from your own bootstrap/UI code (in the editor the
   `pie_chat.set_config` RPC reaches the same path). Configuration pushed
   this way lives for the process lifetime.
2. `~/.pie/models.json` on the machine running the player — the same
   profiles the CLI uses (`profiles` with `baseUrl`/`apiKey`/`models`).

Distribution stays open by design: decide yourself whether shipped players
embed a gateway URL/key via `SetConfig` at startup or read the local
`models.json`.

## Project Extensions

Default project extension directory:

- `Assets/Pie/Extensions`

Each extension is a `*.js` file and should export a module function. Use either:

- `module.exports = (api) => { ... }`
- `export default function (api) { ... }`

The `api` object is a Unity host binding over `@pie/agent-framework`'s runtime extension contract. It includes the shared runtime fields:

- `manifest`
- `dir`
- `config`
- `log()`
- `registerTool()`
- `registerBeforeToolCall()`
- `registerAfterToolCall()`
- `registerOnError()`
- `appendSystemPrompt()`
- `callHost()`

Unity also adds:

- `projectRoot`
- `isEditor`
- `host.listCapabilities(namespace?)`
- `host.call(namespace, name, args?)`

Minimal example:

```js
module.exports = (api) => {
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
};
```

## Recommended Smoke Test

After opening `Tools > Pie > Pie Chat`, run these local commands in order:

1. `/self-check`
2. `/skills`
3. `/extensions`
4. `/extensions reload`
5. `/session-check`

Then send two real prompts:

1. `演示一下 ask 工具`
   Expected: Unity should show an interaction UI instead of appearing to hang.
2. `演示一下 todo 工具，并持续执行直到 todo 结束`
   Expected: todo state should progress and clear when complete.

For repeatable regression steps and reliability runs, see:

- [TESTING.md](TESTING.md) (included in standalone package exports)

## Sessions And Memory

Project context:

- Stored at the Unity project root as `AGENTS.md`
- Shared with the Unity project
- Intended for project conventions and persistent guidance
- `/init` creates or replaces the root `AGENTS.md` and immediately refreshes the active PieChat system prompt
- `/memory` reads the root `AGENTS.md`

Session and runtime state:

- Stored under `Application.persistentDataPath/Pie`
- Intended for local session history and runtime-owned state

Migration note:

- `read_project_memory` and `write_project_memory` have been removed.
- Use `/init`, `/memory`, or normal file tools such as `read_file` and `write_file` against `AGENTS.md`.

## Standalone Release Repository

`pie-unity` is developed inside the `pie` monorepo and exported to a standalone Git repository for distribution.

From the monorepo root:

```bash
npm run export:pie-unity -- \
  --target /absolute/path/to/pie-unity-release \
  --repo-url https://github.com/Cydream-Tech/PieUnity.git
```

This export flow:

- rebuilds `Resources/pie/core.bytes`
- copies only the distributable Unity package payload
- rewrites `package.json` for standalone distribution

## License

`pie-unity` is distributed under `Apache-2.0`.
