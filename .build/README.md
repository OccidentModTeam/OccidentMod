# Windows build source paths

The project overrides tModLoader's external `BuildMod` target on Windows. Before
packaging, `Resolve-ModSource.ps1` selects the absolute Windows path under
`Documents/My Games/Terraria/tModLoader/ModSources`, provided its project file is
the same physical file as the project being built. This handles directory
junctions and slash differences without copying or moving source files.

This keeps VS Code, Visual Studio, and `dotnet build` consistent with the in-game
Mod Sources list. A different checkout with the same name is rejected rather
than silently packaging it. A checkout outside ModSources still builds using
its normalized absolute path when no matching ModSources entry exists.

For a custom tModLoader save/source root, pass
`-p:OccidentModSourcesDirectory="X:\custom\ModSources"` to MSBuild.
`-p:BuildMod=false` still compiles without packaging. Non-Windows builds retain
tModLoader's original target. The `.build` folder is excluded from the mod package
by tModLoader's existing rule for paths beginning with a dot.

Direct `tModLoader.dll -build ...` commands bypass the project target. Prefer the
in-game build button or `dotnet build OccidentMod.csproj`. If a direct command is
needed, pass the path printed by `Resolve-ModSource.ps1` as its `-build` argument.
