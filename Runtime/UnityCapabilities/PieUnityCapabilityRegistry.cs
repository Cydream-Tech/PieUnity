#if PIE_UNITY_SPLIT_SOURCES
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pie
{
    public static class PieUnityCapabilityRegistry
    {
        private sealed class CapabilityRegistration
        {
            public PieUnityCapabilityDescriptor Descriptor;
            public Func<string, string> Handler;
        }

        private sealed class RuntimeHostRegistration
        {
            public string Namespace = "";
            public string DisplayName = "";
            public readonly HashSet<string> ToolKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> RpcKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> CapabilityNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly Dictionary<string, CapabilityRegistration> RpcMethods = new Dictionary<string, CapabilityRegistration>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, CapabilityRegistration> ToolMethods = new Dictionary<string, CapabilityRegistration>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, RuntimeHostRegistration> RuntimeHosts = new Dictionary<string, RuntimeHostRegistration>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> RuntimeHostDiagnostics = new List<string>();
        private static readonly object SyncRoot = new object();
        private static string _instanceId = "";
        private static string _projectPath = "";
        private static string _mode = "";

        public static void ConfigureContext(string instanceId, string projectPath, string mode)
        {
            lock (SyncRoot)
            {
                _instanceId = instanceId ?? "";
                _projectPath = projectPath ?? "";
                _mode = mode ?? "";
            }
        }

        public static string InstanceId
        {
            get { lock (SyncRoot) return _instanceId; }
        }

        public static string ProjectPath
        {
            get { lock (SyncRoot) return _projectPath; }
        }

        public static string Mode
        {
            get { lock (SyncRoot) return _mode; }
        }

        public static void RegisterRpc(
            string name,
            string ns,
            string description,
            string mode,
            bool readOnly,
            bool deprecated,
            string[] aliases,
            PieUnityParameterDescriptor[] parameters,
            Func<string, string> handler,
            string capabilityKind = "host",
            bool convenience = false,
            bool requiresMainThread = true,
            string owner = "",
            string writeScope = "",
            string returns = "",
            string recommendedWorkflow = "",
            string[] examples = null,
            string[] errorCodes = null,
            bool destructive = false,
            bool canTriggerDomainReload = false)
        {
            RegisterBuiltinInternal(
                RpcMethods,
                "rpc",
                name,
                ns,
                description,
                mode,
                readOnly,
                deprecated,
                aliases,
                parameters,
                handler,
                capabilityKind,
                convenience,
                requiresMainThread,
                owner,
                writeScope,
                returns,
                recommendedWorkflow,
                examples,
                errorCodes,
                destructive,
                canTriggerDomainReload);
        }

        public static void RegisterTool(
            string name,
            string ns,
            string description,
            string mode,
            bool readOnly,
            bool deprecated,
            string[] aliases,
            PieUnityParameterDescriptor[] parameters,
            Func<string, string> handler,
            string capabilityKind = "host",
            bool convenience = false,
            bool requiresMainThread = true,
            string owner = "",
            string writeScope = "",
            string returns = "",
            string recommendedWorkflow = "",
            string[] examples = null,
            string[] errorCodes = null,
            bool destructive = false,
            bool canTriggerDomainReload = false)
        {
            RegisterBuiltinInternal(
                ToolMethods,
                "tool",
                name,
                ns,
                description,
                mode,
                readOnly,
                deprecated,
                aliases,
                parameters,
                handler,
                capabilityKind,
                convenience,
                requiresMainThread,
                owner,
                writeScope,
                returns,
                recommendedWorkflow,
                examples,
                errorCodes,
                destructive,
                canTriggerDomainReload);
        }

        public static bool TryReplaceRuntimeHost(
            string hostNamespace,
            string hostDisplayName,
            PieUnityCapabilityDescriptor[] descriptors,
            Func<PieUnityCapabilityDescriptor, Func<string, string>> handlerFactory,
            out string error)
        {
            lock (SyncRoot)
            {
                var normalizedNamespace = NormalizeHostNamespace(hostNamespace);
                if (string.IsNullOrWhiteSpace(normalizedNamespace))
                {
                    error = "Runtime host namespace is required.";
                    AppendRuntimeHostDiagnostic(error);
                    return false;
                }

                if (handlerFactory == null)
                {
                    error = $"Runtime host {normalizedNamespace} is missing a handler factory.";
                    AppendRuntimeHostDiagnostic(error);
                    return false;
                }

                var items = descriptors ?? new PieUnityCapabilityDescriptor[0];
                if (items.Length == 0)
                {
                    error = $"Runtime host {normalizedNamespace} did not register any capabilities.";
                    AppendRuntimeHostDiagnostic(error);
                    return false;
                }

                var validationError = ValidateRuntimeHostDescriptors(normalizedNamespace, items);
                if (!string.IsNullOrWhiteSpace(validationError))
                {
                    error = validationError;
                    AppendRuntimeHostDiagnostic(error);
                    return false;
                }

                RemoveRuntimeHostLocked(normalizedNamespace);
                var registration = new RuntimeHostRegistration
                {
                    Namespace = normalizedNamespace,
                    DisplayName = string.IsNullOrWhiteSpace(hostDisplayName) ? normalizedNamespace : hostDisplayName.Trim(),
                };

                for (var i = 0; i < items.Length; i++)
                {
                    var descriptor = CloneDescriptor(items[i]);
                    descriptor.source = "runtime_host";
                    descriptor.hostNamespace = normalizedNamespace;
                    descriptor.hostDisplayName = registration.DisplayName;
                    descriptor.ns = string.IsNullOrWhiteSpace(descriptor.ns) ? normalizedNamespace : descriptor.ns;
                    descriptor.mode = string.IsNullOrWhiteSpace(descriptor.mode) ? "runtime" : descriptor.mode;
                    descriptor.availableIn = descriptor.mode;
                    descriptor.aliases = descriptor.aliases ?? new string[0];
                    descriptor.parameters = descriptor.parameters ?? new PieUnityParameterDescriptor[0];
                    descriptor.examples = descriptor.examples ?? new string[0];
                    descriptor.errorCodes = descriptor.errorCodes ?? new string[0];

                    var handler = handlerFactory(descriptor);
                    if (handler == null)
                    {
                        error = $"Runtime host {normalizedNamespace} did not provide a handler for capability {descriptor.name}.";
                        AppendRuntimeHostDiagnostic(error);
                        RemoveRuntimeHostLocked(normalizedNamespace);
                        return false;
                    }

                    RegisterRegistrationLocked(
                        string.Equals(descriptor.kind, "rpc", StringComparison.OrdinalIgnoreCase) ? RpcMethods : ToolMethods,
                        descriptor,
                        handler,
                        descriptor.kind,
                        descriptor.aliases,
                        registration);
                }

                RuntimeHosts[normalizedNamespace] = registration;
                error = "";
                return true;
            }
        }

        public static void UnregisterRuntimeHost(string hostNamespace)
        {
            lock (SyncRoot)
            {
                RemoveRuntimeHostLocked(NormalizeHostNamespace(hostNamespace));
            }
        }

        public static void ResetRuntimeHosts()
        {
            lock (SyncRoot)
            {
                var namespaces = RuntimeHosts.Keys.ToArray();
                for (var i = 0; i < namespaces.Length; i++)
                    RemoveRuntimeHostLocked(namespaces[i]);
                RuntimeHosts.Clear();
                RuntimeHostDiagnostics.Clear();
            }
        }

        public static string[] GetRegisteredRuntimeHostNamespaces()
        {
            lock (SyncRoot)
            {
                return RuntimeHosts.Keys
                    .OrderBy((item) => item, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }

        public static int GetRuntimeHostCount()
        {
            lock (SyncRoot)
                return RuntimeHosts.Count;
        }

        public static int GetRuntimeHostCapabilityCount()
        {
            lock (SyncRoot)
                return RuntimeHosts.Values.Sum((entry) => entry.CapabilityNames.Count);
        }

        public static string[] GetRuntimeHostDiagnostics()
        {
            lock (SyncRoot)
                return RuntimeHostDiagnostics.ToArray();
        }

        public static bool TryInvokeRpc(string name, string argsJson, out string resultJson, out string error, out bool handlerInvoked)
        {
            return TryInvoke("rpc", RpcMethods, name, argsJson, out resultJson, out error, out handlerInvoked);
        }

        public static bool TryInvokeTool(string name, string argsJson, out string resultJson, out string error, out bool handlerInvoked)
        {
            return TryInvoke("tool", ToolMethods, name, argsJson, out resultJson, out error, out handlerInvoked);
        }

        public static bool HasTool(string name)
        {
            lock (SyncRoot)
                return ToolMethods.ContainsKey(name ?? "");
        }

        public static bool HasRpc(string name)
        {
            lock (SyncRoot)
                return RpcMethods.ContainsKey(name ?? "");
        }

        public static PieUnityCapabilityDescriptor[] GetManifestEntries(string filterNamespace, string filterName)
        {
            lock (SyncRoot)
            {
                var all = RpcMethods.Values
                    .Concat(ToolMethods.Values)
                    .Select((entry) => entry.Descriptor)
                    .GroupBy((entry) => $"{entry.kind}:{entry.name}", StringComparer.OrdinalIgnoreCase)
                    .Select((group) => group.First())
                    .OrderBy((entry) => entry.kind, StringComparer.Ordinal)
                    .ThenBy((entry) => entry.name, StringComparer.Ordinal)
                    .ToArray();

                if (!string.IsNullOrWhiteSpace(filterName))
                {
                    return all.Where((entry) => string.Equals(entry.name, filterName, StringComparison.OrdinalIgnoreCase)).ToArray();
                }

                if (!string.IsNullOrWhiteSpace(filterNamespace))
                {
                    return all.Where((entry) => string.Equals(entry.ns, filterNamespace, StringComparison.OrdinalIgnoreCase)).ToArray();
                }

                return all;
            }
        }

        public static string BuildManifestJson(string filterNamespace, string filterName)
        {
            var instanceId = InstanceId;
            var projectPath = ProjectPath;
            var mode = Mode;
            if (!string.IsNullOrWhiteSpace(filterNamespace) || !string.IsNullOrWhiteSpace(filterName))
            {
                var detail = new PieUnityManifestDetail
                {
                    instanceId = instanceId,
                    projectPath = projectPath,
                    projectName = DeriveProjectName(projectPath),
                    productName = PieUnityCapabilitiesBootstrap.UnityProductName ?? "",
                    applicationIdentifier = PieUnityCapabilitiesBootstrap.ApplicationIdentifier ?? "",
                    mode = mode,
                    filterNamespace = filterNamespace ?? "",
                    filterName = filterName ?? "",
                    capabilities = GetManifestEntries(filterNamespace, filterName),
                };
                return JsonUtility.ToJson(detail, true);
            }

            var entries = GetManifestEntries(null, null);
            var namespaces = entries
                .Select((entry) => entry.ns)
                .Where((entry) => !string.IsNullOrWhiteSpace(entry))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy((entry) => entry, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var summary = new PieUnityManifestSummary
            {
                instanceId = instanceId,
                projectPath = projectPath,
                projectName = DeriveProjectName(projectPath),
                productName = PieUnityCapabilitiesBootstrap.UnityProductName ?? "",
                applicationIdentifier = PieUnityCapabilitiesBootstrap.ApplicationIdentifier ?? "",
                mode = mode,
                namespaces = namespaces,
                stableTools = entries.Count((entry) => string.Equals(entry.kind, "tool", StringComparison.OrdinalIgnoreCase) && !entry.deprecated),
                deprecatedAliases = entries.Count((entry) => entry.deprecated),
            };
            return JsonUtility.ToJson(summary, true);
        }

        private static bool TryInvoke(
            string kind,
            Dictionary<string, CapabilityRegistration> source,
            string name,
            string argsJson,
            out string resultJson,
            out string error,
            out bool handlerInvoked)
        {
            lock (SyncRoot)
            {
                // Structured dispatch outcome: a lookup miss (nothing executed)
                // is reported separately from a handler failure, so callers can
                // safely fall through to another dispatch layer only when no
                // handler ran — handler error text can never spoof a miss and
                // cause a side-effecting handler to re-run.
                handlerInvoked = false;
                if (!source.TryGetValue(name ?? "", out var registration))
                {
                    resultJson = "null";
                    error = $"Capability not found: {name}";
                    PieDiagnostics.Warning($"[PieUnityCapabilityRegistry] {kind} {name} failed: {error}");
                    return false;
                }

                try
                {
                    handlerInvoked = true;
                    resultJson = registration.Handler(argsJson ?? "{}") ?? "null";
                    error = "";
                    return true;
                }
                catch (Exception ex)
                {
                    resultJson = "null";
                    error = ex.Message;
                    PieDiagnostics.Warning($"[PieUnityCapabilityRegistry] {kind} {name} failed: {ex.Message}");
                    return false;
                }
            }
        }

        private static void RegisterBuiltinInternal(
            Dictionary<string, CapabilityRegistration> source,
            string kind,
            string name,
            string ns,
            string description,
            string capabilityMode,
            bool readOnly,
            bool deprecated,
            string[] aliases,
            PieUnityParameterDescriptor[] parameters,
            Func<string, string> handler,
            string capabilityKind,
            bool convenience,
            bool requiresMainThread,
            string capabilityOwner,
            string writeScope,
            string returnValue,
            string recommendedWorkflow,
            string[] examples,
            string[] errorCodes,
            bool destructive,
            bool canTriggerDomainReload)
        {
            lock (SyncRoot)
            {
                var descriptor = new PieUnityCapabilityDescriptor();
                descriptor.kind = kind;
                descriptor.name = name;
                descriptor.ns = ns;
                descriptor.description = description;
                descriptor.mode = capabilityMode;
                descriptor.availableIn = capabilityMode;
                descriptor.capabilityKind = capabilityKind ?? "host";
                descriptor.source = "unity_builtin";
                descriptor.owner = capabilityOwner ?? "";
                descriptor.writeScope = writeScope ?? "";
                descriptor.returns = returnValue ?? "";
                descriptor.recommendedWorkflow = recommendedWorkflow ?? "";
                descriptor.examples = examples ?? new string[0];
                descriptor.errorCodes = errorCodes ?? new string[0];
                descriptor.readOnly = readOnly;
                descriptor.deprecated = deprecated;
                descriptor.convenience = convenience;
                descriptor.requiresMainThread = requiresMainThread;
                descriptor.destructive = destructive;
                descriptor.editorOnly = string.Equals(capabilityMode, "editor", StringComparison.OrdinalIgnoreCase);
                descriptor.runtimeOnly = string.Equals(capabilityMode, "runtime", StringComparison.OrdinalIgnoreCase);
                descriptor.canTriggerDomainReload = canTriggerDomainReload;
                descriptor.aliases = aliases ?? new string[0];
                descriptor.parameters = parameters ?? new PieUnityParameterDescriptor[0];

                RegisterRegistrationLocked(source, descriptor, handler, kind, descriptor.aliases, null);
            }
        }

        private static void RegisterRegistrationLocked(
            Dictionary<string, CapabilityRegistration> source,
            PieUnityCapabilityDescriptor descriptor,
            Func<string, string> handler,
            string kind,
            string[] aliases,
            RuntimeHostRegistration runtimeHost)
        {
            var registration = new CapabilityRegistration
            {
                Descriptor = descriptor,
                Handler = handler,
            };

            source[descriptor.name] = registration;
            if (runtimeHost != null)
            {
                runtimeHost.CapabilityNames.Add(descriptor.name);
                TrackRuntimeHostKey(runtimeHost, descriptor.kind, descriptor.name);
            }

            if (aliases == null)
                return;

            for (var i = 0; i < aliases.Length; i++)
            {
                var alias = aliases[i];
                if (string.IsNullOrWhiteSpace(alias))
                    continue;
                source[alias] = registration;
                if (runtimeHost != null)
                    TrackRuntimeHostKey(runtimeHost, descriptor.kind, alias);
            }
        }

        private static string ValidateRuntimeHostDescriptors(string hostNamespace, PieUnityCapabilityDescriptor[] descriptors)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < descriptors.Length; i++)
            {
                var descriptor = descriptors[i];
                if (descriptor == null)
                    return $"Runtime host {hostNamespace} has a null capability descriptor.";

                    string kind = descriptor.kind ?? string.Empty;
                    kind = kind.Trim().ToLowerInvariant();
                if (kind != "tool" && kind != "rpc")
                    return $"Runtime host {hostNamespace} capability {descriptor.name ?? "(unnamed)"} must use kind tool or rpc.";

                    string name = descriptor.name ?? string.Empty;
                    name = name.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return $"Runtime host {hostNamespace} has a capability with an empty name.";

                    string ns = descriptor.ns ?? string.Empty;
                    ns = ns.Trim();
                if (string.IsNullOrWhiteSpace(ns))
                    return $"Runtime host {hostNamespace} capability {name} must declare a namespace.";

                if (!string.Equals(ns, hostNamespace, StringComparison.OrdinalIgnoreCase))
                    return $"Runtime host {hostNamespace} capability {name} must stay in namespace {hostNamespace}.";

                var scopedKey = $"{kind}:{name}";
                if (!seen.Add(scopedKey))
                    return $"Runtime host {hostNamespace} registered duplicate capability {name}.";

                var conflict = FindConflictingRegistration(kind == "rpc" ? RpcMethods : ToolMethods, name, hostNamespace);
                if (!string.IsNullOrWhiteSpace(conflict))
                    return conflict;

                var aliases = descriptor.aliases ?? new string[0];
                for (var aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
                {
                        string alias = aliases[aliasIndex] ?? string.Empty;
                        alias = alias.Trim();
                    if (string.IsNullOrWhiteSpace(alias))
                        continue;

                    var aliasKey = $"{kind}:{alias}";
                    if (!seen.Add(aliasKey))
                        return $"Runtime host {hostNamespace} registered duplicate alias {alias}.";

                    conflict = FindConflictingRegistration(kind == "rpc" ? RpcMethods : ToolMethods, alias, hostNamespace);
                    if (!string.IsNullOrWhiteSpace(conflict))
                        return conflict;
                }
            }

            return "";
        }

        private static string FindConflictingRegistration(Dictionary<string, CapabilityRegistration> source, string key, string hostNamespace)
        {
            if (!source.TryGetValue(key ?? "", out var existing))
                return "";

            if (existing?.Descriptor == null)
                return "";

            if (string.Equals(existing.Descriptor.source, "runtime_host", StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.Descriptor.hostNamespace, hostNamespace, StringComparison.OrdinalIgnoreCase))
                return "";

            var owner = string.Equals(existing.Descriptor.source, "runtime_host", StringComparison.OrdinalIgnoreCase)
                ? $"runtime host {existing.Descriptor.hostNamespace}"
                : "built-in pie-unity capability";
            return $"Runtime host {hostNamespace} cannot register {key} because it conflicts with {owner}.";
        }

        private static void RemoveRuntimeHostLocked(string hostNamespace)
        {
            if (string.IsNullOrWhiteSpace(hostNamespace))
                return;

            if (!RuntimeHosts.TryGetValue(hostNamespace, out var registration))
                return;

            foreach (var key in registration.ToolKeys)
                ToolMethods.Remove(key);
            foreach (var key in registration.RpcKeys)
                RpcMethods.Remove(key);

            RuntimeHosts.Remove(hostNamespace);
        }

        private static void TrackRuntimeHostKey(RuntimeHostRegistration registration, string kind, string key)
        {
            if (registration == null || string.IsNullOrWhiteSpace(key))
                return;

            if (string.Equals(kind, "rpc", StringComparison.OrdinalIgnoreCase))
                registration.RpcKeys.Add(key);
            else
                registration.ToolKeys.Add(key);
        }

        private static PieUnityCapabilityDescriptor CloneDescriptor(PieUnityCapabilityDescriptor source)
        {
            return new PieUnityCapabilityDescriptor
            {
                schemaVersion = string.IsNullOrWhiteSpace(source.schemaVersion) ? PieUnityCapabilitiesConstants.ManifestSchemaVersion : source.schemaVersion,
                kind = source.kind ?? "",
                name = source.name ?? "",
                ns = source.ns ?? "",
                description = source.description ?? "",
                mode = source.mode ?? "runtime",
                availableIn = source.availableIn ?? source.mode ?? "runtime",
                capabilityKind = string.IsNullOrWhiteSpace(source.capabilityKind) ? "host" : source.capabilityKind,
                owner = source.owner ?? "",
                writeScope = source.writeScope ?? "",
                returns = source.returns ?? "",
                recommendedWorkflow = source.recommendedWorkflow ?? "",
                examples = source.examples ?? new string[0],
                errorCodes = source.errorCodes ?? new string[0],
                readOnly = source.readOnly,
                deprecated = source.deprecated,
                convenience = source.convenience,
                requiresMainThread = source.requiresMainThread,
                destructive = source.destructive,
                editorOnly = source.editorOnly,
                runtimeOnly = source.runtimeOnly,
                canTriggerDomainReload = source.canTriggerDomainReload,
                aliases = source.aliases ?? new string[0],
                parameters = source.parameters ?? new PieUnityParameterDescriptor[0],
            };
        }

        private static string NormalizeHostNamespace(string hostNamespace)
        {
            return (hostNamespace ?? "").Trim();
        }

        private static void AppendRuntimeHostDiagnostic(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            RuntimeHostDiagnostics.Add(message);
            if (RuntimeHostDiagnostics.Count > 32)
                RuntimeHostDiagnostics.RemoveRange(0, RuntimeHostDiagnostics.Count - 32);
            PieDiagnostics.Warning($"[PieUnityCapabilityRegistry] {message}");
        }

        private static string DeriveProjectName(string projectPath)
        {
            var normalized = (projectPath ?? "").Replace("\\", "/").TrimEnd('/');
            var index = normalized.LastIndexOf('/');
            if (index >= 0 && index < normalized.Length - 1)
                return normalized.Substring(index + 1);
            return string.IsNullOrWhiteSpace(normalized) ? "" : normalized;
        }
    }
}

#endif
