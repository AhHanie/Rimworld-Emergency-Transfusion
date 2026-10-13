# Emergency Transfusion

A Rimworld mod that adds a **Stabilize with hemogen** order to the right-click menu of a drafted colonist. When the clicked pawn is suffering from blood loss, the colonist uses one hemogen pack to reduce it by 35% in about one second, far faster than a surgical transfusion or tending.

The pack comes from the colonist's own inventory, or from the closest reachable, unforbidden pack on the map. The order is manual only. It changes the blood loss condition and nothing else: wounds stay open and keep bleeding, and a hemogenic patient's hemogen is not restored.

## Requirements

- Biotech DLC (declared as a mod dependency)

## Building from source

Each supported RimWorld version has its own project under `<version>/source/`. Build the one you need, and the DLL is written to that version's `Assemblies/` folder.

**1.7** (net10.0). Requires an installed RimWorld 1.7, because the project references the game's `Assembly-CSharp.dll` and `UnityStubs.dll`:

```
dotnet build "1.7/source/Emergency Trasnfusion.csproj" -c Release
```

The game folder defaults to `C:\Program Files (x86)\Steam\steamapps\common\RimWorld`. Override it with `-p:RimWorldDir="D:\Games\RimWorld"`. The output is `1.7/Assemblies/Emergency Trasnfusion.dll`.

**1.6** (net472). A RimWorld install isn't required - `Krafs.Rimworld.Ref` provides stub reference assemblies via NuGet:

```
dotnet build "1.6/source/Emergency Trasnfusion.csproj" -c Release
```

The output is `1.6/Assemblies/Emergency Trasnfusion.dll`.

## License

GPL-3.0-or-later - see `LICENSE` and `COPYRIGHT`.
