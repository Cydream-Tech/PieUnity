# PuerTS xIl2cpp Static Bindings (pie-unity)

pie-unity ships a productized PuerTS xIl2cpp static-binding pipeline. It exists
because pure-reflection PuerTS is 3–8x slower on hot C#<->JS calls (measured in
the PieArcade dogfood player, IL2CPP, 50k ops):

| operation | reflection | generated | speedup |
| --- | --- | --- | --- |
| `Vector3.op_Addition` | 1.40 us/op | 0.30 us/op | 4.7x |
| `Quaternion.LookRotation` | 1.36 | 0.34 | 4.0x |
| `transform.position` set | 0.34 | 0.04 | 8.5x |
| `transform.position` get | 0.76 | 0.24 | 3.2x |
| `new Vector3` | 1.48 | 0.28 | 5.3x |

Reflection fallback stays alive for ungenerated types, so the binding list can
stay conservative (verified: NavMesh and ParticleSystem kept working
ungenerated in an optimized IL2CPP player).

## What the package does for you

1. **Built-in binding preset** — `PiePuerTSBindings` (`[Configure]`) lists the
   standard surface: CoreModule math/scene/resources, Physics, Audio, the
   pie-unity closed `System.*` surface (see `Runtime/binding-surface.json`),
   and the Pie bridge types. All entries are Unity 2022.3-stable APIs; newer
   APIs must be added behind version `#if`s.
2. **Install-time auto-generation** — after every domain reload, a freshness
   check runs once per domain reload and regenerates when stale on Standalone. Default ON;
   opt out with `PieSettings.autoGeneratePuerTSBindings`. Failures are
   warnings, never editor-blocking.
3. **Build gate** — IL2CPP player builds verify freshness. If stale, the gate
   self-heals (regenerates) and fails that one build with a "re-run" message
   (scripts changed, the in-flight build cannot continue). With
   `PIE_PUERTS_GEN_STRICT=1` (or `true`, recommended for CI) it fails without
   healing; install-time auto-generation and implicit define repair are also
   disabled. Explicit menu/API/batch generation is still permitted.
4. **Define rule** — `PUERTS_DISABLE_IL2CPP_OPTIMIZATION` is managed for you:
   generated bindings present => the define is removed (optimization on);
   absent => the define is added (explicit reflection fallback). A stale
   disable define plus generated code breaks compilation
   (`RegisterInfo_Gen.cs` selects legacy C# `*_Wrap` references when optimization
   is disabled, but GenV2 produces native wrappers instead); never set this define
   by hand.

## Host workflows

Manual / batch generation:

```
# Editor menu
Tools/Pie/PuerTS Bindings/Generate (xIl2cpp, full wrapper)
Tools/Pie/PuerTS Bindings/Show Status

# CLI
Unity -batchmode -projectPath <proj> -buildTarget StandaloneOSX \
  -executeMethod Pie.Editor.PiePuerTSBindingGenerator.GenerateAllBatch -quit
# Use the appropriate Standalone -buildTarget for your player.
# Exit 0: PIE-PUERTS-GEN-OK; exit 1: PIE-PUERTS-GEN-FAIL
# Exit 2: PIE-PUERTS-GEN-NOT-READY (recompile in a new process and retry)
```

Generated output goes to a dedicated PuerTS code output directory inside
`Assets` (`Assets/Gen/` by default), including `Plugins/puerts_il2cpp/`, plus
`pie-puerts-stamp.json`. External code output and the native-output defines
`CPP_OUTPUT_TO_NATIVE_SRC` / `PUERTS_CPP_OUTPUT_TO_UPM` are rejected before
GenV2 runs. Remove those defines and recompile before using this pipeline.

The stamp records a pipeline schema, assembly-qualified binding identities,
Unity/PuerTS versions, active target, player settings defines, Editor/Player
compilation defines, source hashes from `CompilationPipeline`, external DLL
reference hashes, player/package manifests, assembly definitions and PuerTS
editor resource hashes. It does not recursively hash scene assets or node_modules. This detects host API, filter and blittable configuration
changes even when the binding type list stays identical. Generated output,
script assembly binaries, native output and `.meta` files are excluded from
input hashing. Unrelated source/config edits can deliberately invalidate it.
Custom configure code that depends on arbitrary external files, environment or
network state must arrange explicit regeneration; such inputs are not inferred.

The stamp also contains a SHA-256 output manifest. Freshness requires the known
GenV2 managed files, `link.xml`, native wrapper/bridge/field files, first wrapper
shard, headers and plugin files, and an exact hash match for all non-meta files
in the output directory. Keep that directory dedicated to generated output.
Missing, edited, added or truncated outputs invalidate freshness.

**Commit the complete output directory and stamp**, not just RegisterInfo.
A matching clone can skip generation; target/toolchain/config differences can
require it again. Ignoring the directory is supported but requires generation
before each clean build. Do not assume one generation process is sufficient:
when exit 2 is returned, start a new Unity process with the same `-buildTarget`,
let it compile, and run the explicit batch entry again until exit 0. Keep strict
mode enabled during both generation and build to avoid implicit CI autoheal.
Do not proceed to the build on exit 1 or 2.

## Isolated smoke player

Smoke uses `productName=PieSmoke` so RPC identity and writable data do not collide
with the host game. The fingerprint deliberately includes ProjectSettings and
custom Configure inputs: **ordinary host bindings are not assumed valid for this
identity**. Do not disable strict mode or remove productName from the fingerprint.
Use a closed test project, set its desired backend to IL2CPP, and run separate
Unity processes with the same active Standalone target:

```bash
export PIE_PUERTS_GEN_STRICT=1
Unity -batchmode -projectPath <project> -buildTarget StandaloneOSX \
  -executeMethod Pie.Editor.PiePlayerSmokeBuild.PrepareSmokeBindingsBatch -quit
# Only proceed on exit 0 / PIE-SMOKE-GEN-OK.
# Exit 2: start a fresh Unity process and repeat preparation after compilation.
Unity -batchmode -projectPath <project> -buildTarget StandaloneOSX \
  -executeMethod Pie.Editor.PiePlayerSmokeBuild.BuildSmokePlayer \
  -pieSmokeOutput /absolute/new/path/PieSmoke.app -quit
# After smoke work, explicitly regenerate for normal host builds:
Unity -batchmode -projectPath <project> -buildTarget StandaloneOSX \
  -executeMethod Pie.Editor.PiePuerTSBindingGenerator.GenerateAllBatch -quit
```

Both smoke entry points restore the host productName before returning/exiting,
including generation failure, compile-required exit 2, and build failure. The
output directory must not already exist. Preparation intentionally writes the
normal generated-output directory with Smoke-specific bindings/stamp; those
remain Smoke-specific after productName is restored. Run normal generation until
exit 0 before returning to host builds or committing host-generated outputs.
Do not commit the transient Smoke stamp as if it were host-fresh. No commands
above launch a model or paid service.

The optional Node verifier lives in the Pie **monorepo**, not the standalone UPM
payload: `node products/pie-unity/scripts/unity-player-smoke.mjs --instance <id>`.
Launch only the newly built smoke player before invoking it. It verifies the
runtime/JS filesystem and `unity_script_run` without model credentials.

## Extending the binding list

Add your own `[Configure]` class in an Editor folder of YOUR project (PuerTS
merges all configure classes via AppDomain scan):

```csharp
[Configure]
public class MyGamePuerBindings
{
    [Binding]
    public static IEnumerable<Type> Bindings
    {
        get
        {
            yield return typeof(MyGame.FastPathComponent);
        }
    }
}
```

Rules:
- Never fork the package preset; add your own class.
- Never declare `[CodeOutputDirectory]` in a package or shared configure
  class — PuerTS resolves the output directory first-wins, so package-level
  declarations race the host. Declare it (if you must) in exactly one
  host-project configure class.
- Bindings are for TYPES callable from JS. PuerTS delegate bridges
  (`UsingAction<T>` etc.) are a separate, runtime-side declaration.

## Delegate ABI and the ungenerated Editor fallback

Type reflection and JS-to-C# delegate bridges are different contracts. In
PuerTS 2.2.2, an Editor with `PUERTS_DISABLE_IL2CPP_OPTIMIZATION` and no generated
delegate bridge can return null / throw `can not find delegate bridge` for
unregistered value-type signatures. Reflection alone does not make SSE/file
completion delivery work.

`PieBridge.RegisterDelegateSignatures` is the single runtime registration list,
called immediately after `new JsEnv`, before injection or delegate Eval. It
covers **all** generic Action/Func Eval signatures, including core message,
script/runtime-host and compiler delegates, not just SSE/file callbacks.
Parameterless `Action` (tick) is built into PuerTS 2.2.2 `GenericDelegate`; there
is no parameterless `UsingAction()` API. The same registrations stay enabled on
the default generated/xIl2cpp path (PuerTS's xIl2cpp methods are no-ops).
Do not hand-edit generated bridge code to repair this contract.

`npm run check:unity-binding-surface` also checks this source contract and boot
ordering; `player-runtime-contract.test.ts` mutation-tests missing registrations
and new signatures. These are source checks, **not real Unity execution**.
Native release verification must separately exercise:

- a fresh Editor fallback host, no generated bridge, explicit disable define,
  and auto-generation disabled: boot/core invoke/tick, compiler, SSE status/data/
  terminal and async file completion (including failure) must actually deliver;
- the default generated Editor and strict xIl2cpp smoke player, with the same
  completion paths and no define/registration regressions.

This repair has not run those native Unity checks. Fallback here means a
supported runtime registration contract for the ungenerated **Editor** path,
not a promise of reflection-only IL2CPP player/AOT support, nor permission to
bypass strict build freshness checks. Configure test-host defines deliberately;
do not leave the disable define alongside GenV2 output.

## Platform status

| platform | status |
| --- | --- |
| Standalone Windows/macOS/Linux IL2CPP | allowed target family; dogfood validation was macOS, Unity 6000.0.67f1; Windows/Linux still need platform verification |
| Editor (Mono) | reflection path requires the runtime delegate ABI registrations above; ungenerated fallback native regression still needs verification |
| iOS / WebGL | **unsupported** by this product pipeline; implicit generation warns/skips, explicit generation and IL2CPP builds fail actionably |
| Consoles / other targets | **unsupported** until separately validated |

There is no guaranteed reflection fallback for unsupported targets. In particular,
iOS registration can reference legacy `*_Wrap` classes which GenV2 does not
produce. Switching target requires recompilation and regeneration; do not reuse
Standalone output as an iOS binding solution.

Minimum supported Unity remains 2022.3; the generation path is validated on
6000.0.67f1 locally (a 2022.3 generation check is a known CI gap).

## Troubleshooting

- Batch sessions with `-quit` and no `-executeMethod` may quit before the
  editor delayCall queue runs, so auto-generation does not fire there. Use
  the explicit batch entry (`GenerateAllBatch`) in CI; auto-generation covers
  interactive editor sessions.
- Generation while `PUERTS_DISABLE_IL2CPP_OPTIMIZATION` is still defined
  produces a REDUCED wrapper set (PuerTS compiles `ArrayExtension` as an
  empty stub under the define, so array extension wrappers vanish). The stamp
  fingerprint records the define state, so such output is stale by
  definition: pass 1 generates + removes the define (+recompile), pass 2
  regenerates with the full generator. Hosts carrying the legacy define heal
  automatically after recompilation when strict mode is off. Batch generation
  reports NOT-READY (exit 2), never OK, for the define transition; retry in a
  new process after compilation. Further target/config changes can need more retries.
- `error CS0246: UnityEngine_Vector3_Wrap ...` after generation: a stale
  `PUERTS_DISABLE_IL2CPP_OPTIMIZATION` define is present. The auto-generate
  pass and the build gate can heal this on Standalone outside strict mode;
  strict mode requires explicit generation. Manual fix: remove the define from
  Project Settings > Player > Scripting Define Symbols.
- Generation appears to do nothing in a fresh clone: check
  `PieSettings.autoGeneratePuerTSBindings` and
  Tools/Pie/PuerTS Bindings/Show Status.
- Builds fail in CI with the strict message: regenerate and commit
  `Assets/Gen/`, or run the batch entry before the build step.
