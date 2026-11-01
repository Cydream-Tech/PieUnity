using UnityEngine;

namespace Pie.GameUse
{
    /// <summary>Runtime knobs the game-use driver needs from script land.
    /// Kept as explicit statics: the runtime bridge exposes calls, not
    /// property writes, and engine accessors are not callable.</summary>
    public static class PieRuntimeOptions
    {
        [UnityEngine.Scripting.Preserve]
        public static string SetRunInBackground(bool enabled)
        {
            Application.runInBackground = enabled;
            return "{\"ok\":true,\"runInBackground\":" + (Application.runInBackground ? "true" : "false") + "}";
        }
    }
}
