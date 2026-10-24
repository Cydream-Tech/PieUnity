# Pie Unity Testing

## Manual Smoke Test

Open `Tools > Pie > Pie Chat`, then run:

1. `/self-check`
2. `/session-check`
3. `/skills`
4. `/extensions`
5. `/extensions reload`

Expected behavior for slash-command bubbles (by design, not a bug):

- UI-only local report replies — the bubbles emitted through
  `emitLocalAssistantMessage` for commands like `/self-check`, `/skills`,
  `/tree`, `/resume <no-arg listing>` — are NOT persisted into the session
  record (see the session_sync gate comment in `Editor/PieChatWindow.cs`).
- After such a command on a fresh session (and provided the command neither
  restores/forks an existing session nor persists messages itself), the
  Editor UI transcript shows the report while disk keeps no session
  directory; the Sessions panel filters `messageCount > 0` and therefore
  never lists it. The report is intentionally ephemeral — restarting the
  Editor does not restore it. `/session-check` and `/self-check` also write
  `probe_*` scratch sessions; those are their own store probes, not this
  session's transcript.
- Exceptions that DO persist by design: `/resume <id>` switches to an
  existing persisted session; `/debug-stream` deliberately appends and
  persists messages like a real turn.
- If a report must survive restarts, persist it out-of-band (for example
  into a project file); do not expect it in session history.

Then send:

1. `演示一下 ask 工具`
   Expected:
   - interaction UI opens inside `Pie Chat`
   - choosing an option immediately submits it
   - long inactivity is treated as skip

2. `演示一下 todo 工具，并持续执行直到 todo 结束`
   Expected:
   - todo panel appears inside `Pie Chat`
   - checklist keeps updating
   - panel clears automatically when complete

3. `Use read_file exactly once with root=project and path=AGENTS.md`
4. `Use list_dir exactly once with root=project and path=Assets/Pie`
5. `Use grep_text exactly once with root=project, path=Assets/Pie, pattern=PieSettings, literal=true`

## Automated Reliability Regression

Prerequisites:

- Unity Editor open with the target project
- Pie dev RPC on `8091`
- UnitySkills on `8090` is optional legacy integration, not a required control plane
- `Pie Chat` configured with valid provider settings

Run:

```bash
node products/pie-unity/scripts/unity-tool-reliability.mjs
```

Optional environment variables:

- `REPEATS=8`
- `WAIT_MS=600`
- `POLL_LIMIT=150`

The script repeatedly drives `Pie Chat` through dev RPC and checks:

- `unity_project_inspect`
- `unity_script_run`
- `list_dir`
- `read_file`
- `write_file`
- `edit_file`
- `grep_text`
- `find_files`
- skill entry loading through `read_file`
- project root AGENTS.md
- `ask_user_multi`
- `manage_todo_list`
- session recovery after script reimport during an in-flight response

This is an opt-in manual debugging script, not a release or CI gate.
Editor installation, model credentials, and RPC availability are environment
prerequisites, not mandatory dependencies of the normal verification chain.
The old real-Unity reliability/deep gates have been removed.

Default verification retains the offline TypeScript behavior suite and local
bundle/package consistency checks. Use the manual smoke checklist for real
Editor, reload recovery, Play Mode, and target Player validation.

## Clean exported-package consumer (opt-in)

From the monorepo, run `npm run verify:unity:consumer -- --unity <executable>`.
This creates a NEW disposable project using the exported UPM package, not the
monorepo source or PieArcade. It validates Editor initialization, bounded explicit
binding preparation, strict macOS IL2CPP build and a PID-pinned Player Smoke.
No model calls or user games are involved. Missing prerequisites report blocked,
not passed. The check is not added to default offline/PR gates.

The monorepo's `scripts/fixtures/pie-unity-consumer/README.md` documents
prerequisites, `--allow-dirty`, retained evidence and cleanup ownership. This is
monorepo tooling; the standalone package's manual workflow follows below.

## Player Smoke (IL2CPP truth)

Editor tests cannot catch IL2CPP-only failures (stripped `CS.*` reflection
surface, base64 bridge corruption, HttpClient hangs). The package ships a
minimal real-player probe:

```bash
# 1. Use a closed test project, set its CURRENT backend to IL2CPP, and
#    explicitly prepare bindings under the isolated Smoke identity:
export PIE_PUERTS_GEN_STRICT=1
Unity -batchmode -projectPath <yourProject> -buildTarget StandaloneOSX \
  -executeMethod Pie.Editor.PiePlayerSmokeBuild.PrepareSmokeBindingsBatch -quit
# Exit 2 means compile in a fresh process and repeat preparation; only exit 0
# / PIE-SMOKE-GEN-OK allows the build. Do not replace this with host generation.

# 2. Build in a separate process, into a NEW output path:
Unity -batchmode -projectPath <yourProject> -buildTarget StandaloneOSX \
  -executeMethod Pie.Editor.PiePlayerSmokeBuild.BuildSmokePlayer \
  -pieSmokeOutput /abs/path/PieSmoke.app -quit

# 3. Launch it, then verify from the Pie monorepo (no billed calls needed):
node products/pie-unity/scripts/unity-player-smoke.mjs --instance <id-substring>

# 4. Restore host-fresh generated outputs before normal builds/commits:
Unity -batchmode -projectPath <yourProject> -buildTarget StandaloneOSX \
  -executeMethod Pie.Editor.PiePuerTSBindingGenerator.GenerateAllBatch -quit
```

Both smoke entry points restore the original productName on success and failure;
the generated bindings remain Smoke-specific until step 4. This preserves strict
fingerprint checking even for custom Configure code that reads player settings.
See [binding policy](PUERTS-BINDINGS.md#isolated-smoke-player) for exit codes and
supported targets. The Node verifier is monorepo tooling, not an exported UPM file.

The verifier checks RPC discovery, then proves the JS filesystem bridge
end to end: it installs a nonce probe skill (C#-side write), triggers
`skills_reload`, and polls `pie_smoke_selftest` until the JS-emitted skills
inventory — produced by the JS runtime scanning the skill layers through
puerts-fs → `CS.System.IO` — contains the exact `probeName`. The verifier passes
that name to the selftest and checks its bounded `probeName`/`probeFound` response,
not the truncated 4000-character `skillsListJson` diagnostic (honest,
freshness-stamped proof of the IL2CPP P1 read surface). It also asserts the self-test's own
flags (bridge pipeline, native file bridge round-trip, synchronous base64
fallback, session paths) and evaluates `unity_script_run`. A second script must return `PIE-BUFFER-OK`
after checking the PuerTS global Buffer's real bytes, base64/UTF-8 roundtrip,
concat/subarray views, zero-filled allocation and malformed UTF-8 replacement.
This checks the JS shim, independently of the C# base64 bridge flag. Model file tools
like `read_file` are intentionally not probed here: they are chat-driven
and need model credentials; the reload-scan probe covers the same
`CS.System.IO.*` read path with zero billed calls. Exit code 0 means the
Pie runtime works in that player. Probe skills (`pie-smoke-probe-*`) are
left in the player's writable skill layer and are safe to delete.

Static counterpart (no build needed): `npm run check:unity-binding-surface`
reconciles the bundle's `CS.*` usage against `Runtime/binding-surface.json`
and `Runtime/link.xml`; it also runs at the end of `check:unity-bundle-drift`.

## Offline Buffer compatibility regressions

```bash
node --import tsx --test products/pie-unity/test/unity-buffer-shim.test.ts
```

Run after `npm run build:unity`. Tests evaluate the actual shared banners from both
`.bytes` artifacts without native Buffer, TextEncoder/TextDecoder, atob/btoa or
require. They compare supported byte/encoding/view semantics with Node, run the
real image decoder and web_fetch fallback, and compile Unicode TypeScript/source
maps. The Player Buffer probe is also tested against both the real banner and a
broken length-only shim; offline tests alone do not establish native compatibility.
The supported subset and explicit non-goals are documented in the monorepo's
`docs/maintenance/unity-bridge.md`.

## Offline lifecycle and correlated binding regressions

```bash
node --import tsx --test products/pie-unity/test/safe-http-lifecycle.test.ts products/pie-unity/test/unity-public-api.test.ts
```

Run from the repository root. The HTTP/smoke test compiles the actual C# sources
with the local dotnet SDK and deterministic Unity request/pump doubles; it covers
worker cancellation followed by teardown without another frame, queued cancellation,
construction cancellation/teardown, header/start/send/dispatch failures, repeated
cleanup, normal completion, and exact probe membership after 4000 characters. It
explicitly skips the compiled harness if dotnet is absent. The JS verifier behavior
also runs offline, including description-only/foreign-name false positives.
TS binding cases cover deferred load, active turns, notfound, exceptions and late
requests/new-session persistence. These tests do not launch Unity or touch saves.

Real Unity UI-consumer probe (parent/manual run; use a closed test project,
not a second editor on the live project):

```bash
Unity -batchmode -projectPath <PieArcade-test-project> -executeMethod PieArcade.EditorTools.ChatSessionBindingChecks.RunBatch -logFile <absolute-chat-check.log>
```

The method exits Unity itself. The chat probe exercises the
actual ChatPanel result/snapshot consumers and the wire DTO without a live runner,
model calls or save writes (empty mod ids make SaveMod a no-op). Parent validation
must still compile the full Unity assemblies, rebuild the TS bundle, and exercise
session switching and UnityWebRequest teardown in the target Player.

The monorepo's `docs/maintenance/unity-bridge.md#correlated-runtime-session-binding`
documents request/result fields, legacy dispatch semantics and retry behavior.

## Accepted-turn lifecycle regressions

```bash
node --import tsx --test products/pie-unity/test/runtime-chat-turn.test.ts products/pie-unity/test/session-persistence.test.ts products/pie-unity/test/session-controller-adapter.test.ts products/pie-unity/test/unity-public-api.test.ts products/pie-unity/test/runtime-closeout.test.ts
```

The composed tests use the production public API, local command handler, session
runtime/store, projection and acceptance lease (no mocked finalizer). Temporary
session directories isolate `/new` and successful `/resume`, next-send admission,
overlap rejection, duplicate settlement, and stale persistence after retirement,
including callers reusing a `clientTurnId`. Synchronous outbound callbacks now
exercise first-user-timeline reentry (B rejected while A's full identity is
reserved), ordinary `OnTurnFinished` send/next-session reentry, and the documented
`session_switch` exception (reentry rejected until the switch returns). A real
Agent + semantic forwarding + public API + projection + lease case checks normal,
failed and aborted closeout waits for prompt-lock release before admitting B.
Healthy-persistence cancellation tests retain next-send success after cancelled
local work. Production-composed tests additionally use the real
`createUnityAgentSessionController` and `UnitySessionPersistence`: abort during
controller preflight produces zero `agent_start`/semantic terminals and exactly
one host terminal; abort after `agent_start` remains semantic-owned. Calling
`prompt()` alone must never mark a lease dispatched.

The production serializer test holds A's underlying IO indefinitely, aborts, and
sends B synchronously from A's terminal callback. B must quickly fail with
`SESSION_PERSISTENCE_UNAVAILABLE`, without queueing persistence or calling the
model; B's finalizer and new-session requests must also finish promptly. Only
after resolving A may C persist and dispatch. IO cannot be physically cancelled:
the contract is fail-closed while the abandoned tail is unresolved, **never** tail
reset or concurrent writes (save may delete/rebuild, allowing old IO to overwrite
new saves). Unit tests cover rejected-tail recovery, whole-tail availability,
and the retained session-generation guard. The production `/new` handler cannot
switch sessions after its await is cancelled. Duplicate and late semantic/finalize
events cannot close B.

The C# closeout harness compiles the actual runner/DTO and proves `nonTerminal`
request errors notify without changing A's busy/status, running tool rows,
transcript or incremental delta prefix. It also drives real `SendChatMessage`
from timeline/terminal callbacks: rejected optimistic sends restore their
snapshot before `OnRuntimeError` (busy A stays busy; session-switch callback stays
idle), ordinary terminal callback sends stay busy, and retry remains possible.
Missing `nonTerminal` continues to exercise the existing terminal-error path.
These are offline tests with deterministic stream/bridge doubles, not an Editor
or Player/PuerTS integration run.

## Runtime RPC / strict smoke regressions

`node --import tsx --test products/pie-unity/test/runtime-closeout.test.ts` compiles
production C# with deterministic Unity/generator doubles. It checks RPC completion,
failure, abort, tool continuation, retained final text, response-local deltas,
session invalidation and smoke preparation/build failure restoration. It never
weakens the binding fingerprint or invokes a provider.

Real Unity consumer probe (closed test project; no model calls or user storage):

```bash
Unity -batchmode -projectPath <PieArcade-test-project> -buildTarget StandaloneOSX \
  -executeMethod PieArcade.EditorTools.RunnerRpcChecks.RunBatch -logFile <absolute-log>
```

Expected marker: `RUNNER-RPC-CHECK-OK`. The entry exits Unity itself. Use the actual
strict prepare/build/Player workflow above to verify native linking and stamp
freshness; the offline generator double cannot prove those.

## Debugging Notes

- If a request fails, check `Editor.log` for `PieHttpBridge` lines.
- `PieHttpBridge` now logs request method, URL, status, content type, and a clipped response body.
- If ask appears hung, first check whether `interaction.get_state` is open before assuming tool failure.
- If recovery regresses, the expected behavior is:
  - original session ID is preserved
  - completed history remains visible
  - interrupted assistant output is not preserved
  - the interrupted prompt is restored to the input box
