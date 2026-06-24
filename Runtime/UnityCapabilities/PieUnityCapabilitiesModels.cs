#if PIE_UNITY_SPLIT_SOURCES
using System;
using UnityEngine;

namespace Pie
{
    [Serializable]
    public sealed class PieUnityEnvelope
    {
        public bool ok = true;
        public string service = "pie-unity";
        public string version = PieUnityCapabilitiesConstants.Version;
        public string instanceId = "";
        public string projectPath = "";
        public string mode = "";
        public string kind = "";
        public string name = "";
        public string result = "null";
        public string error = "";
        public string errorCode = "";
        public string serverAvailability = "";
    }

    [Serializable]
    public sealed class PieUnityParameterDescriptor
    {
        public string name;
        public string type;
        public bool required;
    }

    [Serializable]
    public sealed class PieUnityCapabilityDescriptor
    {
        public string schemaVersion = PieUnityCapabilitiesConstants.ManifestSchemaVersion;
        public string kind;
        public string name;
        public string ns;
        public string description;
        public string mode;
        public string availableIn;
        public string capabilityKind;
        public string source = "unity_builtin";
        public string hostNamespace = "";
        public string hostDisplayName = "";
        public string owner = "";
        public string writeScope = "";
        public string returns = "";
        public string recommendedWorkflow = "";
        public string[] examples = new string[0];
        public string[] errorCodes = new string[0];
        public bool readOnly;
        public bool deprecated;
        public bool convenience;
        public bool requiresMainThread;
        public bool destructive;
        public bool editorOnly;
        public bool runtimeOnly;
        public bool canTriggerDomainReload;
        public string[] aliases;
        public PieUnityParameterDescriptor[] parameters;
    }

    [Serializable]
    public sealed class PieUnityManifestSummary
    {
        public string service = "pie-unity";
        public string version = PieUnityCapabilitiesConstants.Version;
        public string manifestSchemaVersion = PieUnityCapabilitiesConstants.ManifestSchemaVersion;
        public string skillProtocolVersion = PieUnityCapabilitiesConstants.SkillProtocolVersion;
        public string instanceId = "";
        public string projectPath = "";
        public string projectName = "";
        public string productName = "";
        public string applicationIdentifier = "";
        public string mode = "";
        public string[] namespaces;
        public int stableTools;
        public int deprecatedAliases;
    }

    [Serializable]
    public sealed class PieUnityManifestDetail
    {
        public string service = "pie-unity";
        public string version = PieUnityCapabilitiesConstants.Version;
        public string manifestSchemaVersion = PieUnityCapabilitiesConstants.ManifestSchemaVersion;
        public string skillProtocolVersion = PieUnityCapabilitiesConstants.SkillProtocolVersion;
        public string instanceId = "";
        public string projectPath = "";
        public string projectName = "";
        public string productName = "";
        public string applicationIdentifier = "";
        public string mode = "";
        public string filterNamespace = "";
        public string filterName = "";
        public PieUnityCapabilityDescriptor[] capabilities;
    }

    [Serializable]
    public sealed class PieUnityHealthPayload
    {
        public string service = "pie-unity";
        public string version = PieUnityCapabilitiesConstants.Version;
        public string manifestSchemaVersion = PieUnityCapabilitiesConstants.ManifestSchemaVersion;
        public string skillProtocolVersion = PieUnityCapabilitiesConstants.SkillProtocolVersion;
        public string instanceId = "";
        public string projectPath = "";
        public string projectName = "";
        public string productName = "";
        public string applicationIdentifier = "";
        public string mode = "";
        public int port;
        public bool running = true;
        public bool ready = true;
        public bool domainReloadPending = false;
        public bool bridgeReady = false;
        public bool scriptHostReady = false;
        public bool hostBridgeReady = false;
        public bool runtimeBridgeReady = false;
        public string bridgeLastError = "";
        public string bridgeDiagnostic = "";
        public string scriptHostDiagnostic = "";
        public string discoverableOwnerMode = "";
        public bool runtimeActive = false;
        public bool editorActive = false;
        public bool editorSuppressedByRuntime = false;
        public bool playModeActive = false;
        public bool isPlayingOrWillChangePlaymode = false;
        public int behaviorTreeActiveCount = 0;
        public int registeredHostCount = 0;
        public string[] registeredHostNamespaces = new string[0];
        public int hostCapabilityCount = 0;
        public string[] hostDiagnostics = new string[0];
        public bool mainThreadResponsive = true;
        public int activeHttpRequests = 0;
        public int activeFileRequests = 0;
        public int activeScriptRuns = 0;
        public string availability = "";
    }

    [Serializable]
    public sealed class PieUnityInstance
    {
        public string instanceId;
        public string projectPath;
        public string projectName;
        public string displayName;
        public string productName;
        public string applicationIdentifier;
        public string mode;
        public int port;
        public string token;
        public int pid;
        public long lastSeenUnix;
        public string version;
        public string packageVersion;
    }

    [Serializable]
    public sealed class PieUnityInstancesPayload
    {
        public string service = "pie-unity";
        public PieUnityInstance[] instances;
    }

    [Serializable]
    public sealed class PieUnityDiscoverableSnapshot
    {
        public PieUnityInstance discoverableOwner;
        public bool runtimeActive = false;
        public bool editorActive = false;
        public bool editorSuppressedByRuntime = false;
    }

    [Serializable]
    public sealed class PieUnityRef
    {
        public string scope = "";
        public string mode = "";
        public string id = "";
        public string path = "";
        public string name = "";
        public string type = "";
        public string scene = "";
        public string parentPath = "";
    }

    [Serializable]
    public sealed class PieUnityQueryPayload
    {
        public string scope = "scene_object";
        public string name = "";
        public string path = "";
        public string type = "";
        public int limit = 20;
    }

    [Serializable]
    public sealed class PieUnityQueryResult
    {
        public string scope = "scene_object";
        public string summary = "";
        public PieUnityRef[] results;
    }

    [Serializable]
    public sealed class PieUnityInspectPayload
    {
        public PieUnityRef target;
    }

    [Serializable]
    public sealed class PieUnityInspectResult
    {
        public string summary = "";
        public PieUnityRef target;
        public bool found;
        public bool activeSelf = true;
        public bool activeInHierarchy = true;
        public int childCount;
        public string[] componentTypes;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    [Serializable]
    public sealed class PieUnityApplyPayload
    {
        public string action = "";
        public PieUnityRef target;
        public string name = "";
        public string primitiveType = "";
        public PieUnityRef parentRef;
        public string parentName = "";
        public bool active = true;
        public string tag = "";
        public int layer = -1;
        public string componentType = "";
        public bool enabled = true;
        public string propertyName = "";
        public bool applyPosition;
        public float x;
        public float y;
        public float z;
        public bool applyRotation;
        public float rotX;
        public float rotY;
        public float rotZ;
        public bool applyScale;
        public float scaleX = 1f;
        public float scaleY = 1f;
        public float scaleZ = 1f;
    }

    [Serializable]
    public sealed class PieUnityApplyResult
    {
        public string action = "";
        public string summary = "";
        public PieUnityRef target;
        public PieUnityRef[] createdRefs;
    }
}

#endif
