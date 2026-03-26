# ilifview

.NET assembly (.dll) の公開 API を読み取り、型定義・メンバー情報を見やすい形式で出力する CLI ツールです。
`System.Reflection.MetadataLoadContext` を使用しているため、対象アセンブリを実行せずにメタデータだけを安全に読み取ります。

## Features

- 公開型（class / struct / record / enum / interface / delegate）の一覧表示
- フィールド、プロパティ、メソッド、コンストラクタ、イベント、ネスト型の表示
- ジェネリック型パラメータと制約の解析
- Nullable 参照型アノテーションの反映
- カスタム属性の表示（コンパイラ生成属性は除外）
- 3 種類の出力フォーマット: C# 風 / JSON / YAML
- `--type-only` による型宣言のみの出力

## Requirements

- .NET 10 SDK

## Build

```
dotnet build
```

## Usage

```
ilifview <DLL path> [options]
```

### Options

| Option | Short | Description |
|---|---|---|
| `--format <format>` | `-f` | 出力フォーマット: `csharp`(default), `json`, `yaml` |
| `--output <file>` | `-o` | 出力先ファイルパス（省略時は stdout） |
| `--type-only` | | 型宣言のみ表示（メンバーを省略） |

### Examples

```bash
# C# 風に出力（デフォルト）
ilifview MyLibrary.dll

# JSON 形式でファイルに出力
ilifview MyLibrary.dll -f json -o api.json

# 型宣言のみ表示
ilifview MyLibrary.dll --type-only

# YAML 形式
ilifview MyLibrary.dll -f yaml
```

### Sample Output (csharp)

```csharp
// Assembly: MyLibrary
// TargetFramework: .NETCoreApp,Version=v10.0

namespace MyLibrary
{
    public class UserService : IUserService
    {
        public UserService(ILogger logger);

        public string Name { get; set; }

        public virtual User? FindById(int id);
    }
}
```

## Test

```
dotnet test
```

## Project Structure

```
ilifview/
  Program.cs           - Entry point, argument parsing
  AssemblyAnalyzer.cs  - Reflection-based assembly analysis
  AssemblyModel.cs     - Data model (records)
  TypeKindHelper.cs    - Type kind detection (class/struct/record/enum/...)
  IOutputFormatter.cs  - Formatter interface
  CSharpFormatter.cs   - C#-style output
  JsonFormatter.cs     - JSON output
  YamlFormatter.cs     - YAML output
test/                  - xUnit tests
test_assembly/         - Test target assembly
```
