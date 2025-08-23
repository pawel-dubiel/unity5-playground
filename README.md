# Unity5 Project

A modular Unity 2D/3D project template optimized for rapid prototyping or small games.

## Features
- Example game: Pong (2D, under Assets/Games/Pong)
- Prefab-driven architecture with reusable components
- Input System configured
- IDIOMATIC Unity (configure via Inspector, minimal runtime instantiation)

## Structure
- `Assets/` Gameplay scripts, prefabs, scenes
- `Assets/Games/Pong/` Example Pong game
- `Assets/Resources/` Shared resources & prefabs
- `Assets/Scenes/` Project scenes
- `Assets/Scripts/` Runtime codebase (add your logic here)
- `Assets/Editor/` Editor scripts
- `Packages/` Unity package manifests (commit both `manifest.json` and `packages-lock.json`)
- `ProjectSettings/` Unity project configuration

## Setup
1. Open in Unity Hub or launch with CLI:
   `/Applications/Unity/Hub/Editor/<version>/Unity -projectPath .`
2. Use Unity Test Runner for tests or run via CLI:
   `/Applications/Unity/Hub/Editor/<version>/Unity -projectPath . -runTests -testPlatform editmode`
3. Build: File → Build Settings

## Contributing
- Use Conventional Commits (e.g., `feat: add brake lever logic`)
- Place scripts under the proper module (`Assets/Games/<Module>/Scripts/`)
- Tests belong in `Assets/Tests/EditMode/` and `Assets/Tests/PlayMode/`

## License
MIT, see LICENSE file.