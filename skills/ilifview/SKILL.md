---
name: ilifview
description: >
  Inspect .NET DLL public API surface using ilifview. Use this skill whenever the user wants to see
  public types, methods, properties, or interfaces of a .NET assembly (DLL). Trigger when the user
  says things like "show me the public API of this DLL", "what types are in this assembly",
  "inspect this DLL", "list the public methods", "what does this library expose", "/dll", "/ilifview",
  "show the interface of this assembly", "what's the API surface", or pastes a path ending in .dll
  and asks about its contents. Also trigger when the user asks "what methods can I call on X",
  "how do I use this library", wants to understand a NuGet package's public interface before writing
  code against it, needs type signatures for code generation, or asks to compare two DLL APIs.
  Even if the user doesn't say "DLL" explicitly — if they reference a .NET library, NuGet package,
  or assembly and want to know what's inside it, this skill applies.
---

# ilifview — .NET DLL Public API Viewer

Analyze .NET managed assemblies and display their public API surface (types, members, signatures,
nullable annotations) without executing the DLL. Uses `System.Reflection.MetadataLoadContext` under
the hood, so it's safe to run on untrusted assemblies.

## Tool location

```
D:/Documents/Repos/ilifview/ilifview/bin/Debug/net10.0/ilifview.exe
```

If the exe doesn't exist, build it first:

```bash
dotnet build D:/Documents/Repos/ilifview/ilifview/ilifview.csproj -c Debug
```

## CLI usage

```
ilifview <DLL path> [--format csharp|json|yaml] [--output <file>]
```

| Flag             | Short | Default  | Purpose                                  |
|------------------|-------|----------|------------------------------------------|
| `--format <fmt>` | `-f`  | `csharp` | Output format: `csharp`, `json`, `yaml`  |
| `--output <path>`| `-o`  | stdout   | Write output to a file instead of stdout |

## Choosing the right format

- **csharp** — Human-readable C#-style declarations. Best for reading APIs, showing the user type
  signatures, and loading into context for code generation. This is the default and almost always
  the right choice.
- **json** — Machine-readable structured data. Use when the user wants to programmatically process
  the API, diff two versions, or pipe into another tool.
- **yaml** — Compact structured overview. A middle ground — structured but more readable than JSON.

## How to use this skill

### Step 1: Find the DLL

The user may provide an explicit path, or you may need to locate it:

| User says                          | Where to look                                                     |
|------------------------------------|-------------------------------------------------------------------|
| Explicit path (`path/to/Foo.dll`)  | Use as-is                                                         |
| Project name (`MyProject`)         | Find the .csproj, read `<TargetFramework>`, build path: `bin/Debug/<tfm>/MyProject.dll` |
| NuGet package (`Newtonsoft.Json`)  | `~/.nuget/packages/<name>/<version>/lib/<tfm>/<name>.dll`         |
| Framework library (`System.Text.Json`) | `C:/Program Files/dotnet/shared/Microsoft.NETCore.App/<version>/` |
| "this project" / "current project" | Find .csproj in working directory, derive output DLL path         |

If the DLL hasn't been built yet, run `dotnet build` first.

### Step 2: Run ilifview

```bash
"D:/Documents/Repos/ilifview/ilifview/bin/Debug/net10.0/ilifview.exe" "<DLL path>" -f csharp
```

Warnings about skipped types go to stderr — these are usually harmless (MLC limitations) and can
be suppressed with `2>/dev/null` when showing output to the user.

For large assemblies (like `System.Text.Json.dll`), the output can be 1000+ lines. In that case,
either save to a file with `-o` or pipe through `grep`/`head` to extract the relevant section.

### Step 3: Present the results

Adapt your presentation based on output size and user intent:

- **Small output (< 100 lines)**: Show the full output in the response.
- **Medium output (100-500 lines)**: Show the full output but highlight the parts the user asked about.
- **Large output (> 500 lines)**: Save to file with `-o`, then summarize key types/namespaces and
  show specific sections the user is interested in. Offer to show more on request.

When the user asks about a specific type or member, filter the output rather than dumping everything:

```bash
"D:/Documents/Repos/ilifview/ilifview/bin/Debug/net10.0/ilifview.exe" "Foo.dll" | grep -A 20 "class TargetType"
```

### Using the API for code generation

When the user wants to write code against a library, run ilifview first to understand the available
types and signatures. This is especially valuable for:

- Libraries without documentation
- Internal/proprietary DLLs
- Verifying which overloads exist before generating code
- Understanding nullable contracts (ilifview preserves `?` annotations)

## Example output (csharp format)

```csharp
// Assembly: MyLibrary
// TargetFramework: .NETCoreApp,Version=v8.0

namespace MyLibrary
{
    public interface IRepository<T>
        where T : class
    {
        T? GetById(int id);
        IReadOnlyList<T> GetAll();
        void Save(T entity);
    }

    [Flags]
    public enum Permission : byte
    {
        None = 0,
        Read = 1,
        Write = 2,
    }

    public delegate void EventHandler<in T>(object sender, T args);
}
```

## Error handling

| Error                        | Meaning                                          |
|------------------------------|--------------------------------------------------|
| `File not found`             | DLL path is wrong — help the user locate it      |
| `Not a valid managed assembly` | It's a native DLL, not .NET — inform the user  |
| Warnings about skipped types | MLC couldn't fully resolve some types — usually OK, output is still useful |
