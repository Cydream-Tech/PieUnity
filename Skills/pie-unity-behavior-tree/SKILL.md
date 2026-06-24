---
name: pie-unity-behavior-tree
description: Build and run pie-unity behavior-tree action strategies through the local Unity runtime blackboard and behavior_tree manifest tools.
---

# Pie Unity Behavior Tree

Use this skill when the user wants pie-unity to turn runtime game context into an executable action strategy.

Behavior trees are exposed by the Unity host through manifest-discovered tools in the `behavior_tree` namespace. Do not write or compile C# for a behavior strategy unless the user explicitly asks for project code. Prefer controlled JSON behavior-tree specs plus typed blackboard entries.

## Workflow

1. Inspect the local pie-unity host with `pie-unity-rpc` or the in-chat Unity tools.
2. Read `manifest --namespace behavior_tree`; if the namespace is absent, ask the user to use a package version that includes behavior-tree support.
3. Determine the runtime context keys that the game will write into the blackboard.
4. Create a tree with `behavior_tree_create`, optionally passing initial `blackboard` entries and `start: true`.
5. Update runtime context with `behavior_tree_set_blackboard`.
6. Verify with `behavior_tree_status` and `behavior_tree_get_blackboard`.
7. Read logs with `unity_log_read` if a tree fails or an action receiver is missing.

Do not call `behavior_tree_create`, `behavior_tree_start`, `behavior_tree_stop`, `behavior_tree_destroy`, or `behavior_tree_set_blackboard` from inside an action receiver. Action receivers should return `blackboardUpdates` for their own tree, or queue cross-tree work for a later frame/external loop.

Inside PieChat, call these host tools through `unity_tool_call`:

```json
{
  "tool": "behavior_tree_create",
  "data": {
    "treeId": "npc_guard_strategy",
    "name": "NPC Guard Strategy",
    "blackboard": [
      { "key": "enemyVisible", "type": "boolean", "boolValue": false }
    ],
    "root": {
      "type": "selector",
      "children": [
        {
          "type": "condition",
          "key": "enemyVisible",
          "op": "is_equal",
          "value": { "type": "boolean", "boolValue": true },
          "child": {
            "type": "action",
            "action": "game.attack_visible_enemy",
            "argsJson": "{}"
          }
        },
        {
          "type": "action",
          "action": "game.patrol",
          "argsJson": "{}"
        }
      ]
    },
    "start": true
  }
}
```

For blackboard-driven switching, put the observing `condition` directly under the priority `selector` branch. Do not hide that observer inside a `sequence` that can fail before the fallback branch starts; NPBehave stops child observers when the parent composite stops. `stopsOnChange` defaults to `none`; use `stopsOnChange: "immediate_restart"` only on interrupting selector branches.

For long-running runtime control, prefer a `repeat` root with an explicit `intervalSeconds`, and put `minIntervalSeconds` on leaf actions that must not execute every tree restart. The behavior tree should choose tactics at low frequency; game code should perform frame-by-frame movement from the chosen action intent.

When checking Unity state, use `health.playModeActive` for Play Mode. `health.runtimeActive` only means a Pie runtime owner exists and can stay false in editor-driven Play Mode dogfood. For scripts outside `pie-unity-rpc`, handle one `RPC_UNAUTHORIZED` by refreshing the token from the matching `projectPath + port + instanceId` registry entry before retrying once.

## Supported Spec

Supported node types:

- `selector`
- `sequence`
- `parallel`
- `condition`
- `repeat`
- `wait`
- `action`
- `succeed`
- `fail`

Typed blackboard entries use explicit value fields:

```json
{ "key": "enemyVisible", "type": "boolean", "boolValue": true }
{ "key": "health", "type": "number", "numberValue": 0.65 }
{ "key": "ammo", "type": "integer", "intValue": 12 }
{ "key": "targetId", "type": "string", "stringValue": "enemy_01" }
{ "key": "rawState", "type": "json", "jsonValue": "{\"mode\":\"combat\"}" }
```

`action` nodes require a game-side action receiver registered with `PieBehaviorTreeActionRegistry`. Tree-scoped receivers are removed when that tree is destroyed, so register them after creating/replacing a tree or keep the game-side registration refreshed. The built-in test action is `pie.debug.record_action`; it records execution in status/logs and updates `lastDebugAction`. Add `minIntervalSeconds` when repeated tree evaluation should reuse the previous result instead of calling the receiver again.

Action receivers can return `eventType` and `eventJson` for short-lived events such as catches or decisions. `behavior_tree_status` exposes `lastEventType`, `lastEventJson`, and `eventCount`; prefer those fields over scraping Unity logs when validating whether an event occurred during a strategy round.

Use `repeat` to keep a low-frequency strategy alive:

```json
{
  "type": "repeat",
  "intervalSeconds": 0.5,
  "child": {
    "type": "selector",
    "children": [
      { "type": "action", "action": "game.choose_tactic", "minIntervalSeconds": 0.45 }
    ]
  }
}
```

Use `valueKey` when a condition compares two blackboard entries, for example:

```json
{
  "type": "condition",
  "key": "target.distance",
  "op": "<=",
  "valueKey": "catchRadius",
  "stopsOnChange": "immediate_restart",
  "child": { "type": "action", "action": "game.catch_target" }
}
```

If an action receiver is missing, do not guess. Report the missing action name and either ask the user to register it in game code or use `pie.debug.record_action` for a smoke test.
