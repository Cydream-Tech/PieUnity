#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Pie.GameUse
{
    /// <summary>
    /// Game Use input hands (MVP3b): the only C# addition of the Game Use
    /// design. Synthesizes Input System device state so a game-use decider
    /// can play any game that reads Keyboard/Mouse through the Input System
    /// — no per-game adapter needed for input.
    ///
    /// The surface mirrors the field-studied Autopilot hands: press (tap),
    /// hold (leased until released), release, release_all, plus a 2D move
    /// axis for twin-stick style reads. Calls are queued and applied on the
    /// next Update so device state never changes mid-frame from a script.
    ///
    /// Reachable from scripts as CS.Pie.GameUse.PieInputHands.* and from the
    /// dev RPC host as game_use.input.*. Compiled only when the Input System
    /// package is present (ENABLE_INPUT_SYSTEM).
    /// </summary>
    public static class PieInputHands
    {
        private static readonly HashSet<Key> Held = new HashSet<Key>();
        // One entry per Update tick: a key state change, or a frame marker
        // (null) that just advances the queue by one Update.
        private static readonly Queue<(Key? key, bool down)> Pending = new Queue<(Key?, bool)>();
        private static Vector2 _axis;
        private static bool _axisQueued;
        private static Vector2 _queuedAxis;
        private static PieInputHandsDriver _driver;
        // Episode activity guard (see Update): while a game-use episode is
        // pressing keys, a human typing on the same physical keyboard can
        // interleave their own state events and erase our down-edges (the
        // game sees a 0→1→0 within one frame and wasPressedThisFrame fires
        // for neither). When active we re-assert our full keyboard state on
        // EVERY frame, so any foreign event is overwritten by the next one
        // of ours and the game sees our intended held set continuously.
        // Foreign keys the human holds are re-released the same way; during
        // an episode that is the documented contract (the loop owns input).
        private static int _activeEpisodeFrames;

        /// <summary>Declare an episode in progress for the next N frames:
        /// re-assert synthesized state every frame so interleaved human
        /// input cannot erase our down-edges. Call again to extend.</summary>
        [UnityEngine.Scripting.Preserve]
        public static string SetEpisodeActive(int frames)
        {
            EnsureDriver();
            var n = frames <= 0 ? 0 : frames > 100000 ? 100000 : frames;
            _activeEpisodeFrames = n;
            return $"{{\"ok\":true,\"episodeFrames\":{n}}}";
        }

        [UnityEngine.Scripting.Preserve]
        public static string Press(string keyName)
        {
            EnsureDriver();
            var key = ParseKey(keyName);
            if (key == null) return Json(false, $"unknown key {keyName}");
            Enqueue(key.Value, true);
            Enqueue(key.Value, false, framesDelay: 2); // down now, up two frames later
            return Json(true, null);
        }

        [UnityEngine.Scripting.Preserve]
        public static string Hold(
string keyName)
        {
            EnsureDriver();
            var key = ParseKey(keyName);
            if (key == null) return Json(false, $"unknown key {keyName}");
            Enqueue(key.Value, true);
            return Json(true, null);
        }

        [UnityEngine.Scripting.Preserve]
        public static string Release(
string keyName)
        {
            EnsureDriver();
            var key = ParseKey(keyName);
            if (key == null) return Json(false, $"unknown key {keyName}");
            Enqueue(key.Value, false);
            return Json(true, null);
        }

        [UnityEngine.Scripting.Preserve]
        public static string ReleaseAll(
)
        {
            EnsureDriver();
            foreach (var key in Held) Enqueue(key, false, bypassHeld: true);
            Held.Clear();
            return Json(true, null);
        }

        /// <summary>A 2D axis (e.g. movement): x right, y up, each -1..1.</summary>
        [UnityEngine.Scripting.Preserve]
        public static string SetAxis(float x, float y)
        {
            EnsureDriver();
            _queuedAxis = new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
            _axisQueued = true;
            return Json(true, null);
        }

        [UnityEngine.Scripting.Preserve]
        public static string HeldKeys()
        {
            EnsureDriver();
            var names = new List<string>();
            foreach (var key in Held) names.Add(key.ToString());
            return $"[{string.Join(",", names)}]";
        }

        private static void Enqueue(Key key, bool down, int framesDelay = 0, bool bypassHeld = false)
        {
            // framesDelay defers this event by that many Updates (markers
            // first, the event after) so a press spans real frames and games
            // see wasPressedThisFrame exactly once.
            for (var i = 0; i < framesDelay; i++) Pending.Enqueue((null, false));
            Pending.Enqueue((key, down));
            if (!bypassHeld)
            {
                if (down) Held.Add(key);
                else Held.Remove(key);
            }
        }

        private static Key? ParseKey(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            // Accept both Input System names (W, A, Space, UpArrow) and a few
            // friendlier aliases from the field study's vocabulary.
            switch (name.ToLowerInvariant())
            {
                case "space": return Key.Space;
                case "up": case "uparrow": return Key.UpArrow;
                case "down": case "downarrow": return Key.DownArrow;
                case "left": case "leftarrow": return Key.LeftArrow;
                case "right": case "rightarrow": return Key.RightArrow;
            }
            return Enum.TryParse<Key>(name, true, out var key) ? key : (Key?)null;
        }

        private static string Json(bool ok, string error) => $"{{\"ok\":{ok.ToString().ToLowerInvariant()}{(error == null ? "" : $",\"error\":\"{error}\"")}}}";

        /// <summary>Applies queued state on Update; created on first use.</summary>
        [DefaultExecutionOrder(-1000)]
        private sealed class PieInputHandsDriver : MonoBehaviour
        {
            private void Update()
            {
                // Drain pending changes into the held set; frame markers (null)
                // just advance the queue by one Update.
                while (Pending.Count > 0)
                {
                    var (key, down) = Pending.Dequeue();
                    if (key == null) break;
                    if (down) Held.Add(key.Value);
                    else Held.Remove(key.Value);
                }
                if (_axisQueued)
                {
                    _axis = _queuedAxis;
                    _axisQueued = false;
                }
                // One whole-keyboard state event per Update: wasPressedThisFrame
                // in games sees the delta against the previous event.
                var keyboard = Keyboard.current;
                if (keyboard == null) return;
                var idle = Held.Count == 0 && Mathf.Abs(_axis.x) < 0.01f && Mathf.Abs(_axis.y) < 0.01f;
                if (idle && _activeEpisodeFrames <= 0)
                {
                    // Nothing synthesized: stay silent so a human typing
                    // between episodes is never fought over.
                    return;
                }
                if (_activeEpisodeFrames > 0) _activeEpisodeFrames--;
                var pressed = new List<Key>(Held);
                if (_axis.y > 0.5f) pressed.Add(Key.W);
                if (_axis.y < -0.5f) pressed.Add(Key.S);
                if (_axis.x < -0.5f) pressed.Add(Key.A);
                if (_axis.x > 0.5f) pressed.Add(Key.D);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(pressed.ToArray()));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDriver()
        {
            // Unity's destroyed-object null check: a driver destroyed by a
            // script-task cleanup or a scene teardown must be rebuilt, so
            // compare against null the Unity way (a dead UnityEngine.Object
            // still compares equal to null through the overloaded ==).
            if (_driver != null) return;
            var go = new GameObject("PieInputHands");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _driver = go.AddComponent<PieInputHandsDriver>();
        }
    }
}
#endif
