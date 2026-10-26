using System;
using System.Collections.Generic;
using Puerts;

namespace Pie.Editor
{
    /// <summary>
    /// pie-unity's built-in PuerTS binding preset (xIl2cpp static bindings).
    ///
    /// Standard tier: CoreModule math/scene/resources + Physics + Audio, plus
    /// the pie-unity closed System surface (see Runtime/binding-surface.json)
    /// and the Pie bridge types agents touch through CS.*. Every type here is
    /// an API stable since Unity 2022.3 (the pie-unity minimum); anything
    /// newer must be added behind a version #if so 2022.3 hosts keep
    /// compiling.
    ///
    /// Rules that make this compose safely with host projects:
    ///  - PuerTS merges ALL [Configure] classes via AppDomain scan, so hosts
    ///    add their own types with their own [Configure] class — they must
    ///    not fork this file.
    ///  - This class deliberately does NOT declare [CodeOutputDirectory].
    ///    PuerTS resolves the output directory first-wins across configure
    ///    classes; a package-level declaration would race the host's own.
    ///    With no declaration anywhere, PuerTS defaults to Assets/Gen/.
    ///  - Reflection fallback stays alive for everything NOT listed here:
    ///    ungenerated types still work through the slow path, so being
    ///    conservative is safe. (Verified in the dogfood spike: NavMesh and
    ///    ParticleSystem kept working ungenerated in an optimized player.)
    /// </summary>
    [Configure]
    public class PiePuerTSBindings
    {
        [Binding]
        public static IEnumerable<Type> Bindings
        {
            get
            {
                // ── CoreModule: math ────────────────────────────────────
                yield return typeof(UnityEngine.Vector2);
                yield return typeof(UnityEngine.Vector3);
                yield return typeof(UnityEngine.Vector4);
                yield return typeof(UnityEngine.Quaternion);
                yield return typeof(UnityEngine.Matrix4x4);
                yield return typeof(UnityEngine.Mathf);
                yield return typeof(UnityEngine.Color);
                yield return typeof(UnityEngine.Color32);
                yield return typeof(UnityEngine.Rect);
                yield return typeof(UnityEngine.Bounds);
                yield return typeof(UnityEngine.Random);
                yield return typeof(UnityEngine.AnimationCurve);
                // ── CoreModule: scene/object model ──────────────────────
                yield return typeof(UnityEngine.Object);
                yield return typeof(UnityEngine.GameObject);
                yield return typeof(UnityEngine.Transform);
                yield return typeof(UnityEngine.Component);
                yield return typeof(UnityEngine.Behaviour);
                yield return typeof(UnityEngine.MonoBehaviour);
                yield return typeof(UnityEngine.Time);
                yield return typeof(UnityEngine.Debug);
                yield return typeof(UnityEngine.Application);
                yield return typeof(UnityEngine.SceneManagement.SceneManager);
                yield return typeof(UnityEngine.SceneManagement.Scene);
                yield return typeof(UnityEngine.Resources);
                yield return typeof(UnityEngine.JsonUtility);
                yield return typeof(UnityEngine.Input);
                // ── CoreModule: rendering basics ────────────────────────
                yield return typeof(UnityEngine.Camera);
                yield return typeof(UnityEngine.Renderer);
                yield return typeof(UnityEngine.MeshRenderer);
                yield return typeof(UnityEngine.MeshFilter);
                yield return typeof(UnityEngine.Mesh);
                yield return typeof(UnityEngine.Material);
                yield return typeof(UnityEngine.Texture2D);
                yield return typeof(UnityEngine.Light);
                // ── Physics ─────────────────────────────────────────────
                yield return typeof(UnityEngine.Physics);
                yield return typeof(UnityEngine.Rigidbody);
                yield return typeof(UnityEngine.Collider);
                yield return typeof(UnityEngine.BoxCollider);
                yield return typeof(UnityEngine.SphereCollider);
                yield return typeof(UnityEngine.CapsuleCollider);
                yield return typeof(UnityEngine.MeshCollider);
                yield return typeof(UnityEngine.RaycastHit);
                yield return typeof(UnityEngine.CharacterController);
                // ── Audio ───────────────────────────────────────────────
                yield return typeof(UnityEngine.AudioSource);
                yield return typeof(UnityEngine.AudioClip);
                yield return typeof(UnityEngine.AudioListener);
                yield return typeof(UnityEngine.AudioSettings);
                // ── pie-unity closed System surface (binding-surface.json)
                yield return typeof(System.IO.File);
                yield return typeof(System.IO.Directory);
                yield return typeof(System.IO.Path);
                yield return typeof(System.IO.FileInfo);
                yield return typeof(System.IO.DirectoryInfo);
                yield return typeof(System.IO.FileAttributes);
                yield return typeof(System.Convert);
                yield return typeof(System.Environment);
                yield return typeof(System.Environment.SpecialFolder);
                // ── Pie bridges ─────────────────────────────────────────
                yield return typeof(Pie.PieUtf8Sha256);
                yield return typeof(Pie.PieFileBridge);
                yield return typeof(Pie.PieHttpBridge);
                yield return typeof(Pie.PieDiagnostics);
                yield return typeof(Pie.PieBridge);
                yield return typeof(Pie.PieProjectPaths);
                yield return typeof(Pie.PieHostBridge);
                yield return typeof(Pie.PieRunner);
            }
        }
    }
}
