#if PIE_UNITY_SPLIT_SOURCES
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pie
{
    public static class PieUnityHostRuntime
    {
        public static string QueryJson(string argsJson)
        {
            var payload = JsonUtility.FromJson<PieUnityQueryPayload>(argsJson ?? "{}") ?? new PieUnityQueryPayload();
            var refs = QuerySceneObjects(payload);

            var result = new PieUnityQueryResult
            {
                scope = string.IsNullOrWhiteSpace(payload.scope) ? "scene_object" : payload.scope,
                summary = $"Found {refs.Length} scene object(s).",
                results = refs,
            };
            return JsonUtility.ToJson(result);
        }

        public static string InspectJson(string argsJson)
        {
            var payload = JsonUtility.FromJson<PieUnityInspectPayload>(argsJson ?? "{}") ?? new PieUnityInspectPayload();
            var target = ResolveTarget(payload.target);
            if (target == null)
                throw new InvalidOperationException("target is required.");

            var gameObject = ResolveSceneObject(target);
            if (gameObject == null)
                throw new InvalidOperationException("Target scene object not found.");

            var componentTypes = GetComponentTypes(gameObject);
            var inspect = new PieUnityInspectResult
            {
                summary = $"Inspected {gameObject.name}.",
                target = ToRef(gameObject),
                found = true,
                activeSelf = gameObject.activeSelf,
                activeInHierarchy = gameObject.activeInHierarchy,
                childCount = gameObject.transform.childCount,
                componentTypes = componentTypes,
                position = gameObject.transform.position,
                rotation = gameObject.transform.eulerAngles,
                scale = gameObject.transform.localScale,
            };
            return JsonUtility.ToJson(inspect);
        }

        public static string ApplyJson(string argsJson)
        {
            var payload = JsonUtility.FromJson<PieUnityApplyPayload>(argsJson ?? "{}") ?? new PieUnityApplyPayload();
            var action = (payload.action ?? "").Trim();
            if (string.IsNullOrWhiteSpace(action))
                throw new InvalidOperationException("action is required.");

            if (string.Equals(action, "create_scene_object", StringComparison.OrdinalIgnoreCase))
                return CreateSceneObjectJson(payload);

            if (string.Equals(action, "set_transform", StringComparison.OrdinalIgnoreCase))
                return SetTransformJson(payload);
            if (string.Equals(action, "destroy_scene_object", StringComparison.OrdinalIgnoreCase))
                return DestroySceneObjectJson(payload);
            if (string.Equals(action, "set_active", StringComparison.OrdinalIgnoreCase))
                return SetActiveJson(payload);
            if (string.Equals(action, "set_parent", StringComparison.OrdinalIgnoreCase))
                return SetParentJson(payload);
            if (string.Equals(action, "set_name", StringComparison.OrdinalIgnoreCase))
                return SetNameJson(payload);
            if (string.Equals(action, "set_tag_layer", StringComparison.OrdinalIgnoreCase))
                return SetTagLayerJson(payload);
            if (string.Equals(action, "add_component", StringComparison.OrdinalIgnoreCase))
                return AddComponentJson(payload);
            if (string.Equals(action, "remove_component", StringComparison.OrdinalIgnoreCase))
                return RemoveComponentJson(payload);
            if (string.Equals(action, "set_component_enabled", StringComparison.OrdinalIgnoreCase))
                return SetComponentEnabledJson(payload);
            if (string.Equals(action, "set_component_property", StringComparison.OrdinalIgnoreCase))
                return SetComponentPropertyJson(payload, argsJson ?? "{}");

            throw new InvalidOperationException("Unsupported action: " + action);
        }

        public static string RefreshJson()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
            return "{\"refreshed\":true,\"summary\":\"Editor assets refreshed.\"}";
#else
            return "{\"refreshed\":false,\"summary\":\"Refresh is a no-op in runtime mode.\"}";
#endif
        }

        private static string CreateSceneObjectJson(PieUnityApplyPayload payload)
        {
            var requestedName = (payload.name ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(requestedName))
            {
                var existingRefs = QuerySceneObjects(new PieUnityQueryPayload
                {
                    scope = "scene_object",
                    name = requestedName,
                    limit = 3,
                });

                if (existingRefs.Length > 1)
                {
                    throw new InvalidOperationException(
                        "Multiple scene objects already exist with name " + requestedName + ". Reuse or select one existing object instead of creating another duplicate.");
                }

                if (existingRefs.Length == 1)
                {
                    var existingObject = ResolveSceneObject(existingRefs[0]);
                    if (existingObject == null)
                        throw new InvalidOperationException("Existing scene object could not be resolved: " + requestedName);

                    ApplySceneObjectPlacement(existingObject, payload, createdNew: false);
                    var existingRef = ToRef(existingObject);
                    var reusedResult = new PieUnityApplyResult
                    {
                        action = "create_scene_object",
                        summary = "Reused existing scene object " + existingObject.name + ".",
                        target = existingRef,
                        createdRefs = new PieUnityRef[0],
                    };
                    return JsonUtility.ToJson(reusedResult);
                }
            }

            var gameObject = CreateSceneObject(payload);
            if (gameObject == null)
                throw new InvalidOperationException("Failed to create scene object.");

            ApplySceneObjectPlacement(gameObject, payload, createdNew: true);

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
                UnityEditor.Selection.activeGameObject = gameObject;
                UnityEditor.EditorGUIUtility.PingObject(gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

            var targetRef = ToRef(gameObject);
            var result = new PieUnityApplyResult
            {
                action = "create_scene_object",
                summary = "Created scene object " + gameObject.name + ".",
                target = targetRef,
                createdRefs = new[] { targetRef },
            };
            return JsonUtility.ToJson(result);
        }

        private static void ApplySceneObjectPlacement(GameObject gameObject, PieUnityApplyPayload payload, bool createdNew)
        {
            if (gameObject == null)
                throw new InvalidOperationException("Scene object is required.");

            if (HasRef(payload.parentRef) || !string.IsNullOrWhiteSpace(payload.parentName))
            {
                var parentRef = ResolveTarget(payload.parentRef);
                GameObject parent = parentRef != null ? ResolveSceneObject(parentRef) : null;
                if (parent == null && !string.IsNullOrWhiteSpace(payload.parentName))
                    parent = ResolveSceneObjectByQuery(payload.parentName);
                if (parent == null)
                {
                    if (createdNew)
                    {
                        if (Application.isPlaying) UnityEngine.Object.Destroy(gameObject);
                        else UnityEngine.Object.DestroyImmediate(gameObject);
                    }
                    throw new InvalidOperationException("Parent object not found.");
                }

                gameObject.transform.SetParent(parent.transform, false);
            }

            if (!string.IsNullOrWhiteSpace(payload.name))
                gameObject.name = payload.name.Trim();
            gameObject.transform.position = new Vector3(payload.x, payload.y, payload.z);

#if UNITY_EDITOR
            if (!createdNew && !UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.Selection.activeGameObject = gameObject;
                UnityEditor.EditorGUIUtility.PingObject(gameObject);
                UnityEditor.EditorUtility.SetDirty(gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        private static string SetTransformJson(PieUnityApplyPayload payload)
        {
            var gameObject = ResolveApplyTarget(payload);

            if (payload.applyPosition)
                gameObject.transform.position = new Vector3(payload.x, payload.y, payload.z);
            if (payload.applyRotation)
                gameObject.transform.eulerAngles = new Vector3(payload.rotX, payload.rotY, payload.rotZ);
            if (payload.applyScale)
                gameObject.transform.localScale = new Vector3(payload.scaleX, payload.scaleY, payload.scaleZ);

#if UNITY_EDITOR
            MarkSceneObjectChanged(gameObject);
#endif

            var result = new PieUnityApplyResult
            {
                action = "set_transform",
                summary = "Updated transform for " + gameObject.name + ".",
                target = ToRef(gameObject),
                createdRefs = new PieUnityRef[0],
            };
            return JsonUtility.ToJson(result);
        }

        private static string DestroySceneObjectJson(PieUnityApplyPayload payload)
        {
            var gameObject = ResolveApplyTarget(payload, requireExplicitTarget: true);
            var targetRef = ToRef(gameObject);
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying) UnityEngine.Object.Destroy(gameObject);
            else UnityEditor.Undo.DestroyObjectImmediate(gameObject);
#else
            UnityEngine.Object.Destroy(gameObject);
#endif
            var result = new PieUnityApplyResult
            {
                action = "destroy_scene_object",
                summary = "Destroyed scene object " + targetRef.name + ".",
                target = targetRef,
                createdRefs = new PieUnityRef[0],
            };
            return JsonUtility.ToJson(result);
        }

        private static string SetActiveJson(PieUnityApplyPayload payload)
        {
            var gameObject = ResolveApplyTarget(payload);
            gameObject.SetActive(payload.active);
#if UNITY_EDITOR
            MarkSceneObjectChanged(gameObject);
#endif
            return JsonUtility.ToJson(new PieUnityApplyResult
            {
                action = "set_active",
                summary = "Set active for " + gameObject.name + " to " + payload.active + ".",
                target = ToRef(gameObject),
                createdRefs = new PieUnityRef[0],
            });
        }

        private static string SetParentJson(PieUnityApplyPayload payload)
        {
            var gameObject = ResolveApplyTarget(payload);
            if (!HasRef(payload.parentRef) && string.IsNullOrWhiteSpace(payload.parentName))
                throw new InvalidOperationException("parentRef or parentName is required.");

            var parentRef = ResolveTarget(payload.parentRef);
            var parent = parentRef != null ? ResolveSceneObject(parentRef) : null;
            if (parent == null && !string.IsNullOrWhiteSpace(payload.parentName))
                parent = ResolveSceneObjectByQuery(payload.parentName);
            if (parent == null)
                throw new InvalidOperationException("Parent object not found.");

            gameObject.transform.SetParent(parent.transform, false);
#if UNITY_EDITOR
            MarkSceneObjectChanged(gameObject);
#endif
            return JsonUtility.ToJson(new PieUnityApplyResult
            {
                action = "set_parent",
                summary = "Set parent for " + gameObject.name + " to " + parent.name + ".",
                target = ToRef(gameObject),
                createdRefs = new PieUnityRef[0],
            });
        }

        private static string SetNameJson(PieUnityApplyPayload payload)
        {
            var gameObject = ResolveApplyTarget(payload);
            var nextName = (payload.name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nextName))
                throw new InvalidOperationException("name is required.");
            gameObject.name = nextName;
#if UNITY_EDITOR
            MarkSceneObjectChanged(gameObject);
#endif
            return JsonUtility.ToJson(new PieUnityApplyResult
            {
                action = "set_name",
                summary = "Renamed scene object to " + gameObject.name + ".",
                target = ToRef(gameObject),
                createdRefs = new PieUnityRef[0],
            });
        }

        private static string SetTagLayerJson(PieUnityApplyPayload payload)
        {
            var gameObject = ResolveApplyTarget(payload);
            var changed = false;
            if (!string.IsNullOrWhiteSpace(payload.tag))
            {
                gameObject.tag = payload.tag.Trim();
                changed = true;
            }
            if (payload.layer >= 0)
            {
                gameObject.layer = payload.layer;
                changed = true;
            }
            if (!changed)
                throw new InvalidOperationException("tag or layer is required.");
#if UNITY_EDITOR
            MarkSceneObjectChanged(gameObject);
#endif
            return JsonUtility.ToJson(new PieUnityApplyResult
            {
                action = "set_tag_layer",
                summary = "Updated tag/layer for " + gameObject.name + ".",
                target = ToRef(gameObject),
                createdRefs = new PieUnityRef[0],
            });
        }

        private static string AddComponentJson(PieUnityApplyPayload payload)
        {
            var gameObject = ResolveApplyTarget(payload);
            var componentType = ResolveComponentType(payload.componentType);
            var component = gameObject.AddComponent(componentType);
#if UNITY_EDITOR
            MarkSceneObjectChanged(gameObject);
#endif
            return JsonUtility.ToJson(new PieUnityApplyResult
            {
                action = "add_component",
                summary = "Added component " + component.GetType().FullName + " to " + gameObject.name + ".",
                target = ToRef(gameObject),
                createdRefs = new PieUnityRef[0],
            });
        }

        private static string RemoveComponentJson(PieUnityApplyPayload payload)
        {
            var gameObject = ResolveApplyTarget(payload, requireExplicitTarget: true);
            var componentType = ResolveComponentType(payload.componentType);
            if (componentType == typeof(Transform))
                throw new InvalidOperationException("Transform cannot be removed.");
            var component = gameObject.GetComponent(componentType);
            if (component == null)
                throw new InvalidOperationException("Component not found on " + gameObject.name + ": " + componentType.FullName);
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying) UnityEngine.Object.Destroy(component);
            else
            {
                UnityEngine.Object.DestroyImmediate(component);
                MarkSceneObjectChanged(gameObject);
            }
#else
            UnityEngine.Object.Destroy(component);
#endif
            return JsonUtility.ToJson(new PieUnityApplyResult
            {
                action = "remove_component",
                summary = "Removed component " + componentType.FullName + " from " + gameObject.name + ".",
                target = ToRef(gameObject),
                createdRefs = new PieUnityRef[0],
            });
        }

        private static string SetComponentEnabledJson(PieUnityApplyPayload payload)
        {
            var gameObject = ResolveApplyTarget(payload);
            var componentType = ResolveComponentType(payload.componentType);
            var component = ResolveSingleComponent(gameObject, componentType);
            var enabledProperty = component.GetType().GetProperty(
                "enabled",
                BindingFlags.Instance | BindingFlags.Public);
            if (enabledProperty == null || !enabledProperty.CanWrite || enabledProperty.PropertyType != typeof(bool))
                throw new InvalidOperationException("Component does not expose a writable bool enabled property: " + componentType.FullName);

            enabledProperty.SetValue(component, payload.enabled, null);
#if UNITY_EDITOR
            MarkSceneObjectChanged(gameObject);
#endif
            return JsonUtility.ToJson(new PieUnityApplyResult
            {
                action = "set_component_enabled",
                summary = "Set enabled for " + componentType.FullName + " on " + gameObject.name + " to " + payload.enabled + ".",
                target = ToRef(gameObject),
                createdRefs = new PieUnityRef[0],
            });
        }

        private static string SetComponentPropertyJson(PieUnityApplyPayload payload, string argsJson)
        {
            var gameObject = ResolveApplyTarget(payload);
            var componentType = ResolveComponentType(payload.componentType);
            var component = ResolveSingleComponent(gameObject, componentType);
            var propertyName = (payload.propertyName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new InvalidOperationException("propertyName is required.");

            if (!TryExtractJsonProperty(argsJson, "value", out var rawValue))
                throw new InvalidOperationException("value is required.");

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            var field = component.GetType().GetField(propertyName, bindingFlags);
            if (field != null)
            {
                if (field.IsInitOnly || field.IsLiteral)
                    throw new InvalidOperationException("Component field is not writable: " + propertyName);
                var converted = ConvertJsonValue(rawValue, field.FieldType);
                field.SetValue(component, converted);
#if UNITY_EDITOR
                MarkComponentChanged(component);
#endif
                return JsonUtility.ToJson(new PieUnityApplyResult
                {
                    action = "set_component_property",
                    summary = "Set field " + propertyName + " on " + componentType.FullName + ".",
                    target = ToRef(gameObject),
                    createdRefs = new PieUnityRef[0],
                });
            }

            var property = component.GetType().GetProperty(propertyName, bindingFlags);
            if (property == null)
                throw new InvalidOperationException("Component property or field not found: " + propertyName);
            if (!property.CanWrite)
                throw new InvalidOperationException("Component property is not writable: " + propertyName);
            if (property.GetIndexParameters().Length > 0)
                throw new InvalidOperationException("Indexed component properties are not supported: " + propertyName);

            var propertyValue = ConvertJsonValue(rawValue, property.PropertyType);
            property.SetValue(component, propertyValue, null);
#if UNITY_EDITOR
            MarkComponentChanged(component);
#endif
            return JsonUtility.ToJson(new PieUnityApplyResult
            {
                action = "set_component_property",
                summary = "Set property " + propertyName + " on " + componentType.FullName + ".",
                target = ToRef(gameObject),
                createdRefs = new PieUnityRef[0],
            });
        }

        private static GameObject CreateSceneObject(PieUnityApplyPayload payload)
        {
            var primitiveType = (payload.primitiveType ?? "").Trim();
            if (string.IsNullOrWhiteSpace(primitiveType))
                return new GameObject(string.IsNullOrWhiteSpace(payload.name) ? "GameObject" : payload.name.Trim());

            switch (primitiveType.ToLowerInvariant())
            {
                case "cube": return GameObject.CreatePrimitive(PrimitiveType.Cube);
                case "sphere": return GameObject.CreatePrimitive(PrimitiveType.Sphere);
                case "capsule": return GameObject.CreatePrimitive(PrimitiveType.Capsule);
                case "cylinder": return GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                case "plane": return GameObject.CreatePrimitive(PrimitiveType.Plane);
                case "quad": return GameObject.CreatePrimitive(PrimitiveType.Quad);
                default: throw new InvalidOperationException("Unsupported primitiveType: " + primitiveType);
            }
        }

        private static GameObject ResolveApplyTarget(PieUnityApplyPayload payload, bool requireExplicitTarget = false)
        {
            var hasExplicitTarget = HasRef(payload.target);
            var target = ResolveTarget(payload.target);
            if (target == null)
            {
                if (hasExplicitTarget)
                    throw new InvalidOperationException("Target not found.");
                throw new InvalidOperationException(requireExplicitTarget
                    ? "Explicit target is required for this destructive action."
                    : "target is required.");
            }

            var gameObject = ResolveSceneObject(target);
            if (gameObject == null)
                throw new InvalidOperationException("Target scene object not found.");
            return gameObject;
        }

        private static Component ResolveSingleComponent(GameObject gameObject, Type componentType)
        {
            if (gameObject == null)
                throw new InvalidOperationException("Scene object is required.");
            if (componentType == null)
                throw new InvalidOperationException("componentType is required.");

            var components = gameObject.GetComponents(componentType);
            if (components == null || components.Length == 0)
                throw new InvalidOperationException("Component not found on " + gameObject.name + ": " + componentType.FullName);
            if (components.Length > 1)
                throw new InvalidOperationException("Multiple components of type " + componentType.FullName + " found on " + gameObject.name + ". Multiple same-type components are not supported by this action.");
            return components[0];
        }

        private static object ConvertJsonValue(string rawValue, Type targetType)
        {
            if (targetType == null)
                throw new InvalidOperationException("Target member type is unavailable.");

            var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                throw new InvalidOperationException("UnityEngine.Object reference values are not supported by set_component_property.");
            if (type.IsArray || (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type)))
                throw new InvalidOperationException("Array, list, dictionary, and nested collection values are not supported by set_component_property.");

            var raw = (rawValue ?? "").Trim();
            if (string.Equals(raw, "null", StringComparison.OrdinalIgnoreCase))
            {
                if (!type.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                    return null;
                throw new InvalidOperationException("null is not valid for " + type.FullName);
            }

            if (type == typeof(string))
                return ReadJsonStringOrRaw(raw);
            if (type == typeof(bool))
                return ParseJsonBool(raw);
            if (type.IsEnum)
                return Enum.Parse(type, ReadJsonStringOrRaw(raw), true);
            if (IsNumericType(type))
                return Convert.ChangeType(ParseJsonDouble(raw), type, CultureInfo.InvariantCulture);
            if (type == typeof(Vector2))
                return new Vector2(ReadRequiredJsonFloat(raw, "x"), ReadRequiredJsonFloat(raw, "y"));
            if (type == typeof(Vector3))
                return new Vector3(ReadRequiredJsonFloat(raw, "x"), ReadRequiredJsonFloat(raw, "y"), ReadRequiredJsonFloat(raw, "z"));
            if (type == typeof(Color))
                return new Color(
                    ReadRequiredJsonFloat(raw, "r"),
                    ReadRequiredJsonFloat(raw, "g"),
                    ReadRequiredJsonFloat(raw, "b"),
                    TryReadJsonNumberMember(raw, "a", out var a) ? a : 1f);

            throw new InvalidOperationException("Unsupported component property type: " + type.FullName);
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal);
        }

        private static bool ParseJsonBool(string raw)
        {
            if (string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
                return false;
            throw new InvalidOperationException("Expected boolean JSON value.");
        }

        private static double ParseJsonDouble(string raw)
        {
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new InvalidOperationException("Expected numeric JSON value.");
            return value;
        }

        private static float ReadRequiredJsonFloat(string raw, string memberName)
        {
            if (TryReadJsonNumberMember(raw, memberName, out var value))
                return value;
            throw new InvalidOperationException("Missing numeric JSON member: " + memberName);
        }

        private static bool TryReadJsonNumberMember(string raw, string memberName, out float value)
        {
            value = 0f;
            if (!TryExtractJsonProperty(raw, memberName, out var memberRaw))
                return false;
            if (!float.TryParse(memberRaw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw new InvalidOperationException("JSON member " + memberName + " must be numeric.");
            return true;
        }

        private static bool TryExtractJsonProperty(string json, string propertyName, out string rawValue)
        {
            rawValue = "";
            var text = json ?? "";
            var key = "\"" + propertyName + "\"";
            for (var i = 0; i < text.Length; i++)
            {
                if (!StringStartsWith(text, i, key))
                    continue;
                var cursor = i + key.Length;
                cursor = SkipWhitespace(text, cursor);
                if (cursor >= text.Length || text[cursor] != ':')
                    continue;
                cursor = SkipWhitespace(text, cursor + 1);
                var end = FindJsonValueEnd(text, cursor);
                if (end <= cursor)
                    return false;
                rawValue = text.Substring(cursor, end - cursor);
                return true;
            }
            return false;
        }

        private static int SkipWhitespace(string text, int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;
            return index;
        }

        private static bool StringStartsWith(string text, int index, string value)
        {
            if (index < 0 || index + value.Length > text.Length)
                return false;
            return string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
        }

        private static int FindJsonValueEnd(string text, int start)
        {
            if (start >= text.Length)
                return start;

            var first = text[start];
            if (first == '"')
            {
                var escaped = false;
                for (var i = start + 1; i < text.Length; i++)
                {
                    var ch = text[i];
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }
                    if (ch == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                    if (ch == '"')
                        return i + 1;
                }
                return text.Length;
            }

            if (first == '{' || first == '[')
            {
                var depth = 0;
                var inString = false;
                var escaped = false;
                for (var i = start; i < text.Length; i++)
                {
                    var ch = text[i];
                    if (inString)
                    {
                        if (escaped)
                        {
                            escaped = false;
                            continue;
                        }
                        if (ch == '\\')
                        {
                            escaped = true;
                            continue;
                        }
                        if (ch == '"')
                            inString = false;
                        continue;
                    }
                    if (ch == '"')
                    {
                        inString = true;
                        continue;
                    }
                    if (ch == '{' || ch == '[')
                        depth++;
                    if (ch == '}' || ch == ']')
                    {
                        depth--;
                        if (depth == 0)
                            return i + 1;
                    }
                }
                return text.Length;
            }

            for (var i = start; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch == ',' || ch == '}')
                    return i;
            }
            return text.Length;
        }

        private static string ReadJsonStringOrRaw(string raw)
        {
            raw = (raw ?? "").Trim();
            if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"')
                return UnescapeJsonString(raw.Substring(1, raw.Length - 2));
            return raw;
        }

        private static string UnescapeJsonString(string value)
        {
            var result = new System.Text.StringBuilder();
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch != '\\' || i + 1 >= value.Length)
                {
                    result.Append(ch);
                    continue;
                }

                var escaped = value[++i];
                switch (escaped)
                {
                    case '"': result.Append('"'); break;
                    case '\\': result.Append('\\'); break;
                    case '/': result.Append('/'); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case 'u':
                        if (i + 4 >= value.Length)
                            throw new InvalidOperationException("Invalid JSON unicode escape.");
                        var hex = value.Substring(i + 1, 4);
                        result.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default:
                        result.Append(escaped);
                        break;
                }
            }
            return result.ToString();
        }

        private static Type ResolveComponentType(string componentTypeName)
        {
            var requested = (componentTypeName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(requested))
                throw new InvalidOperationException("componentType is required.");

            var matches = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch
                {
                    continue;
                }

                for (var j = 0; j < types.Length; j++)
                {
                    var type = types[j];
                    if (type == null || !typeof(Component).IsAssignableFrom(type) || type.IsAbstract)
                        continue;
                    if (string.Equals(type.FullName, requested, StringComparison.Ordinal)
                        || string.Equals(type.Name, requested, StringComparison.Ordinal))
                    {
                        matches.Add(type);
                    }
                }
            }

            if (matches.Count == 0)
                throw new InvalidOperationException("Component type not found: " + requested);
            if (matches.Count > 1)
                throw new InvalidOperationException("Component type is ambiguous: " + requested + ". Use a full type name.");
            return matches[0];
        }

#if UNITY_EDITOR
        private static void MarkSceneObjectChanged(GameObject gameObject)
        {
            if (gameObject == null)
                return;
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        private static void MarkComponentChanged(Component component)
        {
            if (component == null || UnityEditor.EditorApplication.isPlaying)
                return;
            UnityEditor.EditorUtility.SetDirty(component);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
        }
#endif

        private static PieUnityRef[] QuerySceneObjects(PieUnityQueryPayload payload)
        {
            var limit = Math.Max(1, payload.limit <= 0 ? 20 : payload.limit);
            var results = new List<PieUnityRef>();
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return new PieUnityRef[0];

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                CollectMatches(roots[i], payload, results, limit);
                if (results.Count >= limit)
                    break;
            }

            return results.ToArray();
        }

        private static void CollectMatches(GameObject gameObject, PieUnityQueryPayload payload, List<PieUnityRef> results, int limit)
        {
            if (gameObject == null || results.Count >= limit)
                return;

            if (Matches(gameObject, payload))
                results.Add(ToRef(gameObject));

            var transform = gameObject.transform;
            for (var i = 0; i < transform.childCount && results.Count < limit; i++)
                CollectMatches(transform.GetChild(i).gameObject, payload, results, limit);
        }

        private static bool Matches(GameObject gameObject, PieUnityQueryPayload payload)
        {
            if (gameObject == null)
                return false;

            if (!string.IsNullOrWhiteSpace(payload.scope)
                && !string.Equals(payload.scope, "scene_object", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrWhiteSpace(payload.name)
                && !string.Equals(gameObject.name, payload.name, StringComparison.Ordinal))
                return false;

            if (!string.IsNullOrWhiteSpace(payload.path)
                && !string.Equals(BuildHierarchyPath(gameObject.transform), payload.path, StringComparison.Ordinal))
                return false;

            if (!string.IsNullOrWhiteSpace(payload.type))
            {
                var components = gameObject.GetComponents<Component>();
                var matched = false;
                for (var i = 0; i < components.Length; i++)
                {
                    var typeName = components[i] != null ? components[i].GetType().Name : "";
                    var fullName = components[i] != null ? components[i].GetType().FullName : "";
                    if (string.Equals(typeName, payload.type, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(fullName, payload.type, StringComparison.OrdinalIgnoreCase))
                    {
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                    return false;
            }

            return true;
        }

        private static PieUnityRef ResolveTarget(PieUnityRef target)
        {
            if (HasRef(target))
                return target;

            return null;
        }

        private static bool HasRef(PieUnityRef target)
        {
            if (target == null)
                return false;

            return !string.IsNullOrWhiteSpace(target.id)
                || !string.IsNullOrWhiteSpace(target.path)
                || !string.IsNullOrWhiteSpace(target.name);
        }

        private static GameObject ResolveSceneObject(PieUnityRef target)
        {
            if (target == null)
                return null;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return null;

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var resolved = ResolveSceneObjectRecursive(roots[i], target);
                if (resolved != null)
                    return resolved;
            }
            return null;
        }

        private static GameObject ResolveSceneObjectRecursive(GameObject gameObject, PieUnityRef target)
        {
            if (gameObject == null)
                return null;

            if (string.Equals(target.id, gameObject.GetInstanceID().ToString(), StringComparison.Ordinal)
                || (!string.IsNullOrWhiteSpace(target.path) && string.Equals(target.path, BuildHierarchyPath(gameObject.transform), StringComparison.Ordinal)))
                return gameObject;

            var transform = gameObject.transform;
            for (var i = 0; i < transform.childCount; i++)
            {
                var resolved = ResolveSceneObjectRecursive(transform.GetChild(i).gameObject, target);
                if (resolved != null)
                    return resolved;
            }
            return null;
        }

        private static GameObject ResolveSceneObjectByQuery(string nameOrPath)
        {
            var payload = new PieUnityQueryPayload
            {
                scope = "scene_object",
                name = nameOrPath.IndexOf('/') >= 0 ? "" : nameOrPath,
                path = nameOrPath.IndexOf('/') >= 0 ? nameOrPath : "",
                limit = 2,
            };
            var refs = QuerySceneObjects(payload);
            if (refs.Length != 1)
                return null;
            return ResolveSceneObject(refs[0]);
        }

        private static PieUnityRef ToRef(GameObject gameObject)
        {
            return new PieUnityRef
            {
                scope = "scene_object",
                mode = PieUnityCapabilityRegistry.Mode,
                id = gameObject.GetInstanceID().ToString(),
                path = BuildHierarchyPath(gameObject.transform),
                name = gameObject.name ?? "",
                type = gameObject.GetType().FullName ?? "UnityEngine.GameObject",
                scene = gameObject.scene.path ?? gameObject.scene.name ?? "",
                parentPath = gameObject.transform.parent != null ? BuildHierarchyPath(gameObject.transform.parent) : "",
            };
        }

        private static string[] GetComponentTypes(GameObject gameObject)
        {
            var components = gameObject.GetComponents<Component>();
            var items = new List<string>();
            for (var i = 0; i < components.Length; i++)
            {
                var type = components[i] != null ? components[i].GetType().FullName : "";
                if (!string.IsNullOrWhiteSpace(type))
                    items.Add(type);
            }
            return items.ToArray();
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            if (transform == null)
                return "";

            var names = new List<string>();
            var current = transform;
            while (current != null)
            {
                names.Add(current.name ?? "");
                current = current.parent;
            }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

    }
}

#endif
