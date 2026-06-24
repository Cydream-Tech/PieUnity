namespace Pie
{
    public static class PieBehaviorTreeBootstrap
    {
        public static void RegisterCapabilities(bool isEditor)
        {
            var mode = isEditor ? "editor" : "runtime";
            PieUnityCapabilityRegistry.RegisterTool(
                "behavior_tree_create",
                "behavior_tree",
                "Create a managed NPBehave behavior tree from a controlled JSON spec. Supported node types: selector, sequence, parallel, repeat, condition, wait, action, succeed, fail.",
                mode,
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "treeId", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "id", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "name", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "root", type = "object", required = true },
                    new PieUnityParameterDescriptor { name = "blackboard", type = "array", required = false },
                    new PieUnityParameterDescriptor { name = "start", type = "boolean", required = false },
                    new PieUnityParameterDescriptor { name = "replaceExisting", type = "boolean", required = false },
                },
                PieBehaviorTreeRuntime.CreateJson,
                capabilityKind: "behavior_tree",
                writeScope: "Creates or replaces an in-memory behavior tree instance.");

            PieUnityCapabilityRegistry.RegisterTool(
                "behavior_tree_start",
                "behavior_tree",
                "Start a managed behavior tree by treeId.",
                mode,
                false,
                false,
                null,
                TreeIdParameters(),
                PieBehaviorTreeRuntime.StartJson,
                capabilityKind: "behavior_tree",
                writeScope: "Starts behavior tree execution.");

            PieUnityCapabilityRegistry.RegisterTool(
                "behavior_tree_stop",
                "behavior_tree",
                "Stop a managed behavior tree by treeId.",
                mode,
                false,
                false,
                null,
                TreeIdParameters(),
                PieBehaviorTreeRuntime.StopJson,
                capabilityKind: "behavior_tree",
                writeScope: "Stops behavior tree execution.");

            PieUnityCapabilityRegistry.RegisterTool(
                "behavior_tree_destroy",
                "behavior_tree",
                "Stop and remove a managed behavior tree by treeId.",
                mode,
                false,
                false,
                null,
                TreeIdParameters(),
                PieBehaviorTreeRuntime.DestroyJson,
                capabilityKind: "behavior_tree",
                writeScope: "Destroys an in-memory behavior tree instance.",
                destructive: true);

            PieUnityCapabilityRegistry.RegisterTool(
                "behavior_tree_status",
                "behavior_tree",
                "Read status for one behavior tree by treeId, or all managed behavior trees when treeId is omitted.",
                mode,
                true,
                false,
                null,
                TreeIdParameters(required: false),
                PieBehaviorTreeRuntime.StatusJson,
                capabilityKind: "inspect");

            PieUnityCapabilityRegistry.RegisterTool(
                "behavior_tree_set_blackboard",
                "behavior_tree",
                "Set typed blackboard entries for a managed behavior tree. Entries use explicit type/value fields instead of arbitrary dictionaries.",
                mode,
                false,
                false,
                null,
                new[]
                {
                    new PieUnityParameterDescriptor { name = "treeId", type = "string", required = true },
                    new PieUnityParameterDescriptor { name = "id", type = "string", required = false },
                    new PieUnityParameterDescriptor { name = "entries", type = "array", required = true },
                },
                PieBehaviorTreeRuntime.SetBlackboardJson,
                capabilityKind: "behavior_tree",
                writeScope: "Updates behavior tree blackboard values.");

            PieUnityCapabilityRegistry.RegisterTool(
                "behavior_tree_get_blackboard",
                "behavior_tree",
                "Read typed blackboard entries for a managed behavior tree.",
                mode,
                true,
                false,
                null,
                TreeIdParameters(),
                PieBehaviorTreeRuntime.GetBlackboardJson,
                capabilityKind: "inspect");
        }

        private static PieUnityParameterDescriptor[] TreeIdParameters(bool required = true)
        {
            return new[]
            {
                new PieUnityParameterDescriptor { name = "treeId", type = "string", required = required },
                new PieUnityParameterDescriptor { name = "id", type = "string", required = false },
            };
        }
    }
}
