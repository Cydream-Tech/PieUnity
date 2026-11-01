---
name: game-use
description: Play a Unity game toward a goal with the game-use decision loop - research the game, write a harness (facts, options, status), run it against a decider (Jev, scripted, or replay), and report. Use when the user wants the agent to play a game, QA a system by playing it, or drive a character.
---

# Game Use

One loop, goal as the use: a QA goal ("verify the shop buy-back price") and a
role-play goal ("you are a cautious guard") are the same harness with different
words. You are the researcher; the loop is the player.

## The three deliverables (a harness)

For a goal you research the game and produce exactly three things:

1. **Facts pipeline** — what `observe()` reads each tick. Read game state
   directly (runtime calls, static fields, scene state). Facts the decider
   sees must be numbers/booleans/short strings; prefix private keys with `_`.
   Fact quality is the ceiling: with complete facts a deterministic policy
   wins; with partial facts the semantic decider improvises.
2. **Option space** — the action menu. Each option: a name, a one-sentence
   human description, its facts (where it leads), and a `run` that acts on
   the game (call a method, send input, drive a controller). Illegal options
   are simply not offered this tick.
3. **Status rule** — when the goal is `done`/`failed`, judged on game state
   (a counter, a position, a flag), never on the option history.

Optional: a `plan` of steps for multi-step goals (data, not code) and
`tempo` (decisions/second: 0.5 deliberate .. 8 reflex).

## Workflow

1. Research: read the game's scripts (player controller, state managers,
   input handling); probe the live runtime with reads; note units and frames.
2. Write the harness against the game surface (reads/writes you verified).
3. Run it: `game_use_run` with the harness module source (default export
   or a named `harness` export), a `readPath`, and a decider —
   `policySource` (a scripted policy expression) or `jev: {key}`. The same
   call as a script is `ctx.gameUse.playWithHarness` in `unity_script_run`.
   Pass `resetPath` to start from a clean state and `runInBackground: true`
   for unattended runs. In the `game_use_run` form, observe/run bodies may
   reference `ctx` directly (the tool splices the harness into a driver that
   has it). Start with the scripted policy if your facts already decide the
   game (free, exact); switch to Jev when judgment is needed.
4. Read the report: outcome, decisions, where it stalled, what failed —
   plus detector findings (log errors, frame spikes) observed while
   playing. A QA verdict is the goal result AND the findings: "won, but
   logged 2 errors" is a bug report. A stall usually means missing facts
   or an option whose `run` lies - fix the harness, rerun. Convergence
   in 2-4 iterations is normal.
5. Report to the user: the verdict, the evidence, and the decision trail.

## Deciders

- `jev` — fast semantic choice (~300ms, ~40 tokens). Needs TYPESAFE_API_KEY.
- `scripted` — your policy as code, zero cost, fully deterministic. Use for
  regression and for games whose facts already decide them.
- `replay` — recorded choices replayed in order; the test double.

## Rules

- The harness never talks to a model; it only declares options with facts.
- Status is read from the game, not inferred from what was attempted.
- One decision is cheap; a run is 30-200 decisions. Keep facts small.
- Do not widen the option space to "try everything": offer what is legal now.
- Reset the game before you verify: an already-won game proves nothing.

## The call, concretely

```js
export async function run(ctx) {
  const trail = [];
  const episode = await ctx.gameUse.playWithHarness({
    readPath: "CS.MyGame.Snapshot",   // static C# method returning state JSON
    maxSeconds: 60,                   // hard episode cap
    harness: {
      goal: "Collect all three sparks",
      tempo: 1,                       // decisions/second; 0.5 deliberate .. 8 reflex
      stallSeconds: 12,               // no-progress budget before "stalled"
      async observe(surface) {
        const s = await surface.read("snapshot"); // parsed readPath JSON
        return { x: s.x, z: s.z, moves: s.moves, score: s.score, won: s.won };
      },
      options(obs) {
        // legal moves only (bounds + walls are game-specific); each option
        // carries facts the decider scores
        return [{ name: "move_north", description: "Step north to (0,1)",
          facts: { steps_to_goal: 6 },
          run: async () => {
            const ok = await ctx.runtime.call("CS.MyGame.Step", [0, 1]);
            await ctx.waitFrames(2);           // let the frame land
            return ok === true;                // confirm from the game, not hope
          } } ];
      },
      status(obs) {
        if (obs.won) return { state: "done", note: "all sparks", evidence: { score: obs.score } };
        if (obs.moves > 60) return { state: "failed", note: "wandered" };
        return { state: "running" };
      },
    },
    policy: (request) => {              // your scripted policy; omit for built-in greedy
      let best, bestScore = Infinity;
      for (const [name, o] of Object.entries(request.options))
        if (o.facts.steps_to_goal < bestScore) { bestScore = o.facts.steps_to_goal; best = name; }
      return best;   // must be an offered option name, else the run rejects
    },
    onDecision: (r) => trail.push({ call: r.call, chosen: r.chosen, situation: r.request.situation }),
  });
  return { episode, trail }; // episode: { outcome, note, decisions, evidence }
}
```

Notes on the shape:
- `status` returns `{ state: "running" | "done" | "failed", note?, evidence? }` —
  a plain string does not parse; nonterminal ticks return `running`.
- `policy` (scripted) and `jev` (live model) are mutually exclusive; passing
  neither runs the built-in greedy (scores `collects_spark`,
  `toward_closest_spark`, `steps_to_goal` facts). Passing both throws.
- An option's `run` acts on the game the way you verified while researching:
  close over `ctx.runtime.call("CS.MyGame.Step", [dx, dz])` (or
  `CS.Pie.GameUse.PieInputHands.Press` for key synthesis). The `surface`
  argument is for reads; its `act` is only wired when a host-provided
  harness sets an act path, so don't rely on it in an inline harness.
- Record decisions with `onDecision` — the episode result carries counts and
  evidence, not the trail.
- Timing: `tempo` bounds decisions/second; `stallSeconds` bounds no-progress
  ticks; `maxSeconds` bounds the whole episode. An option's `run` should
  confirm its own effect (e.g. wait a few frames and re-read) before
  returning true — editors drop frames under load.
