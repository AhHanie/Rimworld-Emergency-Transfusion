# Emergency Transfusion

A Rimworld mod that adds a **Stabilize with hemogen** order to the right-click menu of a drafted colonist. When the clicked pawn is suffering from blood loss, the colonist uses one hemogen pack to reduce it by 35% in about one second, far faster than a surgical transfusion or tending.

The pack comes from the colonist's own inventory, or from the closest reachable, unforbidden pack on the map. The order is manual only. It changes the blood loss condition and nothing else: wounds stay open and keep bleeding, and a hemogenic patient's hemogen is not restored.

## Requirements

- Biotech DLC (declared as a mod dependency)

## Building from source

The mod source lives under `1.6/source/` as a .NET SDK project targeting net472:

```
cd "1.6/source"
dotnet build -c Release
```

The compiled DLL is written to `1.6/Assemblies/Emergency Trasnfusion.dll`. A working Rimworld install isn't required to build - `Krafs.Rimworld.Ref` provides stub reference assemblies via NuGet.

## License

GPL-3.0-or-later - see `LICENSE` and `COPYRIGHT`.
