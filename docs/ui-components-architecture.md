# MobileForge UI Components Architecture

## Overview

The UI component system replaces the monolithic `GameRenderer.cs` with a two-tier architecture:

1. **Primitives** — prefab-based visual components (MFButton, MFGemBoard, MFProgressBar, etc.)
2. **Pages** — code-based compositions that instantiate/arrange primitives into complete screens

## Architecture Diagram

```
IScreen (pure C#)
  TitleScreen, BattleScreen, GachaScreen, etc.
       │ Bind(screen)
       ▼
Pages (PageBase<TScreen> MonoBehaviours)
  TitlePage, BattlePage, GachaPage, etc.
  - OnBind(): creates primitives, wires data + events
  - OnRefresh(): re-reads screen state
       │ contains / spawns
       ▼
Primitives (MonoBehaviours + prefabs)
  MFButton, MFGemBoard, MFProgressBar, MFScrollList, etc.
  - Simple data API (SetLabel, SetProgress, SetElements)
  - Events (OnClick, OnGemSwapped, OnItemSelected)
       │
       ▼
UIComponentRouter (replaces GameRenderer)
  - Subscribes to UIRouter.OnNavigated
  - Looks up registered page for screenId
  - Instantiates page, calls Bind(screen), Mount()
```

## Assembly Structure

```
MobileForge                    (pure C# framework — unchanged)
  └── Presentation/UIComponents/
      ├── IUIComponent.cs           IUIComponent, IUIComponent<TScreen>
      └── UIComponentRegistry.cs    screenId → page factory mapping

MobileForge.UIComponents       (Unity-specific UI library)
  ├── PageBase.cs               Base class with layout helpers
  ├── UIComponentRouter.cs      Replaces GameRenderer routing
  ├── MFPrimitiveLibrary.cs     ScriptableObject: type → prefab map
  ├── Primitives/               MFButton, MFOverlay, MFProgressBar, etc.
  ├── Pages/                    TitlePage, BattlePage, GachaPage, etc.
  ├── Prefabs/                  Unity prefab assets (designer-created)
  └── TestScenes/               Gallery, Preview, Mocks

Assembly-CSharp                (game-specific wiring)
  ├── TosUISetup.cs             Registers ToS screen→page mappings
  ├── GameBootstrap.cs          Creates TosGame, loads JSON data
  └── Editor/SceneSetup.cs      Creates UISystem GameObject hosting TosUISetup
```

## Key Interfaces

### IUIComponent / IUIComponent<TScreen>
```csharp
public interface IUIComponent
{
    void Mount(Transform parent);
    void Unmount();
    void Refresh();
    void BindUntyped(object screen);
}

public interface IUIComponent<TScreen> : IUIComponent where TScreen : class
{
    void Bind(TScreen screen);
    TScreen Screen { get; }
}
```

### PageBase<TScreen>
MonoBehaviour implementing `IUIComponent<TScreen>`. Provides:
- `SpawnPrimitive<T>(parent)` — instantiates a primitive from the library
- `CreateRegion(name, anchor, padding)` — anchor-based layout zones
- `CreateVBox/HBox/Grid(name, parent, spacing)` — UGUI layout containers
- `SafeArea` — auto-insets for device notch/cutout
- `OnLayoutChanged(w, h)` — virtual hook for responsive behavior
- `OnBind(screen)` / `OnRefresh()` — abstract override points

### MFPrimitiveLibrary
ScriptableObject loaded from `Resources/MFPrimitiveLibrary`. Maps primitive types to prefab references. Fallback: creates bare GameObjects if no prefab exists (enables code-first development).

## Primitives

| Primitive | Interface | Events | Bridges |
|-----------|-----------|--------|---------|
| MFButton | SetLabel, SetEnabled, SetColor | OnClick | ButtonFeedback |
| MFOverlay | Show, Hide, SetOpacity | OnTapped | — |
| MFProgressBar | SetProgress, SetColor, SetAnimated | — | UIAnim |
| MFScrollList | Setup, SetItems | OnItemSelected | VirtualList |
| MFScrollGrid | Setup, SetItems | OnItemSelected | GridView |
| MFGemBoard | SetElements, SetColorMap | OnGemSwapped | — |
| MFMonsterCard | SetMonster, SetData | OnTapped | CardView |
| MFCurrencyDisplay | Bind, SetValue, SetImmediate | — | CurrencyBar |

## Pages

| Page | Screen Type | Key Primitives |
|------|-------------|---------------|
| TitlePage | TitleScreen | MFButton (nav entries) |
| LevelSelectPage | DungeonSelectScreen | MFScrollList, MFButton |
| TeamSelectPage | TeamSelectScreen | MFScrollGrid, MFButton, MFMonsterCard |
| BattlePage | BattleScreen | MFGemBoard, MFProgressBar, MFButton |
| ResultPage | ResultScreen | MFButton |
| GachaPage | GachaScreen | MFCurrencyDisplay, MFButton, MFScrollGrid |
| InventoryPage | MonsterBoxScreen | MFScrollGrid |
| ShopPage | ShopScreen | MFButton, MFCurrencyDisplay |
| PlaceholderPage | IScreen | MFButton |

## Responsive Layout

- **CanvasScaler**: reference 540x960, ScaleWithScreenSize, match=0.5
- **MFAnchor regions**: Top, Center, Bottom, Fill partition the screen
- **LayoutGroups**: auto-arrange children, prevent overlap
- **ContentSizeFitter**: containers auto-size to content
- **ScrollRect**: wraps any content that might overflow
- **AspectRatioFitter**: gem board uses FitInParent (6:5 ratio)
- **OnLayoutChanged(w,h)**: pages can override for custom responsive logic

## Testing Tiers

| Tier | Scene/Tool | Tests | Who |
|------|-----------|-------|-----|
| 1. Primitive QA | PrimitiveGallery.unity | Visual inspection | Designer |
| 2. Page QA | PagePreview.unity + mocks | Layout at multiple resolutions | Designer/Dev |
| 3. Structural | NUnit Edit Mode tests | Primitives created, data bound | CI |
| 4. Player E2E | Playwright via WebGL | Full game flow | CI/Dev |

## Developer Workflow

1. Browse PrimitiveGallery to see available components
2. Create page class extending `PageBase<TScreen>`
3. Override `OnBind()` to create/arrange primitives
4. Register in `TosUISetup.cs`
5. Preview in PagePreview scene with mock screen
6. Test E2E with Playwright

## Design Decisions

- **Primitives = prefab-based, Pages = code-based**: visual polish via prefabs, orchestration via code
- **Fallback mode**: primitives work without prefabs (code creates minimal UI)
- **MFPrimitiveLibrary override**: swap prefabs to retheme all pages
- **Screens unchanged**: pure C# IScreen implementations don't know about UI components
- **UIComponentRouter**: replaces GameRenderer (removed from scene), subscribes to OnNavigated event
