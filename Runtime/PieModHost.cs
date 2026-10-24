using System;
using Puerts;
using UnityEngine;

namespace Pie
{
    /// <summary>
    /// Host-neutral host for plain-script JS mods on a PuerTS JsEnv: evaluates a
    /// mod, caches its hook delegates, arms a step-budget guard around every
    /// invocation, and records a rolling frame-time window for perf tooling.
    ///
    /// Mod contract (plain script, no import/export): define global functions
    ///   function game_init(api)        - build the world; called once per load
    ///   function game_update(api, dt)  - every frame, dt in seconds (optional)
    ///   function game_onKey(api, key)  - key names as the host defines (optional)
    ///   function game_dispose()        - cleanup internal state (optional)
    /// State between reloads must live in `var`-declared globals only (top-level
    /// let/const break re-evaluation on hot reload).
    ///
    /// Step guard: InstallStepGuards* evals the bundled acorn-based injector
    /// (Resources/pie/mod-guards.bytes) and caches its entry points. With guards
    /// installed, every loop body in the mod calls __pieStepGuard(); when an armed
    /// budget deadline passes, the guard throws an error whose message contains
    /// "PIE_STEP_BUDGET_EXCEEDED". Without the guard a mod with an infinite loop
    /// hangs the whole player on the main thread. The guard cannot preempt
    /// synchronous native calls.
    ///
    /// Budgets: init gets InitBudgetSeconds (default 30s — first load may stream
    /// assets), update ticks get UpdateBudgetSeconds (default 2s; retunable via
    /// PerfTool {"budgetMs": N}). Keys and disposal have separate 2s budgets.
    /// This is cooperative protection for trusted mods, not a security sandbox:
    /// recursion, dynamic code and native calls cannot be forcibly preempted.
    ///
    /// Telemetry: a 600-frame ring (~10s at 60fps) of update-tick milliseconds,
    /// jank frames (>8ms) and budget aborts. The first WarmupFrames ticks after
    /// each load are executed (guarded) but not recorded, so JIT/first-tick
    /// spikes do not pollute the window. PerfTool is report-first: it builds the
    /// snapshot BEFORE applying reset/budget changes, so {"reset":true} returns
    /// the window that was just measured and then clears it.
    ///
    /// Failure: on any hook exception the mod is disposed best-effort, hooks are
    /// cleared, LastError is set and ModFailed fires with the phase ("init",
    /// "update", "onKey"; a "-budget" suffix marks guard aborts). The host owns
    /// all UI teardown in response.
    /// </summary>
    public sealed class PieModHost<TApi> where TApi : class
    {
        public const string StepBudgetErrorMarker = "PIE_STEP_BUDGET_EXCEEDED";
        public const string DefaultGuardsResourcePath = "pie/mod-guards";
        public const int PerfRingSize = 600; // ~10s at 60fps

        private readonly JsEnv _env;
        private TApi _api;

        private Action<TApi> _fnInit;
        private Action<TApi, float> _fnUpdate;
        private Action<TApi, string> _fnOnKey;
        private Action _fnDispose;
        private readonly Action<float> _resetJsHooks;

        private bool _guardsReady;
        private Func<string, string> _fnInjectGuards;
        private Action<float> _fnArmGuard;
        // Bound as Action<float> (parameterless JS functions accept the extra
        // argument) because PuerTS has no UsingAction() for plain Action.
        private Action<float> _fnDisarmGuard;

        /// <summary>Init-hook budget in seconds. Default 30 (first load may stream assets).</summary>
        public float InitBudgetSeconds = 30f;
        public float KeyBudgetSeconds = 2f;
        public float DisposeBudgetSeconds = 2f;
        /// <summary>Update ticks executed but not recorded after each load. Default 2.</summary>
        public int WarmupFrames = 2;
        /// <summary>Frames above this many milliseconds count as jank. Default 8.</summary>
        public float JankMs = 8f;
        /// <summary>Frames slower than this log a warning (at most one per frame). Default 200.</summary>
        public float SlowWarnMs = 200f;

        private float _updateBudgetSeconds = 2.0f;

        private readonly float[] _perfRing = new float[PerfRingSize];
        private int _perfRingPos;
        private int _perfFrames;
        private int _perfJank;
        private int _perfAborts;
        private int _warmupLeft;
        private int _slowWarnFrame = -1;

        /// <summary>True after a successful LoadMod until UnloadMod or a failure.</summary>
        public bool HasMod { get; private set; }
        /// <summary>Host-set id used in perf/error reporting.</summary>
        public string CurrentModId = "";
        public string LastError { get; private set; } = "";
        public bool GuardsReady => _guardsReady;
        public bool HasUpdateHook => _fnUpdate != null;
        public bool IsWarmingUp => _warmupLeft > 0;
        public float UpdateBudgetSeconds => _updateBudgetSeconds;

        /// <summary>Fired after a hook failure has torn the mod down: (phase, error).</summary>
        public event Action<string, Exception> ModFailed;

        public PieModHost(JsEnv env)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));
            // Declare the delegate bridges used for mod hooks; without these,
            // Eval<Action<...>> fails with "can not find delegate bridge".
            _env.UsingAction<TApi>();
            _env.UsingAction<TApi, float>();
            _env.UsingAction<TApi, string>();
            _env.UsingAction<float>();
            _env.UsingFunc<string, string>();
            _resetJsHooks = _env.Eval<Action<float>>(@"(function () {
                globalThis.game_init = undefined;
                globalThis.game_update = undefined;
                globalThis.game_onKey = undefined;
                globalThis.game_dispose = undefined;
            })");
        }

        /// <summary>
        /// Eval the guard bundle source and cache its entry points as delegates.
        /// Returns false (and mods run WITHOUT the step guard) when the source is
        /// missing or the entry points do not bind.
        /// </summary>
        public bool InstallStepGuards(string guardsSource, string chunkName = "mod-guards.js")
        {
            _guardsReady = false;
            if (string.IsNullOrEmpty(guardsSource))
            {
                Debug.LogWarning("[Pie] mod step guards: empty source; mods run WITHOUT the step guard.");
                return false;
            }
            try
            {
                _env.Eval(guardsSource, chunkName);
                _fnInjectGuards = _env.Eval<Func<string, string>>(
                    "(typeof __pieInjectStepGuards==='function')?__pieInjectStepGuards:null");
                _fnArmGuard = _env.Eval<Action<float>>(
                    "(typeof __pieArmGuard==='function')?__pieArmGuard:null");
                _fnDisarmGuard = _env.Eval<Action<float>>(
                    "(typeof __pieDisarmGuard==='function')?__pieDisarmGuard:null");
                _guardsReady = _fnInjectGuards != null && _fnArmGuard != null && _fnDisarmGuard != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Pie] mod step guards install failed: " + e.Message);
            }
            if (!_guardsReady)
                Debug.LogWarning("[Pie] step-guard entry points missing; mods run WITHOUT the step guard.");
            return _guardsReady;
        }

        /// <summary>Load the guard bundle from a Resources TextAsset (default pie/mod-guards).</summary>
        public bool InstallStepGuardsFromResources(string resourcePath = DefaultGuardsResourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null || string.IsNullOrEmpty(asset.text))
            {
                Debug.LogWarning("[Pie] " + resourcePath + ".bytes not found; mods run WITHOUT the step guard.");
                return false;
            }
            return InstallStepGuards(asset.text);
        }

        /// <summary>
        /// Eval a mod, bind its hooks and call game_init under the init budget.
        /// Unloads any previous mod first. CurrentModId is supplied by the host.
        /// </summary>
        public bool LoadMod(string code, string chunkName, TApi api)
        {
            var requestedId = CurrentModId;
            UnloadMod();
            CurrentModId = requestedId;
            LastError = "";
            _api = api;
            ResetPerf();
            try
            {
                if (_guardsReady && _fnInjectGuards != null)
                {
                    code = _fnInjectGuards(code);
                }
                ArmGuard(InitBudgetSeconds);
                try
                {
                    _env.Eval(code, chunkName);
                    _fnDispose = EvalHook<Action>("game_dispose");
                    _fnInit = EvalHook<Action<TApi>>("game_init");
                    _fnUpdate = EvalHook<Action<TApi, float>>("game_update");
                    _fnOnKey = EvalHook<Action<TApi, string>>("game_onKey");
                    if (_fnInit == null) throw new Exception("game_init(api) is not defined");
                    _fnInit(api);
                }
                finally { DisarmGuard(); }
                HasMod = true;
                return true;
            }
            catch (Exception e)
            {
                // A top-level failure may still have declared a cleanup hook.
                if (_fnDispose == null) _fnDispose = EvalHook<Action>("game_dispose");
                Fail("init", e);
                return false;
            }
        }

        /// <summary>Best-effort game_dispose, then clear hooks. Idempotent.</summary>
        public void UnloadMod()
        {
            var dispose = _fnDispose;
            // Clear eligibility before calling user cleanup (also covers failed init).
            ClearHooks();
            HasMod = false;
            _api = null;
            CurrentModId = "";
            try
            {
                if (dispose != null)
                {
                    ArmGuard(DisposeBudgetSeconds);
                    dispose();
                }
            }
            catch (Exception e) { Debug.LogWarning("[Pie] mod dispose error: " + e.Message); }
            finally
            {
                DisarmGuard();
                _resetJsHooks(0f);
            }
        }

        /// <summary>
        /// Run one game_update tick under the update budget and record telemetry.
        /// Returns false when the mod failed (already torn down; see ModFailed).
        /// </summary>
        public bool Tick(float dt)
        {
            if (_fnUpdate == null || !HasMod) return true;
            var start = Time.realtimeSinceStartupAsDouble;
            Exception failure = null;
            ArmGuard(_updateBudgetSeconds);
            try { _fnUpdate(_api, dt); }
            catch (Exception e) { failure = e; }
            finally { DisarmGuard(); }
            if (failure != null) { Fail("update", failure); return false; }
            var ms = (float)((Time.realtimeSinceStartupAsDouble - start) * 1000.0);
            if (_warmupLeft > 0) _warmupLeft--;
            else RecordFrameMs(ms);
            if (ms > SlowWarnMs && Time.frameCount != _slowWarnFrame)
            {
                _slowWarnFrame = Time.frameCount;
                Debug.LogWarning($"[Pie] mod update took {ms:F0}ms");
            }
            return true;
        }

        /// <summary>Dispatch a budgeted key hook; failures tear the mod down.</summary>
        public void DispatchKey(string key)
        {
            if (!HasMod || _fnOnKey == null) return;
            Exception failure = null;
            ArmGuard(KeyBudgetSeconds);
            try { _fnOnKey(_api, key); }
            catch (Exception e) { failure = e; }
            finally { DisarmGuard(); }
            if (failure != null) Fail("onKey", failure);
        }

        private void Fail(string phase, Exception e)
        {
            if (e.Message != null && e.Message.Contains(StepBudgetErrorMarker))
            {
                _perfAborts++;
                phase = phase + "-budget";
            }
            LastError = $"mod '{(string.IsNullOrEmpty(CurrentModId) ? "?" : CurrentModId)}' {phase} failed: {e.Message}";
            Debug.LogError("[Pie] " + LastError);
            UnloadMod();
            ModFailed?.Invoke(phase, e);
        }

        private void ClearHooks()
        {
            _fnInit = null;
            _fnUpdate = null;
            _fnOnKey = null;
            _fnDispose = null;
        }

        private void ArmGuard(float seconds)
        {
            if (!_guardsReady) return;
            try { _fnArmGuard(seconds); } catch { /* guard must never break the frame */ }
        }

        private void DisarmGuard()
        {
            if (!_guardsReady) return;
            try { _fnDisarmGuard(0f); } catch { }
        }

        private void RecordFrameMs(float ms)
        {
            _perfRing[_perfRingPos] = ms;
            _perfRingPos = (_perfRingPos + 1) % PerfRingSize;
            _perfFrames++;
            if (ms > JankMs) _perfJank++;
        }

        private void ResetPerf()
        {
            _perfRingPos = 0;
            _perfFrames = 0;
            _perfJank = 0;
            _perfAborts = 0;
            _warmupLeft = Math.Max(0, WarmupFrames);
        }

        private static float Percentile(float[] sorted, int count, float p)
        {
            if (count <= 0) return 0f;
            var index = (int)Mathf.Ceil(p * count) - 1;
            if (index < 0) index = 0;
            if (index >= count) index = count - 1;
            return sorted[index];
        }

        [Serializable]
        private class PerfSnapshot
        {
            public string modId;
            public bool guardsReady;
            public float budgetMs;
            public int frames;
            public int jankFrames;
            public int budgetAborts;
            public float p50;
            public float p95;
            public float p99;
            public float max;
        }

        [Serializable]
        private class PerfToolPayload
        {
            public bool reset;
            public float budgetMs;
        }

        /// <summary>
        /// Perf tool handler. Report-first semantics: the snapshot is built
        /// BEFORE reset/budget are applied, so {"reset":true} returns the window
        /// that was just measured and then clears it (profiler snapshot-and-clear).
        /// </summary>
        public string PerfTool(string argsJson)
        {
            var payload = string.IsNullOrEmpty(argsJson)
                ? new PerfToolPayload()
                : JsonUtility.FromJson<PerfToolPayload>(argsJson) ?? new PerfToolPayload();
            var report = GetPerfJson();
            if (payload.budgetMs > 0f)
                _updateBudgetSeconds = payload.budgetMs / 1000f;
            if (payload.reset)
                ResetPerf();
            return report;
        }

        public string GetPerfJson()
        {
            var count = Mathf.Min(_perfFrames, PerfRingSize);
            var samples = new float[count];
            for (var i = 0; i < count; i++) samples[i] = _perfRing[i];
            Array.Sort(samples);
            var snap = new PerfSnapshot
            {
                modId = CurrentModId ?? "",
                guardsReady = _guardsReady,
                budgetMs = _updateBudgetSeconds * 1000f,
                frames = _perfFrames,
                jankFrames = _perfJank,
                budgetAborts = _perfAborts,
                p50 = Percentile(samples, count, 0.50f),
                p95 = Percentile(samples, count, 0.95f),
                p99 = Percentile(samples, count, 0.99f),
                max = count > 0 ? samples[count - 1] : 0f,
            };
            return JsonUtility.ToJson(snap, true);
        }

        private T EvalHook<T>(string name) where T : class
        {
            try { return _env.Eval<T>("(typeof " + name + "==='function')?" + name + ":null"); }
            catch (Exception e) { Debug.LogWarning("[Pie] mod hook " + name + " bind failed: " + e.Message); return null; }
        }
    }
}
