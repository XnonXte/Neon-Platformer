# AGENTS.md

## Project overview

- This repository is a Unity 6+ 2D platformer project.
- The main gameplay code lives in [Assets/Scripts/PlayerController.cs](Assets/Scripts/PlayerController.cs).
- Unity project metadata and package references are defined in [Assembly-CSharp.csproj](Assembly-CSharp.csproj) and [Packages/manifest.json](Packages/manifest.json).

## Coding conventions

- Prefer editing the existing player controller rather than introducing new gameplay classes unless absolutely necessary.
- Keep gameplay logic in a single, readable script when possible.
- Use Rigidbody2D movement with direct velocity changes; avoid AddForce-based movement.
- Handle input in Update and physics in FixedUpdate.
- Expose tuning values as serialized fields and group them with [Header], [Space], and [Tooltip] attributes.
- Favor clear, deterministic platformer behavior over overly abstract architecture.

## Unity-specific guidance

- The project uses the New Input System; prefer InputActionReference-based bindings when adding or touching input logic.
- Use ground and wall checks with Transform references and LayerMask-based overlap checks.
- Keep collision and debug visualization simple and readable, and draw Gizmos for gameplay checks where helpful.
- Preserve the feel of responsive, forgiving movement: smooth acceleration, clean deceleration, coyote time, jump buffering, and consistent state transitions.

## Change expectations

- Make small, focused edits that fit the current script structure.
- Avoid unnecessary allocations and keep the code easy to tune from the Inspector.
- When changing movement behavior, preserve the existing feel and keep the implementation easy to reason about.

## Validation

- There is no dedicated automated test suite in this repository, so gameplay changes should be validated by playtesting in the Unity Editor.
- If a script change affects compilation, verify it with the Unity editor diagnostics or the project build workflow before finishing.
