# Repository Guidelines

## Project Structure & Module Organization
- Root: Unity project (this folder opened in Unity Hub).
- `Assets/`: gameplay code, scenes, input, and settings. Prefer `Assets/Scripts/` for runtime code, `Assets/Editor/` for editor utilities, `Assets/Scenes/` for scenes.
- `Packages/`: Unity package manifest and lock (commit both).
- `ProjectSettings/`: project-wide settings (commit). `UserSettings/` is developer-local.
- Do not commit `Library/`, `Temp/`, or `Logs/`.

## Architecture Overview
- Approach: idiomatic Unity — compose via Prefabs and components; configure in Inspector; minimal runtime instantiation.
- Prefabs: primary game objects live under `Assets/Resources/Pong/` (e.g., `Paddle.prefab`, `Ball.prefab`).
- 2D Setup: prefer `SpriteRenderer`, `Rigidbody2D`, `BoxCollider2D/CircleCollider2D`, and `PhysicsMaterial2D` for gameplay.
- Input: use the Input System (`Keyboard.current`) or an `InputActionAsset` for bindings; avoid direct `Find` and wire references via `[SerializeField]`.
- Editor Utilities: use `Assets/Editor/` for one-click prefab/build helpers (e.g., `Pong > Build Prefabs (2D)`).

## Build, Test, and Development Commands
- Open locally: use Unity Hub (Add project) or CLI: `/Applications/Unity/Hub/Editor/<version>/Unity -projectPath .`.
- Build (Editor): File → Build Settings → select target → Build.
- Build (CLI example): `/Applications/Unity/Hub/Editor/<version>/Unity -batchmode -quit -projectPath . -executeMethod BuildScript.Build -logFile build.log` (expects `Assets/Editor/BuildScript.cs` with a static `Build()` method).
- Test (CLI): `/Applications/Unity/Hub/Editor/<version>/Unity -batchmode -quit -projectPath . -runTests -testPlatform editmode -testResults TestResults.xml -logFile`.

## Coding Style & Naming Conventions
- C# with 4-space indents, UTF-8, LF endings.
- Classes, methods: PascalCase; private fields: `_camelCase`; constants: `UPPER_SNAKE_CASE`.
- Unity specifics: serialize with `[SerializeField] private Type _field;` and prefer `RequireComponent` where applicable.
- Formatting: use your IDE’s formatter; optional: `dotnet format Assembly-CSharp.csproj`.
- Assets: Prefabs PascalCase, materials suffixed `Mat`, textures suffixed `Tex`.

## Testing Guidelines
- Framework: Unity Test Framework (EditMode/PlayMode).
- Location: `Assets/Tests/EditMode` and `Assets/Tests/PlayMode`.
- Naming: `SomethingTests.cs`, methods start with `Test_...` or `[UnityTest]` for coroutines.
- Coverage: include tests for new logic; for gameplay, prefer PlayMode with deterministic setups.

## Commit & Pull Request Guidelines
- Commits: use Conventional Commits (e.g., `feat: add brake lever logic`).
- Include scene/script paths in body when relevant (e.g., `Assets/Scenes/Main.unity`).
- PRs: clear description, linked issue, before/after screenshots or short GIF for user-visible changes, and test plan (how verified: Editor/PlayMode/target platform).

## Security & Configuration Tips
- In Unity: Version Control = Visible Meta Files; Asset Serialization = Force Text.
- Always commit `.meta` files; avoid moving assets without committing associated `.meta` changes.
