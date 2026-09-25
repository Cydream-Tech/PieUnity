using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Pie
{
    public static class PieUnityEditorAuthoring
    {
        [Serializable]
        private sealed class AssetFindPayload
        {
            public string filter = "";
            public string[] folders = new string[0];
            public int limit = 50;
        }

        [Serializable]
        private sealed class AssetPathPayload
        {
            public string path = "";
        }

        [Serializable]
        private sealed class AssetImportPayload
        {
            public string sourcePath = "";
            public string destinationPath = "";
        }

        [Serializable]
        private sealed class AssetFolderPayload
        {
            public string path = "";
        }

        [Serializable]
        private sealed class PrefabPayload
        {
            public string prefabPath = "";
            public string action = "";
            public string componentType = "";
            public string propertyPath = "";
            public string value = "";
            public string objectPath = "";
            public string objectComponentType = "";
            public int listIndex = -1;
            public string listAction = "";
        }

        [Serializable]
        private sealed class ScriptablePayload
        {
            public string path = "";
            public string typeName = "";
            public string action = "";
            public string propertyPath = "";
            public string value = "";
            public string objectPath = "";
            public string objectComponentType = "";
            public int listIndex = -1;
            public string listAction = "";
        }

        [Serializable]
        private sealed class MenuPayload
        {
            public string menuPath = "";
        }

        [Serializable]
        private sealed class AssetRef
        {
            public string path = "";
            public string guid = "";
            public string type = "";
            public string name = "";
        }

        [Serializable]
        private sealed class AssetListResult
        {
            public string summary = "";
            public AssetRef[] assets = new AssetRef[0];
        }

        [Serializable]
        private sealed class AuthoringResult
        {
            public bool ok = true;
            public string summary = "";
            public string path = "";
            public string type = "";
            public string phase = "";
            public string[] warnings = new string[0];
        }

        [Serializable]
        private sealed class CompileStatusResult
        {
            public bool isCompiling;
            public bool isUpdating;
            public bool isPlaying;
            public bool isPaused;
            public string summary = "";
        }

        public static void RegisterEditorTools()
        {
#if UNITY_EDITOR
            PieUnityCapabilityRegistry.RegisterTool(
                "unity_asset_find",
                "unity.asset",
                "Find Unity assets by AssetDatabase filter and return asset paths, GUIDs, and types.",
                "editor",
                true,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "filter", type = "string", required = true },
                    new PieUnityParameterDescriptor { name = "folders", type = "string[]", required = false },
                    new PieUnityParameterDescriptor { name = "limit", type = "integer", required = false },
                },
                AssetFindJson,
                capabilityKind: "query",
                writeScope: "none",
                returns: "assets[]",
                recommendedWorkflow: "Find assets before editing prefabs or ScriptableObjects.",
                examples: new[] { "{\"filter\":\"t:Prefab Asteroid\",\"folders\":[\"Assets/Samples\"],\"limit\":10}" });

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_asset_inspect",
                "unity.asset",
                "Inspect one Unity asset path.",
                "editor",
                true,
                false,
                null,
                new[] { new PieUnityParameterDescriptor { name = "path", type = "string", required = true } },
                AssetInspectJson,
                capabilityKind: "inspect",
                writeScope: "none",
                returns: "asset metadata");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_asset_create_folder",
                "unity.asset",
                "Create an AssetDatabase folder path if it does not already exist.",
                "editor",
                false,
                false,
                null,
                new[] { new PieUnityParameterDescriptor { name = "path", type = "string", required = true } },
                AssetCreateFolderJson,
                capabilityKind: "mutate",
                writeScope: "Assets",
                returns: "created folder path");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_asset_import",
                "unity.asset",
                "Copy a file into the Unity Assets tree and import it.",
                "editor",
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "sourcePath", type = "string", required = true },
                    new PieUnityParameterDescriptor { name = "destinationPath", type = "string", required = true },
                },
                AssetImportJson,
                capabilityKind: "mutate",
                writeScope: "Assets",
                returns: "imported asset path");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_asset_save",
                "unity.asset",
                "Save dirty Unity assets.",
                "editor",
                false,
                false,
                null,
                new PieUnityParameterDescriptor[0],
                _ => SaveAssetsJson(),
                capabilityKind: "host",
                writeScope: "Assets");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_prefab_inspect",
                "unity.prefab",
                "Inspect a prefab asset and its root components.",
                "editor",
                true,
                false,
                null,
                new[] { new PieUnityParameterDescriptor { name = "prefabPath", type = "string", required = true } },
                PrefabInspectJson,
                capabilityKind: "inspect",
                writeScope: "none");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_prefab_edit",
                "unity.prefab",
                "Edit a prefab asset. Supported actions: add_component, remove_component, set_scalar, set_object_reference, list_patch, validate.",
                "editor",
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "prefabPath", type = "string", required = true },
                    new PieUnityParameterDescriptor { name = "action", type = "string", required = true },
                    new PieUnityParameterDescriptor { name = "componentType", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "propertyPath", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "value", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "objectPath", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "objectComponentType", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "listAction", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "listIndex", type = "integer", required = false },
                },
                PrefabEditJson,
                capabilityKind: "mutate",
                writeScope: "Assets",
                returns: "prefab edit result",
                errorCodes: new[] { "ASSET_NOT_FOUND", "TYPE_NOT_FOUND", "PROPERTY_NOT_FOUND" });

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_scriptable_object_create",
                "unity.scriptable_object",
                "Create a ScriptableObject asset by C# type name.",
                "editor",
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "path", type = "string", required = true },
                    new PieUnityParameterDescriptor { name = "typeName", type = "string", required = true },
                },
                ScriptableCreateJson,
                capabilityKind: "mutate",
                writeScope: "Assets");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_scriptable_object_inspect",
                "unity.scriptable_object",
                "Inspect a ScriptableObject asset.",
                "editor",
                true,
                false,
                null,
                new[] { new PieUnityParameterDescriptor { name = "path", type = "string", required = true } },
                ScriptableInspectJson,
                capabilityKind: "inspect",
                writeScope: "none");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_scriptable_object_edit",
                "unity.scriptable_object",
                "Edit a ScriptableObject asset. Supported actions: set_scalar, set_object_reference, list_patch.",
                "editor",
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "path", type = "string", required = true },
                    new PieUnityParameterDescriptor { name = "action", type = "string", required = true },
                    new PieUnityParameterDescriptor { name = "propertyPath", type = "string", required = true },
                    new PieUnityParameterDescriptor { name = "value", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "objectPath", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "objectComponentType", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "listAction", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "listIndex", type = "integer", required = false },
                },
                ScriptableEditJson,
                capabilityKind: "mutate",
                writeScope: "Assets",
                returns: "scriptable object edit result");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_editor_execute_menu",
                "unity.editor",
                "Execute a Unity editor menu item.",
                "editor",
                false,
                false,
                null,
                new[] { new PieUnityParameterDescriptor { name = "menuPath", type = "string", required = true } },
                ExecuteMenuJson,
                capabilityKind: "host",
                writeScope: "editor",
                canTriggerDomainReload: true);

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_compile_status",
                "unity.console",
                "Read Unity editor compile/play state.",
                "editor",
                true,
                false,
                null,
                new PieUnityParameterDescriptor[0],
                _ => CompileStatusJson(),
                capabilityKind: "inspect",
                writeScope: "none");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_console_read",
                "unity.console",
                "Read Unity editor and Pie logs for diagnostics.",
                "editor",
                true,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "source", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "tailLines", type = "integer", required = false },
                    new PieUnityParameterDescriptor { name = "contains", type = "string", required = false },
                },
                PieUnityCapabilitiesBootstrap.ReadUnityLogJson,
                capabilityKind: "inspect",
                writeScope: "none");

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_playmode_start",
                "unity.editor",
                "Enter Unity play mode.",
                "editor",
                false,
                false,
                null,
                new PieUnityParameterDescriptor[0],
                _ => SetPlayModeJson(true),
                capabilityKind: "host",
                writeScope: "editor",
                canTriggerDomainReload: true);

            PieUnityCapabilityRegistry.RegisterTool(
                "unity_playmode_stop",
                "unity.editor",
                "Exit Unity play mode.",
                "editor",
                false,
                false,
                null,
                new PieUnityParameterDescriptor[0],
                _ => SetPlayModeJson(false),
                capabilityKind: "host",
                writeScope: "editor");
#endif
        }

        public static string AssetFindJson(string argsJson)
        {
#if UNITY_EDITOR
            var payload = JsonUtility.FromJson<AssetFindPayload>(argsJson ?? "{}") ?? new AssetFindPayload();
            var filter = string.IsNullOrWhiteSpace(payload.filter) ? "t:Object" : payload.filter;
            var folders = payload.folders != null && payload.folders.Length > 0 ? payload.folders : null;
            var guids = folders == null ? AssetDatabase.FindAssets(filter) : AssetDatabase.FindAssets(filter, folders);
            var limit = payload.limit <= 0 ? 50 : Math.Min(payload.limit, 500);
            var assets = new List<AssetRef>();
            for (var i = 0; i < guids.Length && assets.Count < limit; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                assets.Add(new AssetRef
                {
                    path = path,
                    guid = guids[i],
                    type = type != null ? type.FullName : "",
                    name = Path.GetFileNameWithoutExtension(path),
                });
            }
            return JsonUtility.ToJson(new AssetListResult { summary = "Found " + assets.Count + " asset(s).", assets = assets.ToArray() });
#else
            throw new InvalidOperationException("unity_asset_find is only available in the Unity Editor.");
#endif
        }

        public static string AssetInspectJson(string argsJson)
        {
#if UNITY_EDITOR
            var payload = JsonUtility.FromJson<AssetPathPayload>(argsJson ?? "{}") ?? new AssetPathPayload();
            var path = NormalizeAssetPath(payload.path);
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (obj == null)
                throw new InvalidOperationException("ASSET_NOT_FOUND: " + path);
            return JsonUtility.ToJson(new AuthoringResult
            {
                summary = "Inspected " + path + ".",
                path = path,
                type = obj.GetType().FullName,
            });
#else
            throw new InvalidOperationException("unity_asset_inspect is only available in the Unity Editor.");
#endif
        }

        public static string AssetCreateFolderJson(string argsJson)
        {
#if UNITY_EDITOR
            var payload = JsonUtility.FromJson<AssetFolderPayload>(argsJson ?? "{}") ?? new AssetFolderPayload();
            var path = NormalizeAssetPath(payload.path);
            EnsureAssetFolder(path);
            return JsonUtility.ToJson(new AuthoringResult { summary = "Created folder " + path + ".", path = path, type = "folder" });
#else
            throw new InvalidOperationException("unity_asset_create_folder is only available in the Unity Editor.");
#endif
        }

        public static string AssetImportJson(string argsJson)
        {
#if UNITY_EDITOR
            var payload = JsonUtility.FromJson<AssetImportPayload>(argsJson ?? "{}") ?? new AssetImportPayload();
            if (string.IsNullOrWhiteSpace(payload.sourcePath) || !File.Exists(payload.sourcePath))
                throw new InvalidOperationException("Source file not found: " + payload.sourcePath);
            var destination = NormalizeAssetPath(payload.destinationPath);
            EnsureAssetFolder(Path.GetDirectoryName(destination).Replace("\\", "/"));
            File.Copy(payload.sourcePath, ToAbsoluteProjectPath(destination), true);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            return JsonUtility.ToJson(new AuthoringResult { summary = "Imported " + destination + ".", path = destination, type = "asset" });
#else
            throw new InvalidOperationException("unity_asset_import is only available in the Unity Editor.");
#endif
        }

        public static string SaveAssetsJson()
        {
#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return JsonUtility.ToJson(new AuthoringResult { summary = "Saved Unity assets.", phase = "save" });
#else
            throw new InvalidOperationException("unity_asset_save is only available in the Unity Editor.");
#endif
        }

        public static string PrefabInspectJson(string argsJson)
        {
#if UNITY_EDITOR
            var payload = JsonUtility.FromJson<PrefabPayload>(argsJson ?? "{}") ?? new PrefabPayload();
            var prefab = LoadAsset<GameObject>(payload.prefabPath, "ASSET_NOT_FOUND");
            return JsonUtility.ToJson(new AuthoringResult
            {
                summary = "Prefab " + payload.prefabPath + " has " + prefab.GetComponents<Component>().Length + " root component(s).",
                path = NormalizeAssetPath(payload.prefabPath),
                type = "prefab",
            });
#else
            throw new InvalidOperationException("unity_prefab_inspect is only available in the Unity Editor.");
#endif
        }

        public static string PrefabEditJson(string argsJson)
        {
#if UNITY_EDITOR
            var payload = JsonUtility.FromJson<PrefabPayload>(argsJson ?? "{}") ?? new PrefabPayload();
            var path = NormalizeAssetPath(payload.prefabPath);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                throw new InvalidOperationException("ASSET_NOT_FOUND: " + path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ApplyComponentAction(root, payload.action, payload.componentType, payload.propertyPath, payload.value, payload.objectPath, payload.objectComponentType, payload.listAction, payload.listIndex);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                AssetDatabase.SaveAssets();
                return JsonUtility.ToJson(new AuthoringResult { summary = "Updated prefab " + path + ".", path = path, type = "prefab", phase = payload.action });
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
#else
            throw new InvalidOperationException("unity_prefab_edit is only available in the Unity Editor.");
#endif
        }

        public static string ScriptableCreateJson(string argsJson)
        {
#if UNITY_EDITOR
            var payload = JsonUtility.FromJson<ScriptablePayload>(argsJson ?? "{}") ?? new ScriptablePayload();
            var path = NormalizeAssetPath(payload.path);
            var type = ResolveType(payload.typeName);
            if (!typeof(ScriptableObject).IsAssignableFrom(type))
                throw new InvalidOperationException("TYPE_NOT_SCRIPTABLE_OBJECT: " + payload.typeName);
            EnsureAssetFolder(Path.GetDirectoryName(path).Replace("\\", "/"));
            var asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return JsonUtility.ToJson(new AuthoringResult { summary = "Created ScriptableObject " + path + ".", path = path, type = type.FullName });
#else
            throw new InvalidOperationException("unity_scriptable_object_create is only available in the Unity Editor.");
#endif
        }

        public static string ScriptableInspectJson(string argsJson)
        {
#if UNITY_EDITOR
            var payload = JsonUtility.FromJson<ScriptablePayload>(argsJson ?? "{}") ?? new ScriptablePayload();
            var asset = LoadAsset<ScriptableObject>(payload.path, "ASSET_NOT_FOUND");
            return JsonUtility.ToJson(new AuthoringResult { summary = "Inspected ScriptableObject " + payload.path + ".", path = NormalizeAssetPath(payload.path), type = asset.GetType().FullName });
#else
            throw new InvalidOperationException("unity_scriptable_object_inspect is only available in the Unity Editor.");
#endif
        }

        public static string ScriptableEditJson(string argsJson)
        {
#if UNITY_EDITOR
            var payload = JsonUtility.FromJson<ScriptablePayload>(argsJson ?? "{}") ?? new ScriptablePayload();
            var asset = LoadAsset<ScriptableObject>(payload.path, "ASSET_NOT_FOUND");
            var serialized = new SerializedObject(asset);
            ApplySerializedAction(serialized, payload.action, payload.propertyPath, payload.value, payload.objectPath, payload.objectComponentType, payload.listAction, payload.listIndex);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return JsonUtility.ToJson(new AuthoringResult { summary = "Updated ScriptableObject " + payload.path + ".", path = NormalizeAssetPath(payload.path), type = asset.GetType().FullName, phase = payload.action });
#else
            throw new InvalidOperationException("unity_scriptable_object_edit is only available in the Unity Editor.");
#endif
        }

        public static string ExecuteMenuJson(string argsJson)
        {
#if UNITY_EDITOR
            var payload = JsonUtility.FromJson<MenuPayload>(argsJson ?? "{}") ?? new MenuPayload();
            if (string.IsNullOrWhiteSpace(payload.menuPath))
                throw new InvalidOperationException("menuPath is required.");
            var ok = EditorApplication.ExecuteMenuItem(payload.menuPath);
            if (!ok)
                throw new InvalidOperationException("MENU_NOT_FOUND: " + payload.menuPath);
            return JsonUtility.ToJson(new AuthoringResult { summary = "Executed menu " + payload.menuPath + ".", phase = "menu" });
#else
            throw new InvalidOperationException("unity_editor_execute_menu is only available in the Unity Editor.");
#endif
        }

        public static string CompileStatusJson()
        {
#if UNITY_EDITOR
            return JsonUtility.ToJson(new CompileStatusResult
            {
                isCompiling = EditorApplication.isCompiling,
                isUpdating = EditorApplication.isUpdating,
                isPlaying = EditorApplication.isPlaying,
                isPaused = EditorApplication.isPaused,
                summary = "Unity compile/play state read.",
            });
#else
            throw new InvalidOperationException("unity_compile_status is only available in the Unity Editor.");
#endif
        }

        public static string SetPlayModeJson(bool value)
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = value;
            return JsonUtility.ToJson(new AuthoringResult { summary = value ? "Entering play mode." : "Exiting play mode.", phase = value ? "play" : "stop" });
#else
            throw new InvalidOperationException("Play mode control is only available in the Unity Editor.");
#endif
        }


#if UNITY_EDITOR
        private static void ApplyComponentAction(GameObject root, string action, string componentType, string propertyPath, string value, string objectPath, string objectComponentType, string listAction, int listIndex)
        {
            if (string.Equals(action, "validate", StringComparison.OrdinalIgnoreCase))
                return;
            var type = ResolveType(componentType);
            if (!typeof(Component).IsAssignableFrom(type))
                throw new InvalidOperationException("TYPE_NOT_COMPONENT: " + componentType);
            if (string.Equals(action, "add_component", StringComparison.OrdinalIgnoreCase))
            {
                if (root.GetComponent(type) == null)
                    root.AddComponent(type);
                return;
            }
            var component = root.GetComponent(type);
            if (component == null)
                throw new InvalidOperationException("COMPONENT_NOT_FOUND: " + componentType);
            if (string.Equals(action, "remove_component", StringComparison.OrdinalIgnoreCase))
            {
                UnityEngine.Object.DestroyImmediate(component, true);
                return;
            }
            var serialized = new SerializedObject(component);
            ApplySerializedAction(serialized, action, propertyPath, value, objectPath, objectComponentType, listAction, listIndex);
            serialized.ApplyModifiedProperties();
        }

        private static void ApplySerializedAction(SerializedObject serialized, string action, string propertyPath, string value, string objectPath, string objectComponentType, string listAction, int listIndex)
        {
            var property = serialized.FindProperty(propertyPath ?? "");
            if (property == null)
                throw new InvalidOperationException("PROPERTY_NOT_FOUND: " + propertyPath);

            if (string.Equals(action, "set_scalar", StringComparison.OrdinalIgnoreCase))
            {
                SetScalarProperty(property, value);
                return;
            }
            if (string.Equals(action, "set_object_reference", StringComparison.OrdinalIgnoreCase))
            {
                property.objectReferenceValue = ResolveObjectReference(objectPath, objectComponentType);
                return;
            }
            if (string.Equals(action, "list_patch", StringComparison.OrdinalIgnoreCase))
            {
                ApplyListPatch(property, listAction, listIndex, value, objectPath, objectComponentType);
                return;
            }
            throw new InvalidOperationException("Unsupported serialized action: " + action);
        }

        private static void ApplyListPatch(SerializedProperty property, string action, int index, string value, string objectPath, string objectComponentType)
        {
            if (!property.isArray)
                throw new InvalidOperationException("PROPERTY_NOT_ARRAY: " + property.propertyPath);
            var itemIndex = index < 0 ? property.arraySize : index;
            if (string.Equals(action, "clear", StringComparison.OrdinalIgnoreCase))
            {
                property.ClearArray();
                return;
            }
            if (string.Equals(action, "remove", StringComparison.OrdinalIgnoreCase))
            {
                if (itemIndex < 0 || itemIndex >= property.arraySize)
                    throw new InvalidOperationException("LIST_INDEX_OUT_OF_RANGE: " + itemIndex);
                property.DeleteArrayElementAtIndex(itemIndex);
                return;
            }
            if (!string.Equals(action, "add", StringComparison.OrdinalIgnoreCase) && !string.Equals(action, "set", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unsupported listAction: " + action);
            if (string.Equals(action, "add", StringComparison.OrdinalIgnoreCase))
            {
                property.InsertArrayElementAtIndex(property.arraySize);
                itemIndex = property.arraySize - 1;
            }
            if (itemIndex < 0 || itemIndex >= property.arraySize)
                throw new InvalidOperationException("LIST_INDEX_OUT_OF_RANGE: " + itemIndex);
            var item = property.GetArrayElementAtIndex(itemIndex);
            if (item.propertyType == SerializedPropertyType.ObjectReference)
                item.objectReferenceValue = ResolveObjectReference(objectPath, objectComponentType);
            else
                SetScalarProperty(item, value);
        }

        private static void SetScalarProperty(SerializedProperty property, string value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    property.stringValue = value ?? "";
                    return;
                case SerializedPropertyType.Boolean:
                    property.boolValue = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    return;
                case SerializedPropertyType.Integer:
                    property.longValue = long.Parse(value ?? "0", CultureInfo.InvariantCulture);
                    return;
                case SerializedPropertyType.Float:
                    property.doubleValue = double.Parse(value ?? "0", CultureInfo.InvariantCulture);
                    return;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = ResolveEnumIndex(property, value);
                    return;
                default:
                    throw new InvalidOperationException("PROPERTY_NOT_SCALAR: " + property.propertyPath + " (" + property.propertyType + ")");
            }
        }

        private static int ResolveEnumIndex(SerializedProperty property, string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                return index;
            for (var i = 0; i < property.enumNames.Length; i++)
            {
                if (string.Equals(property.enumNames[i], value, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(property.enumDisplayNames[i], value, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            throw new InvalidOperationException("ENUM_VALUE_NOT_FOUND: " + value);
        }

        private static UnityEngine.Object ResolveObjectReference(string objectPath, string componentType)
        {
            var path = NormalizeAssetPath(objectPath);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
                throw new InvalidOperationException("OBJECT_REFERENCE_NOT_FOUND: " + objectPath);
            if (!string.IsNullOrWhiteSpace(componentType))
            {
                var go = asset as GameObject;
                if (go == null)
                    throw new InvalidOperationException("OBJECT_REFERENCE_NOT_GAMEOBJECT: " + objectPath);
                var type = ResolveType(componentType);
                var component = go.GetComponent(type);
                if (component == null)
                    throw new InvalidOperationException("OBJECT_COMPONENT_NOT_FOUND: " + componentType);
                return component;
            }
            return asset;
        }

        private static T LoadAsset<T>(string path, string code) where T : UnityEngine.Object
        {
            var normalized = NormalizeAssetPath(path);
            var asset = AssetDatabase.LoadAssetAtPath<T>(normalized);
            if (asset == null)
                throw new InvalidOperationException(code + ": " + normalized);
            return asset;
        }

        private static Type ResolveType(string typeName)
        {
            var requested = (typeName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(requested))
                throw new InvalidOperationException("typeName/componentType is required.");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch { continue; }
                for (var i = 0; i < types.Length; i++)
                {
                    var type = types[i];
                    if (string.Equals(type.FullName, requested, StringComparison.Ordinal)
                        || string.Equals(type.Name, requested, StringComparison.Ordinal))
                        return type;
                }
            }
            throw new InvalidOperationException("TYPE_NOT_FOUND: " + requested);
        }

        private static string NormalizeAssetPath(string path)
        {
            var value = (path ?? "").Replace("\\", "/").Trim();
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Asset path is required.");
            if (value.StartsWith(Application.dataPath.Replace("\\", "/"), StringComparison.Ordinal))
                value = "Assets" + value.Substring(Application.dataPath.Length);
            if (!value.StartsWith("Assets/", StringComparison.Ordinal) && value != "Assets")
                throw new InvalidOperationException("Path must be under Assets: " + path);
            return value;
        }

        private static string ToAbsoluteProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath).Replace("\\", "/");
        }

        private static void EnsureAssetFolder(string path)
        {
            path = NormalizeAssetPath(path);
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
#endif
    }
}
