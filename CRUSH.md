# CRUSH.md for Unity Train Project

## Build, Lint, Test Commands
- Open project: /Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity -projectPath /Users/paweldubiel/Documents/projects/unity/Train (replace version as needed)
- Build (Editor): Use Unity Editor: File > Build Settings > Build
- Build (CLI): /Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -buildTarget <platform> -logFile build.log
- Run all tests (CLI): /Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -runTests -testPlatform editmode -testResults TestResults.xml -logFile test.log
- Run single test: Use Unity Test Runner in Editor, or CLI with -testFilter <TestName> (requires Unity Test Framework setup)
- Lint: No standard lint; use Roslyn analyzers in Visual Studio or Rider; dotnet format for C# files

## Code Style Guidelines
- Language: C# with UTF-8 encoding, LF line endings
- Indentation: 4 spaces, no tabs
- Naming: PascalCase for classes/methods/properties; _camelCase for private fields; UPPER_SNAKE_CASE for constants
- Fields: Use [SerializeField] private Type _fieldName; for Inspector-exposed fields
- Imports: Organize alphabetically; avoid using directives in code; group Unity, System, then others
- Formatting: Brace on new line; space before/after operators; use IDE formatter (e.g., dotnet format)
- Types: Prefer explicit typing; use var for local variables where type is obvious
- Error Handling: Use try-catch for external APIs; log errors with Debug.LogError; avoid swallowing exceptions
- Unity Specific: Use RequireComponent; minimize runtime instantiation; configure via Inspector
- Comments: Use XML docs for public methods; avoid unnecessary comments
- File Structure: Scripts in Assets/Scripts/ or subfolders; one class per file
- Testing: Use Unity Test Framework; tests in Assets/Tests/; aim for unit tests on logic

## Codebase Structure
- Assets/: Gameplay code, scenes, prefabs
- Assets/Games/Pong/: Example 2D game with scripts like Ball2D.cs, PaddleAI.cs
- Prefabs: In Assets/Resources/Pong/
- Input: Use Input System with InputActionAsset

## Important Unity Guidelines
- NEVER modify .meta files directly - Unity manages these automatically
- Always commit .meta files to version control when assets are added/renamed/moved
- If .meta files become corrupted, delete them and let Unity regenerate them
- Use Unity Editor for all asset operations to ensure proper .meta file synchronization

(Note: No Cursor/Copilot rules found; extend as needed)