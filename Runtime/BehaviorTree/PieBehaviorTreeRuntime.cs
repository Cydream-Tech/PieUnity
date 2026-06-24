using System;
using System.Collections.Generic;
using UnityEngine;
using NPAction = NPBehave.Action;
using NPBlackboard = NPBehave.Blackboard;
using NPNode = NPBehave.Node;
using NPRoot = NPBehave.Root;

namespace Pie
{
    [Serializable]
    public sealed class PieBehaviorTreeBlackboardEntry
    {
        public string key = "";
        public string type = "string";
        public string stringValue = "";
        public float numberValue;
        public int intValue;
        public bool boolValue;
        public string jsonValue = "";
    }

    [Serializable]
    public sealed class PieBehaviorTreeNodeSpec
    {
        public string type = "";
        public string id = "";
        public string name = "";
        public PieBehaviorTreeNodeSpec[] children = new PieBehaviorTreeNodeSpec[0];
        public PieBehaviorTreeNodeSpec child;
        public string key = "";
        public string valueKey = "";
        public string op = "";
        public PieBehaviorTreeBlackboardEntry value;
        public string stopsOnChange = "";
        public float seconds;
        public string action = "";
        public string argsJson = "";
        public float intervalSeconds;
        public float minIntervalSeconds;
        public string successPolicy = "";
        public string failurePolicy = "";
    }

    [Serializable]
    public sealed class PieBehaviorTreeCreatePayload
    {
        public string treeId = "";
        public string id = "";
        public string name = "";
        public PieBehaviorTreeNodeSpec root;
        public PieBehaviorTreeBlackboardEntry[] blackboard = new PieBehaviorTreeBlackboardEntry[0];
        public bool start;
        public bool replaceExisting = true;
    }

    [Serializable]
    public sealed class PieBehaviorTreeIdPayload
    {
        public string treeId = "";
        public string id = "";
    }

    [Serializable]
    public sealed class PieBehaviorTreeBlackboardPayload
    {
        public string treeId = "";
        public string id = "";
        public PieBehaviorTreeBlackboardEntry[] entries = new PieBehaviorTreeBlackboardEntry[0];
    }

    [Serializable]
    public sealed class PieBehaviorTreeActionResult
    {
        public bool success = true;
        public string resultJson = "{}";
        public string error = "";
        public string eventType = "";
        public string eventJson = "";
        public PieBehaviorTreeBlackboardEntry[] blackboardUpdates = new PieBehaviorTreeBlackboardEntry[0];
    }

    public sealed class PieBehaviorTreeActionContext
    {
        public string TreeId { get; internal set; }
        public string ActionName { get; internal set; }
        public string ArgsJson { get; internal set; }
        public PieBehaviorTreeBlackboardEntry[] Blackboard { get; internal set; }
    }

    public interface IPieBehaviorTreeActionReceiver
    {
        string ActionName { get; }
        PieBehaviorTreeActionResult Execute(PieBehaviorTreeActionContext context);
    }

    [Serializable]
    public sealed class PieBehaviorTreeStatus
    {
        public string treeId = "";
        public string name = "";
        public bool found;
        public bool isActive;
        public string state = "";
        public string rootState = "";
        public int actionCount;
        public int tickCount;
        public float actionRatePerSecond;
        public string lastAction = "";
        public bool lastActionSuccess;
        public string lastActionResultJson = "";
        public long lastActionAtUnixMs;
        public long lastBlackboardWriteAtUnixMs;
        public string lastSkippedAction = "";
        public int skippedActionCount;
        public string lastEventType = "";
        public string lastEventJson = "";
        public int eventCount;
        public string lastError = "";
        public bool isExecutingAction;
        public string lastTickSource = "";
        public PieBehaviorTreeBlackboardEntry[] blackboard = new PieBehaviorTreeBlackboardEntry[0];
        public string[] logs = new string[0];
    }

    [Serializable]
    public sealed class PieBehaviorTreeStatusResult
    {
        public string service = "pie-behavior-tree";
        public PieBehaviorTreeStatus[] trees = new PieBehaviorTreeStatus[0];
    }

    public static class PieBehaviorTreeActionRegistry
    {
        private static readonly Dictionary<string, IPieBehaviorTreeActionReceiver> Receivers =
            new Dictionary<string, IPieBehaviorTreeActionReceiver>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Dictionary<string, IPieBehaviorTreeActionReceiver>> ScopedReceivers =
            new Dictionary<string, Dictionary<string, IPieBehaviorTreeActionReceiver>>(StringComparer.OrdinalIgnoreCase);

        static PieBehaviorTreeActionRegistry()
        {
            Register(new DebugRecordActionReceiver());
        }

        public static void Register(IPieBehaviorTreeActionReceiver receiver)
        {
            if (receiver == null)
                throw new ArgumentNullException(nameof(receiver));
            var name = (receiver.ActionName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Behavior tree action receiver name is required.", nameof(receiver));
            Receivers[name] = receiver;
        }

        public static void Register(string treeId, IPieBehaviorTreeActionReceiver receiver)
        {
            if (receiver == null)
                throw new ArgumentNullException(nameof(receiver));
            treeId = (treeId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(treeId))
                throw new ArgumentException("Behavior tree scoped receiver treeId is required.", nameof(treeId));
            var name = (receiver.ActionName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Behavior tree action receiver name is required.", nameof(receiver));
            if (!ScopedReceivers.TryGetValue(treeId, out var receivers))
            {
                receivers = new Dictionary<string, IPieBehaviorTreeActionReceiver>(StringComparer.OrdinalIgnoreCase);
                ScopedReceivers[treeId] = receivers;
            }
            receivers[name] = receiver;
        }

        public static bool Unregister(string actionName)
        {
            actionName = (actionName ?? "").Trim();
            return !string.IsNullOrWhiteSpace(actionName) && Receivers.Remove(actionName);
        }

        public static bool Unregister(string treeId, string actionName)
        {
            treeId = (treeId ?? "").Trim();
            actionName = (actionName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(treeId) || string.IsNullOrWhiteSpace(actionName))
                return false;
            if (!ScopedReceivers.TryGetValue(treeId, out var receivers))
                return false;
            var removed = receivers.Remove(actionName);
            if (receivers.Count == 0)
                ScopedReceivers.Remove(treeId);
            return removed;
        }

        public static bool UnregisterTree(string treeId)
        {
            treeId = (treeId ?? "").Trim();
            return !string.IsNullOrWhiteSpace(treeId) && ScopedReceivers.Remove(treeId);
        }

        public static bool TryExecute(PieBehaviorTreeActionContext context, out PieBehaviorTreeActionResult result)
        {
            result = null;
            var actionName = (context?.ActionName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(actionName))
            {
                result = Failure("Behavior tree action name is required.");
                return false;
            }

            var treeId = (context?.TreeId ?? "").Trim();
            IPieBehaviorTreeActionReceiver receiver = null;
            if (!string.IsNullOrWhiteSpace(treeId)
                && ScopedReceivers.TryGetValue(treeId, out var scoped)
                && scoped.TryGetValue(actionName, out var scopedReceiver))
                receiver = scopedReceiver;
            if (receiver == null)
                Receivers.TryGetValue(actionName, out receiver);

            if (receiver == null)
            {
                result = Failure("Behavior tree action receiver is not registered: action=" + actionName + " tree=" + treeId);
                return false;
            }

            try
            {
                result = receiver.Execute(context) ?? Failure("Behavior tree action receiver returned null: " + actionName);
            }
            catch (Exception ex)
            {
                result = Failure(ex.Message);
            }
            return result.success;
        }

        private static PieBehaviorTreeActionResult Failure(string message)
        {
            return new PieBehaviorTreeActionResult
            {
                success = false,
                resultJson = "{}",
                error = message ?? "Behavior tree action failed.",
                blackboardUpdates = new PieBehaviorTreeBlackboardEntry[0],
            };
        }

        private sealed class DebugRecordActionReceiver : IPieBehaviorTreeActionReceiver
        {
            public string ActionName => "pie.debug.record_action";

            public PieBehaviorTreeActionResult Execute(PieBehaviorTreeActionContext context)
            {
                var action = context?.ActionName ?? ActionName;
                var args = context?.ArgsJson ?? "{}";
                PieDiagnostics.Info($"[PieBehaviorTree] debug action tree={context?.TreeId ?? ""} action={action} args={args}");
                return new PieBehaviorTreeActionResult
                {
                    success = true,
                    resultJson = "{\"recorded\":true}",
                    error = "",
                    blackboardUpdates = new[]
                    {
                        new PieBehaviorTreeBlackboardEntry
                        {
                            key = "lastDebugAction",
                            type = "string",
                            stringValue = action,
                        },
                    },
                };
            }
        }
    }

    public static class PieBehaviorTreeRuntime
    {
        private const string ReentrantMutationError =
            "Behavior tree runtime mutation is not allowed inside an action receiver. Return blackboardUpdates or queue work for next frame.";
        private const int MaxLogs = 100;
        private const int MaxEvents = 32;
        private static readonly Dictionary<string, BehaviorTreeInstance> Trees =
            new Dictionary<string, BehaviorTreeInstance>(StringComparer.OrdinalIgnoreCase);
        private static int actionExecutionDepth;

        public static int GetActiveTreeCount()
        {
            var count = 0;
            foreach (var instance in Trees.Values)
            {
                if (instance.Root != null && instance.Root.CurrentState == NPNode.State.ACTIVE)
                    count++;
            }
            return count;
        }

        public static string CreateJson(string argsJson)
        {
            EnsureCanMutateRuntime();
            var payload = ParseCreatePayload(argsJson);
            var treeId = FirstNonEmpty(payload.treeId, payload.id, payload.name);
            if (string.IsNullOrWhiteSpace(treeId))
                treeId = "tree_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            if (payload.root == null)
                throw new InvalidOperationException("root is required.");

            if (Trees.ContainsKey(treeId))
            {
                if (!payload.replaceExisting)
                    throw new InvalidOperationException("Behavior tree already exists: " + treeId);
                Destroy(treeId);
            }

            var instance = new BehaviorTreeInstance(treeId, string.IsNullOrWhiteSpace(payload.name) ? treeId : payload.name.Trim());
            SetBlackboardEntries(instance, payload.blackboard);
            instance.Root = new NPRoot(instance.Blackboard, instance.Clock, CompileNode(instance, payload.root));
            Trees[treeId] = instance;
            instance.Log("created");
            if (payload.start)
                Start(treeId);
            return JsonUtility.ToJson(BuildStatus(instance), true);
        }

        public static string StartJson(string argsJson)
        {
            EnsureCanMutateRuntime();
            Start(GetTreeId(argsJson));
            return StatusJson(argsJson);
        }

        public static string StopJson(string argsJson)
        {
            EnsureCanMutateRuntime();
            Stop(GetTreeId(argsJson));
            return StatusJson(argsJson);
        }

        public static string DestroyJson(string argsJson)
        {
            EnsureCanMutateRuntime();
            var treeId = GetTreeId(argsJson);
            Destroy(treeId);
            return "{\"destroyed\":true,\"treeId\":\"" + Escape(treeId) + "\"}";
        }

        public static string StatusJson(string argsJson)
        {
            var payload = ParseIdPayload(argsJson);
            var treeId = FirstNonEmpty(payload.treeId, payload.id);
            if (!string.IsNullOrWhiteSpace(treeId))
            {
                if (!Trees.TryGetValue(treeId, out var instance))
                {
                    return JsonUtility.ToJson(new PieBehaviorTreeStatus
                    {
                        treeId = treeId,
                        found = false,
                        logs = new string[0],
                        blackboard = new PieBehaviorTreeBlackboardEntry[0],
                    }, true);
                }
                return JsonUtility.ToJson(BuildStatus(instance), true);
            }

            var items = new List<PieBehaviorTreeStatus>();
            foreach (var instance in Trees.Values)
                items.Add(BuildStatus(instance));
            return JsonUtility.ToJson(new PieBehaviorTreeStatusResult
            {
                trees = items.ToArray(),
            }, true);
        }

        public static string SetBlackboardJson(string argsJson)
        {
            EnsureCanMutateRuntime();
            var payload = ParseBlackboardPayload(argsJson);
            var instance = RequireTree(FirstNonEmpty(payload.treeId, payload.id));
            SetBlackboardEntries(instance, payload.entries);
            PumpClock(instance, 0.001f, 3, "set_blackboard");
            instance.Log("blackboard updated: " + (payload.entries == null ? 0 : payload.entries.Length) + " entrie(s)");
            return JsonUtility.ToJson(BuildStatus(instance), true);
        }

        public static string GetBlackboardJson(string argsJson)
        {
            var instance = RequireTree(GetTreeId(argsJson));
            return JsonUtility.ToJson(new BlackboardEntriesResult
            {
                treeId = instance.TreeId,
                entries = instance.GetBlackboardSnapshot(),
            }, true);
        }

        public static void StopAll()
        {
            EnsureCanMutateRuntime();
            foreach (var treeId in new List<string>(Trees.Keys))
                Destroy(treeId);
        }

        private static void Start(string treeId)
        {
            var instance = RequireTree(treeId);
            if (instance.Root.CurrentState == NPNode.State.ACTIVE)
                return;
            instance.LastTickSource = "start";
            instance.Root.Start();
            instance.Log("started");
        }

        private static void Stop(string treeId)
        {
            var instance = RequireTree(treeId);
            if (instance.Root.CurrentState == NPNode.State.ACTIVE || instance.Root.CurrentState == NPNode.State.STOP_REQUESTED)
            {
                instance.Root.Stop();
                instance.Log("stopped");
            }
        }

        private static void Destroy(string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId))
                throw new InvalidOperationException("treeId is required.");
            if (!Trees.TryGetValue(treeId, out var instance))
                return;
            if (instance.Root != null && (instance.Root.CurrentState == NPNode.State.ACTIVE || instance.Root.CurrentState == NPNode.State.STOP_REQUESTED))
                instance.Root.Stop();
            Trees.Remove(treeId);
            PieBehaviorTreeActionRegistry.UnregisterTree(treeId);
            instance.Log("destroyed");
        }

        private static BehaviorTreeInstance RequireTree(string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId))
                throw new InvalidOperationException("treeId is required.");
            if (!Trees.TryGetValue(treeId, out var instance))
                throw new InvalidOperationException("Behavior tree not found: " + treeId);
            return instance;
        }

        private static string GetTreeId(string argsJson)
        {
            var payload = ParseIdPayload(argsJson);
            return FirstNonEmpty(payload.treeId, payload.id);
        }

        private static PieBehaviorTreeCreatePayload ParseCreatePayload(string argsJson)
        {
            var map = ParseJsonObject(argsJson);
            return new PieBehaviorTreeCreatePayload
            {
                treeId = GetString(map, "treeId"),
                id = GetString(map, "id"),
                name = GetString(map, "name"),
                root = ParseNodeSpec(GetObject(map, "root")),
                blackboard = ParseBlackboardEntries(GetArray(map, "blackboard")),
                start = GetBool(map, "start", false),
                replaceExisting = GetBool(map, "replaceExisting", true),
            };
        }

        private static PieBehaviorTreeIdPayload ParseIdPayload(string argsJson)
        {
            var map = ParseJsonObject(argsJson);
            return new PieBehaviorTreeIdPayload
            {
                treeId = GetString(map, "treeId"),
                id = GetString(map, "id"),
            };
        }

        private static PieBehaviorTreeBlackboardPayload ParseBlackboardPayload(string argsJson)
        {
            var map = ParseJsonObject(argsJson);
            return new PieBehaviorTreeBlackboardPayload
            {
                treeId = GetString(map, "treeId"),
                id = GetString(map, "id"),
                entries = ParseBlackboardEntries(GetArray(map, "entries")),
            };
        }

        private static PieBehaviorTreeNodeSpec ParseNodeSpec(Dictionary<string, object> map)
        {
            if (map == null)
                return null;

            var childrenValues = GetArray(map, "children");
            var children = new List<PieBehaviorTreeNodeSpec>();
            for (var i = 0; i < childrenValues.Count; i++)
            {
                var child = ParseNodeSpec(childrenValues[i] as Dictionary<string, object>);
                if (child != null)
                    children.Add(child);
            }

            return new PieBehaviorTreeNodeSpec
            {
                type = GetString(map, "type"),
                id = GetString(map, "id"),
                name = GetString(map, "name"),
                children = children.ToArray(),
                child = ParseNodeSpec(GetObject(map, "child")),
                key = GetString(map, "key"),
                valueKey = GetString(map, "valueKey"),
                op = GetString(map, "op"),
                value = ParseBlackboardEntry(GetObject(map, "value")),
                stopsOnChange = GetString(map, "stopsOnChange"),
                seconds = GetFloat(map, "seconds", 0f),
                action = GetString(map, "action"),
                argsJson = GetString(map, "argsJson"),
                intervalSeconds = GetFloat(map, "intervalSeconds", 0f),
                minIntervalSeconds = GetFloat(map, "minIntervalSeconds", 0f),
                successPolicy = GetString(map, "successPolicy"),
                failurePolicy = GetString(map, "failurePolicy"),
            };
        }

        private static PieBehaviorTreeBlackboardEntry[] ParseBlackboardEntries(List<object> values)
        {
            if (values == null || values.Count == 0)
                return new PieBehaviorTreeBlackboardEntry[0];
            var entries = new List<PieBehaviorTreeBlackboardEntry>();
            for (var i = 0; i < values.Count; i++)
            {
                var entry = ParseBlackboardEntry(values[i] as Dictionary<string, object>);
                if (entry != null)
                    entries.Add(entry);
            }
            return entries.ToArray();
        }

        private static PieBehaviorTreeBlackboardEntry ParseBlackboardEntry(Dictionary<string, object> map)
        {
            if (map == null)
                return null;
            return new PieBehaviorTreeBlackboardEntry
            {
                key = GetString(map, "key"),
                type = FirstNonEmpty(GetString(map, "type"), "string"),
                stringValue = GetString(map, "stringValue"),
                numberValue = GetFloat(map, "numberValue", 0f),
                intValue = GetInt(map, "intValue", 0),
                boolValue = GetBool(map, "boolValue", false),
                jsonValue = GetString(map, "jsonValue"),
            };
        }

        private static Dictionary<string, object> ParseJsonObject(string json)
        {
            var parsed = PieBehaviorTreeJson.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var map = parsed as Dictionary<string, object>;
            if (map == null)
                throw new InvalidOperationException("Behavior tree payload must be a JSON object.");
            return map;
        }

        private static Dictionary<string, object> GetObject(Dictionary<string, object> map, string key)
        {
            if (map == null || !map.TryGetValue(key, out var value))
                return null;
            return value as Dictionary<string, object>;
        }

        private static List<object> GetArray(Dictionary<string, object> map, string key)
        {
            if (map == null || !map.TryGetValue(key, out var value))
                return new List<object>();
            return value as List<object> ?? new List<object>();
        }

        private static string GetString(Dictionary<string, object> map, string key)
        {
            if (map == null || !map.TryGetValue(key, out var value) || value == null)
                return "";
            if (value is string text)
                return text;
            if (value is bool boolean)
                return boolean ? "true" : "false";
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
        }

        private static bool GetBool(Dictionary<string, object> map, string key, bool fallback)
        {
            if (map == null || !map.TryGetValue(key, out var value) || value == null)
                return fallback;
            if (value is bool boolean)
                return boolean;
            if (value is string text && bool.TryParse(text, out var parsed))
                return parsed;
            return fallback;
        }

        private static float GetFloat(Dictionary<string, object> map, string key, float fallback)
        {
            if (map == null || !map.TryGetValue(key, out var value) || value == null)
                return fallback;
            if (value is double number)
                return (float)number;
            if (value is float single)
                return single;
            if (value is int integer)
                return integer;
            if (value is string text && float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return fallback;
        }

        private static int GetInt(Dictionary<string, object> map, string key, int fallback)
        {
            if (map == null || !map.TryGetValue(key, out var value) || value == null)
                return fallback;
            if (value is double number)
                return (int)number;
            if (value is int integer)
                return integer;
            if (value is string text && int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return fallback;
        }

        private static NPNode CompileNode(BehaviorTreeInstance instance, PieBehaviorTreeNodeSpec spec, string parentType = "")
        {
            if (spec == null)
                throw new InvalidOperationException("Behavior tree node is required.");
            var type = (spec.type ?? "").Trim().ToLowerInvariant();
            NPNode node;
            switch (type)
            {
                case "selector":
                    node = new NPBehave.Selector(CompileChildren(instance, spec, type));
                    break;
                case "sequence":
                    node = new NPBehave.Sequence(CompileChildren(instance, spec, type));
                    break;
                case "parallel":
                    node = new NPBehave.Parallel(
                        ParseParallelPolicy(spec.successPolicy, NPBehave.Parallel.Policy.ALL),
                        ParseParallelPolicy(spec.failurePolicy, NPBehave.Parallel.Policy.ONE),
                        CompileChildren(instance, spec, type));
                    break;
                case "repeat":
                    node = CompileRepeatNode(instance, spec);
                    break;
                case "condition":
                    node = CompileConditionNode(instance, spec, parentType);
                    break;
                case "wait":
                    node = new NPBehave.Wait(Math.Max(0f, spec.seconds));
                    break;
                case "action":
                    node = new NPAction(() => ExecuteAction(instance, spec));
                    break;
                case "succeed":
                    node = new NPAction(() => true);
                    break;
                case "fail":
                    node = new NPAction(() => false);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported behavior tree node type: " + type);
            }

            var label = FirstNonEmpty(spec.id, spec.name, spec.action, type);
            if (!string.IsNullOrWhiteSpace(label))
                node.Label = label;
            return node;
        }

        private static NPNode CompileRepeatNode(BehaviorTreeInstance instance, PieBehaviorTreeNodeSpec spec)
        {
            if (spec.child == null)
                throw new InvalidOperationException("repeat node requires child.");
            var child = CompileNode(instance, spec.child, "repeat");
            var interval = Math.Max(0f, spec.intervalSeconds);
            if (interval > 0f)
                child = new NPBehave.Sequence(new NPBehave.Succeeder(child), new NPBehave.Wait(interval, 0f));
            return new NPBehave.Repeater(child);
        }

        private static NPNode[] CompileChildren(BehaviorTreeInstance instance, PieBehaviorTreeNodeSpec spec, string parentType)
        {
            var children = spec.children ?? new PieBehaviorTreeNodeSpec[0];
            if (children.Length == 0)
                throw new InvalidOperationException(spec.type + " node requires children.");
            var nodes = new NPNode[children.Length];
            for (var i = 0; i < children.Length; i++)
                nodes[i] = CompileNode(instance, children[i], parentType);
            return nodes;
        }

        private static NPNode CompileConditionNode(BehaviorTreeInstance instance, PieBehaviorTreeNodeSpec spec, string parentType)
        {
            var key = RequireText(spec.key, "condition.key");
            var valueKey = (spec.valueKey ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(valueKey) && spec.value != null)
                throw new InvalidOperationException("condition.value and condition.valueKey cannot both be set.");
            var stops = ParseStopsForParent(spec.stopsOnChange, parentType);

            if (!string.IsNullOrWhiteSpace(valueKey))
            {
                var op = ParseOperator(spec.op, new PieBehaviorTreeBlackboardEntry { type = "string" });
                return new NPBehave.BlackboardQuery(
                    new[] { key, valueKey },
                    stops,
                    () => CompareBlackboardValues(instance.Blackboard.Get(key), op, instance.Blackboard.Get(valueKey)),
                    CompileNode(instance, spec.child, "condition"));
            }

            var literalOp = ParseOperator(spec.op, spec.value);
            var literalValue = spec.value == null ? null : ToBlackboardValue(spec.value);
            return new NPBehave.BlackboardQuery(
                new[] { key },
                stops,
                () => CompareBlackboardValues(instance.Blackboard.Get(key), literalOp, literalValue),
                CompileNode(instance, spec.child, "condition"));
        }

        private static bool ExecuteAction(BehaviorTreeInstance instance, PieBehaviorTreeNodeSpec spec)
        {
            var actionName = RequireText(spec.action, "action.action");
            var argsJson = string.IsNullOrWhiteSpace(spec.argsJson) ? "{}" : spec.argsJson;
            var throttleKey = BuildActionThrottleKey(spec, actionName, argsJson);
            var actionLabel = FirstNonEmpty(spec.id, spec.name, actionName);
            var nowUnixMs = NowUnixMs();
            var minIntervalMs = (long)(Math.Max(0f, spec.minIntervalSeconds) * 1000f);
            if (minIntervalMs > 0
                && instance.ActionThrottleStates.TryGetValue(throttleKey, out var throttle)
                && throttle.LastExecutedAtUnixMs > 0
                && nowUnixMs - throttle.LastExecutedAtUnixMs < minIntervalMs)
            {
                instance.LastSkippedAction = actionLabel;
                instance.SkippedActionCount++;
                instance.Log("action " + actionLabel + " skipped minIntervalSeconds=" + spec.minIntervalSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                return throttle.LastSuccess;
            }

            var context = new PieBehaviorTreeActionContext
            {
                TreeId = instance.TreeId,
                ActionName = actionName,
                ArgsJson = argsJson,
                Blackboard = instance.GetBlackboardSnapshot(),
            };
            actionExecutionDepth++;
            instance.IsExecutingAction = true;
            try
            {
                var success = PieBehaviorTreeActionRegistry.TryExecute(context, out var result);
                result = result ?? new PieBehaviorTreeActionResult { success = false, error = "Action returned no result." };
                if (result.blackboardUpdates != null && result.blackboardUpdates.Length > 0)
                    SetBlackboardEntries(instance, result.blackboardUpdates);
                if (!string.IsNullOrWhiteSpace(result.eventType))
                    instance.RecordEvent(result.eventType, result.eventJson);
                instance.ActionCount++;
                instance.LastAction = actionName;
                instance.LastActionSuccess = success;
                instance.LastActionResultJson = string.IsNullOrWhiteSpace(result.resultJson) ? "{}" : result.resultJson;
                instance.LastError = success ? "" : result.error;
                instance.LastActionAtUnixMs = nowUnixMs;
                instance.ActionThrottleStates[throttleKey] = new ActionThrottleState
                {
                    LastExecutedAtUnixMs = nowUnixMs,
                    LastSuccess = success,
                };
                instance.Log("action " + actionName + " => " + (success ? "success" : "failed"));
                return success;
            }
            finally
            {
                actionExecutionDepth = Math.Max(0, actionExecutionDepth - 1);
                instance.IsExecutingAction = false;
            }
        }

        private static void SetBlackboardEntries(BehaviorTreeInstance instance, PieBehaviorTreeBlackboardEntry[] entries)
        {
            if (entries == null)
                return;
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;
                var normalized = CloneEntry(entry);
                if (string.Equals(normalized.type, "null", StringComparison.OrdinalIgnoreCase))
                {
                    instance.BlackboardMirror.Remove(normalized.key);
                    instance.Blackboard.Unset(normalized.key);
                }
                else
                {
                    instance.BlackboardMirror[normalized.key] = normalized;
                    instance.Blackboard[normalized.key] = ToBlackboardValue(normalized);
                }
                instance.LastBlackboardWriteAtUnixMs = NowUnixMs();
            }
        }

        private static void PumpClock(BehaviorTreeInstance instance, float deltaTime, int maxTicks, string source)
        {
            if (instance == null || instance.Clock == null)
                return;
            instance.LastTickSource = string.IsNullOrWhiteSpace(source) ? "manual" : source;
            var ticks = Math.Max(1, maxTicks);
            for (var i = 0; i < ticks; i++)
            {
                instance.TickCount++;
                instance.Clock.Update(deltaTime);
            }
        }

        private static void EnsureCanMutateRuntime()
        {
            if (actionExecutionDepth > 0)
                throw new InvalidOperationException(ReentrantMutationError);
        }

        private static bool CompareBlackboardValues(object left, NPBehave.Operator op, object right)
        {
            if (op == NPBehave.Operator.IS_SET)
                return left != null;
            if (op == NPBehave.Operator.IS_NOT_SET)
                return left == null;
            if (op == NPBehave.Operator.ALWAYS_TRUE)
                return true;

            if (left == null || right == null)
            {
                switch (op)
                {
                    case NPBehave.Operator.IS_EQUAL:
                        return left == null && right == null;
                    case NPBehave.Operator.IS_NOT_EQUAL:
                        return !(left == null && right == null);
                    default:
                        return false;
                }
            }

            if (TryGetNumber(left, out var leftNumber) && TryGetNumber(right, out var rightNumber))
                return CompareComparable(leftNumber.CompareTo(rightNumber), op);

            if (left is bool || right is bool)
            {
                if (left is bool typedLeft && right is bool typedRight)
                {
                    switch (op)
                    {
                        case NPBehave.Operator.IS_EQUAL:
                            return typedLeft == typedRight;
                        case NPBehave.Operator.IS_NOT_EQUAL:
                            return typedLeft != typedRight;
                        default:
                            return false;
                    }
                }
                return false;
            }

            var comparison = string.Compare(
                Convert.ToString(left, System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToString(right, System.Globalization.CultureInfo.InvariantCulture),
                StringComparison.Ordinal);
            return CompareComparable(comparison, op);
        }

        private static string BuildActionThrottleKey(PieBehaviorTreeNodeSpec spec, string actionName, string argsJson)
        {
            var id = (spec.id ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(id))
                return "id:" + id;
            return "action:" + actionName + "\nargs:" + (argsJson ?? "{}");
        }

        private static bool CompareComparable(int comparison, NPBehave.Operator op)
        {
            switch (op)
            {
                case NPBehave.Operator.IS_EQUAL:
                    return comparison == 0;
                case NPBehave.Operator.IS_NOT_EQUAL:
                    return comparison != 0;
                case NPBehave.Operator.IS_GREATER:
                    return comparison > 0;
                case NPBehave.Operator.IS_GREATER_OR_EQUAL:
                    return comparison >= 0;
                case NPBehave.Operator.IS_SMALLER:
                    return comparison < 0;
                case NPBehave.Operator.IS_SMALLER_OR_EQUAL:
                    return comparison <= 0;
                case NPBehave.Operator.ALWAYS_TRUE:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetNumber(object value, out float number)
        {
            number = 0f;
            if (value is float single)
            {
                number = single;
                return true;
            }
            if (value is double dbl)
            {
                number = (float)dbl;
                return true;
            }
            if (value is int integer)
            {
                number = integer;
                return true;
            }
            if (value is long longInteger)
            {
                number = longInteger;
                return true;
            }
            return false;
        }

        private static object ToBlackboardValue(PieBehaviorTreeBlackboardEntry entry)
        {
            var type = (entry.type ?? "string").Trim().ToLowerInvariant();
            switch (type)
            {
                case "bool":
                case "boolean":
                    return entry.boolValue;
                case "int":
                case "integer":
                    return entry.intValue;
                case "float":
                case "number":
                    return entry.numberValue;
                case "json":
                    return entry.jsonValue ?? "";
                case "null":
                    return null;
                case "string":
                default:
                    return entry.stringValue ?? "";
            }
        }

        private static PieBehaviorTreeBlackboardEntry CloneEntry(PieBehaviorTreeBlackboardEntry entry)
        {
            return new PieBehaviorTreeBlackboardEntry
            {
                key = (entry.key ?? "").Trim(),
                type = string.IsNullOrWhiteSpace(entry.type) ? "string" : entry.type.Trim(),
                stringValue = entry.stringValue ?? "",
                numberValue = entry.numberValue,
                intValue = entry.intValue,
                boolValue = entry.boolValue,
                jsonValue = entry.jsonValue ?? "",
            };
        }

        private static NPBehave.Operator ParseOperator(string op, PieBehaviorTreeBlackboardEntry value)
        {
            op = (op ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(op))
                return value == null ? NPBehave.Operator.IS_SET : NPBehave.Operator.IS_EQUAL;
            switch (op)
            {
                case "is_set":
                case "set":
                    return NPBehave.Operator.IS_SET;
                case "is_not_set":
                case "not_set":
                    return NPBehave.Operator.IS_NOT_SET;
                case "is_equal":
                case "equal":
                case "==":
                    return NPBehave.Operator.IS_EQUAL;
                case "is_not_equal":
                case "not_equal":
                case "!=":
                    return NPBehave.Operator.IS_NOT_EQUAL;
                case "is_greater_or_equal":
                case ">=":
                    return NPBehave.Operator.IS_GREATER_OR_EQUAL;
                case "is_greater":
                case ">":
                    return NPBehave.Operator.IS_GREATER;
                case "is_smaller_or_equal":
                case "<=":
                    return NPBehave.Operator.IS_SMALLER_OR_EQUAL;
                case "is_smaller":
                case "<":
                    return NPBehave.Operator.IS_SMALLER;
                case "always_true":
                case "true":
                    return NPBehave.Operator.ALWAYS_TRUE;
                default:
                    throw new InvalidOperationException("Unsupported behavior tree condition op: " + op);
            }
        }

        private static NPBehave.Stops ParseStops(string stops)
        {
            stops = (stops ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(stops))
                return NPBehave.Stops.NONE;
            switch (stops)
            {
                case "none":
                    return NPBehave.Stops.NONE;
                case "self":
                    return NPBehave.Stops.SELF;
                case "lower_priority":
                    return NPBehave.Stops.LOWER_PRIORITY;
                case "both":
                    return NPBehave.Stops.BOTH;
                case "immediate_restart":
                    return NPBehave.Stops.IMMEDIATE_RESTART;
                case "lower_priority_immediate_restart":
                    return NPBehave.Stops.LOWER_PRIORITY_IMMEDIATE_RESTART;
                default:
                    throw new InvalidOperationException("Unsupported behavior tree stopsOnChange: " + stops);
            }
        }

        private static NPBehave.Stops ParseStopsForParent(string stops, string parentType)
        {
            var parsed = ParseStops(stops);
            if (string.Equals((parentType ?? "").Trim(), "parallel", StringComparison.OrdinalIgnoreCase)
                && (parsed == NPBehave.Stops.LOWER_PRIORITY
                    || parsed == NPBehave.Stops.BOTH
                    || parsed == NPBehave.Stops.LOWER_PRIORITY_IMMEDIATE_RESTART))
                throw new InvalidOperationException("condition.stopsOnChange=" + (string.IsNullOrWhiteSpace(stops) ? "none" : stops)
                    + " is unsupported for a direct child of parallel; use none, self, or immediate_restart.");
            return parsed;
        }

        private static NPBehave.Parallel.Policy ParseParallelPolicy(string policy, NPBehave.Parallel.Policy fallback)
        {
            policy = (policy ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(policy))
                return fallback;
            if (policy == "one")
                return NPBehave.Parallel.Policy.ONE;
            if (policy == "all")
                return NPBehave.Parallel.Policy.ALL;
            throw new InvalidOperationException("Unsupported behavior tree parallel policy: " + policy);
        }

        private static PieBehaviorTreeStatus BuildStatus(BehaviorTreeInstance instance)
        {
            var rootState = instance.Root == null ? "missing" : instance.Root.CurrentState.ToString();
            var elapsedSeconds = Math.Max(0.001f, (NowUnixMs() - instance.CreatedAtUnixMs) / 1000f);
            return new PieBehaviorTreeStatus
            {
                treeId = instance.TreeId,
                name = instance.Name,
                found = true,
                isActive = instance.Root != null && instance.Root.CurrentState == NPNode.State.ACTIVE,
                state = rootState,
                rootState = rootState,
                actionCount = instance.ActionCount,
                tickCount = instance.TickCount,
                actionRatePerSecond = instance.ActionCount / elapsedSeconds,
                lastAction = instance.LastAction,
                lastActionSuccess = instance.LastActionSuccess,
                lastActionResultJson = instance.LastActionResultJson,
                lastActionAtUnixMs = instance.LastActionAtUnixMs,
                lastBlackboardWriteAtUnixMs = instance.LastBlackboardWriteAtUnixMs,
                lastSkippedAction = instance.LastSkippedAction,
                skippedActionCount = instance.SkippedActionCount,
                lastEventType = instance.LastEventType,
                lastEventJson = instance.LastEventJson,
                eventCount = instance.EventCount,
                lastError = instance.LastError,
                isExecutingAction = instance.IsExecutingAction,
                lastTickSource = instance.LastTickSource,
                blackboard = instance.GetBlackboardSnapshot(),
                logs = instance.Logs.ToArray(),
            };
        }

        private static string RequireText(string value, string label)
        {
            value = (value ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(label + " is required.");
            return value;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
                return "";
            for (var i = 0; i < values.Length; i++)
            {
                var value = (values[i] ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return "";
        }

        private static string Escape(string value)
        {
            return (value ?? "")
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private static long NowUnixMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private sealed class BehaviorTreeInstance
        {
            public readonly string TreeId;
            public readonly string Name;
            public readonly long CreatedAtUnixMs;
            public readonly NPBehave.Clock Clock;
            public readonly NPBlackboard Blackboard;
            public readonly Dictionary<string, PieBehaviorTreeBlackboardEntry> BlackboardMirror =
                new Dictionary<string, PieBehaviorTreeBlackboardEntry>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, ActionThrottleState> ActionThrottleStates =
                new Dictionary<string, ActionThrottleState>(StringComparer.OrdinalIgnoreCase);
            public readonly List<string> Logs = new List<string>();
            public readonly List<BehaviorTreeEvent> Events = new List<BehaviorTreeEvent>();
            public NPRoot Root;
            public int ActionCount;
            public int TickCount;
            public string LastAction = "";
            public bool LastActionSuccess;
            public string LastActionResultJson = "";
            public long LastActionAtUnixMs;
            public long LastBlackboardWriteAtUnixMs;
            public string LastSkippedAction = "";
            public int SkippedActionCount;
            public string LastEventType = "";
            public string LastEventJson = "";
            public int EventCount;
            public string LastError = "";
            public bool IsExecutingAction;
            public string LastTickSource = "";

            public BehaviorTreeInstance(string treeId, string name)
            {
                TreeId = treeId;
                Name = name;
                CreatedAtUnixMs = NowUnixMs();
                Clock = NPBehave.UnityContext.GetClock();
                Blackboard = new NPBlackboard(Clock);
            }

            public PieBehaviorTreeBlackboardEntry[] GetBlackboardSnapshot()
            {
                var entries = new List<PieBehaviorTreeBlackboardEntry>();
                foreach (var item in BlackboardMirror.Values)
                    entries.Add(CloneEntry(item));
                entries.Sort((left, right) => string.Compare(left.key, right.key, StringComparison.OrdinalIgnoreCase));
                return entries.ToArray();
            }

            public void Log(string message)
            {
                Logs.Add(DateTime.UtcNow.ToString("o") + " " + (message ?? ""));
                if (Logs.Count > MaxLogs)
                    Logs.RemoveRange(0, Logs.Count - MaxLogs);
            }

            public void RecordEvent(string eventType, string eventJson)
            {
                eventType = (eventType ?? "").Trim();
                if (string.IsNullOrWhiteSpace(eventType))
                    return;
                eventJson = string.IsNullOrWhiteSpace(eventJson) ? "{}" : eventJson.Trim();
                LastEventType = eventType;
                LastEventJson = eventJson;
                EventCount++;
                Events.Add(new BehaviorTreeEvent
                {
                    type = eventType,
                    json = eventJson,
                    unixMs = NowUnixMs(),
                });
                if (Events.Count > MaxEvents)
                    Events.RemoveRange(0, Events.Count - MaxEvents);
                Log("event " + eventType + " " + eventJson);
            }
        }

        private sealed class ActionThrottleState
        {
            public long LastExecutedAtUnixMs;
            public bool LastSuccess;
        }

        private sealed class BehaviorTreeEvent
        {
            public string type = "";
            public string json = "{}";
            public long unixMs;
        }

        [Serializable]
        private sealed class BlackboardEntriesResult
        {
            public string treeId = "";
            public PieBehaviorTreeBlackboardEntry[] entries = new PieBehaviorTreeBlackboardEntry[0];
        }

        private sealed class PieBehaviorTreeJson
        {
            private readonly string json;
            private int index;

            private PieBehaviorTreeJson(string json)
            {
                this.json = json ?? "";
            }

            public static object Parse(string json)
            {
                var parser = new PieBehaviorTreeJson(json);
                var value = parser.ParseValue();
                parser.SkipWhitespace();
                if (!parser.IsEnd)
                    throw new InvalidOperationException("Unexpected JSON content at offset " + parser.index + ".");
                return value;
            }

            private bool IsEnd => index >= json.Length;

            private object ParseValue()
            {
                SkipWhitespace();
                if (IsEnd)
                    throw new InvalidOperationException("Unexpected end of JSON.");
                var c = json[index];
                if (c == '{')
                    return ParseObject();
                if (c == '[')
                    return ParseArray();
                if (c == '"')
                    return ParseString();
                if (c == 't')
                    return ParseLiteral("true", true);
                if (c == 'f')
                    return ParseLiteral("false", false);
                if (c == 'n')
                    return ParseLiteral("null", null);
                if (c == '-' || (c >= '0' && c <= '9'))
                    return ParseNumber();
                throw new InvalidOperationException("Unexpected JSON character '" + c + "' at offset " + index + ".");
            }

            private Dictionary<string, object> ParseObject()
            {
                Expect('{');
                var map = new Dictionary<string, object>(StringComparer.Ordinal);
                SkipWhitespace();
                if (TryConsume('}'))
                    return map;
                while (true)
                {
                    SkipWhitespace();
                    var key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    map[key] = ParseValue();
                    SkipWhitespace();
                    if (TryConsume('}'))
                        return map;
                    Expect(',');
                }
            }

            private List<object> ParseArray()
            {
                Expect('[');
                var values = new List<object>();
                SkipWhitespace();
                if (TryConsume(']'))
                    return values;
                while (true)
                {
                    values.Add(ParseValue());
                    SkipWhitespace();
                    if (TryConsume(']'))
                        return values;
                    Expect(',');
                }
            }

            private string ParseString()
            {
                Expect('"');
                var result = new System.Text.StringBuilder();
                while (!IsEnd)
                {
                    var c = json[index++];
                    if (c == '"')
                        return result.ToString();
                    if (c != '\\')
                    {
                        result.Append(c);
                        continue;
                    }
                    if (IsEnd)
                        throw new InvalidOperationException("Unterminated JSON string escape.");
                    var escaped = json[index++];
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
                            result.Append(ParseUnicodeEscape());
                            break;
                        default:
                            throw new InvalidOperationException("Unsupported JSON string escape: \\" + escaped + ".");
                    }
                }
                throw new InvalidOperationException("Unterminated JSON string.");
            }

            private string ParseUnicodeEscape()
            {
                if (index + 4 > json.Length)
                    throw new InvalidOperationException("Incomplete JSON unicode escape.");
                var hex = json.Substring(index, 4);
                index += 4;
                if (!int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var codePoint))
                    throw new InvalidOperationException("Invalid JSON unicode escape: " + hex + ".");
                return char.ConvertFromUtf32(codePoint);
            }

            private object ParseLiteral(string literal, object value)
            {
                if (index + literal.Length > json.Length || string.CompareOrdinal(json, index, literal, 0, literal.Length) != 0)
                    throw new InvalidOperationException("Invalid JSON literal at offset " + index + ".");
                index += literal.Length;
                return value;
            }

            private double ParseNumber()
            {
                var start = index;
                if (json[index] == '-')
                    index++;
                ConsumeDigits();
                if (!IsEnd && json[index] == '.')
                {
                    index++;
                    ConsumeDigits();
                }
                if (!IsEnd && (json[index] == 'e' || json[index] == 'E'))
                {
                    index++;
                    if (!IsEnd && (json[index] == '+' || json[index] == '-'))
                        index++;
                    ConsumeDigits();
                }
                var raw = json.Substring(start, index - start);
                if (!double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
                    throw new InvalidOperationException("Invalid JSON number: " + raw + ".");
                return value;
            }

            private void ConsumeDigits()
            {
                var start = index;
                while (!IsEnd && json[index] >= '0' && json[index] <= '9')
                    index++;
                if (start == index)
                    throw new InvalidOperationException("Expected JSON digit at offset " + index + ".");
            }

            private bool TryConsume(char expected)
            {
                if (IsEnd || json[index] != expected)
                    return false;
                index++;
                return true;
            }

            private void Expect(char expected)
            {
                if (IsEnd || json[index] != expected)
                    throw new InvalidOperationException("Expected JSON character '" + expected + "' at offset " + index + ".");
                index++;
            }

            private void SkipWhitespace()
            {
                while (!IsEnd)
                {
                    var c = json[index];
                    if (c != ' ' && c != '\n' && c != '\r' && c != '\t')
                        return;
                    index++;
                }
            }
        }
    }
}
