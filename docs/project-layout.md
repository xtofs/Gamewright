# Project layout

A game is written once and run on several platforms. The code is split so that a game knows
nothing about the platform and a platform knows nothing about any game.

```
src/
  Gamewright/             core: GameApp, Canvas, frames, input (no platform code)
  Gamewright.Desktop/     desktop renderer (Avalonia): GameApplication, GameWindow, GameControl
  Gamewright.Browser/     browser renderer: AppHost (C#) and main.ts (canvas)
  Gamewright.HexagonalGrid/  shared hex-grid library used by games
  games/                  one class library per game; each has one GameApp subclass
    Chess/
    Havannah/
  apps/                   one project per game x platform; runs a game on a platform
    Chess.Desktop/   Chess.Browser/
    Havannah.Desktop/  Havannah.Browser/
build/
  Desktop.Host.props      shared by every *.Desktop app
  Browser.Host.props      shared by every *.Browser app
tests/
```

## A game

A library in `src/games/<Game>` with a `GameApp` subclass. The constructor passes the frame size
and the title (`base(width, height, "Chess")`). It references only `Gamewright`.

## An app

An app is a project that hosts one game on one platform. It has no source files of its own:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="../../../build/Desktop.Host.props" />

  <PropertyGroup><GameApp>Chess.ChessApp</GameApp></PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../../games/Chess/Chess.csproj" />
  </ItemGroup>
</Project>
```

The imported props file sets the target framework and the platform reference
(`Gamewright.Desktop` or `Gamewright.Browser`), and defines a target, `GenerateGameEntry`, that runs
before compilation. It writes `obj/<config>/<tfm>/GameEntry.g.cs` containing `Program.Main`, which
starts `new <GameApp>()` on the platform. Browser apps also get the
`SupportedOSPlatform("browser")` assembly attribute.

To see the generated file, build and open `obj/Debug/net11.0/GameEntry.g.cs` in the app's folder.
If a game needs its own start-up code, add a `Program.cs` and remove the `Import` of the props file.

Details that are still per game:
- Browser apps keep their own `wwwroot/index.html` and `Properties/launchSettings.json` (ports).
- Desktop apps link the sprite sheet from the matching Browser app's `wwwroot` as an `AvaloniaResource`.

## Adding a game

1. Create `src/games/<Game>/<Game>.csproj` with a `GameApp` subclass.
2. Copy `src/apps/Chess.Desktop` and `src/apps/Chess.Browser` to `<Game>.Desktop` and `<Game>.Browser`;
   change `GameApp`, the `ProjectReference` and the sprite link; give the browser app its own ports.
3. Add the four projects to `Gamewright.slnx`.

## Running

```
dotnet run --project src/apps/Chess.Desktop
dotnet run --project src/apps/Chess.Desktop -- --snapshot out.png   # headless PNG of the game
dotnet run --project src/apps/Chess.Browser
```
