Vector 1.4.4 Unity project and PC port made using Ghidra and DevXUnity.

The code is a combination of Ghidra decompilation and Vector 2's code, though the game behaviour is almost exactly the same as the original.

The project uses Unity version 6000.6.8f1 with the Universal Render Pipeline.

Game content (levels, objects, models, animations, textures, sounds, localization) is loaded from `Vector_Data/StreamingAssets` (`Assets/StreamingAssets` in the project), so it can be modded without rebuilding the game.

## Features

- Unhardcoded locations
- Unhardcoded video cutscenes at the start or end of a level
- Vector 2 movement system support
- Custom texture support
- Controller support
- Graphics options: resolution, window mode, VSync and anti-aliasing (FXAA, SMAA, MSAA 2x/4x/8x)

## Project layout

| Folder | Contents |
| --- | --- |
| `Assets/Art` | UI sprites (by purpose), sprite atlases, branding images, render textures |
| `Assets/Prefabs/UI` | Menu screens, dialogs and list items |
| `Assets/Resources` | Only what is loaded by path at runtime: fonts, shaders, gameplay prefabs, `SoundsManager`, DOTween settings |
| `Assets/Scenes` | `Preloader` (loads data) and `Main` (menus and gameplay) |
| `Assets/Scripts` | `App`, `Audio`, `Content` (StreamingAssets loading), `Data` (save data and game data), `Gameplay`, `Input`, `Localization`, `Settings`, `UI`, and `Nekki` (the game core and screens, kept in their original namespaces) |
| `Assets/Settings` | URP pipeline/renderer/volume assets and the input actions |
| `Assets/StreamingAssets` | Moddable game content |

The player's progress is saved to `saves/UserData.data` next to the game executable (the persistent data path on mobile).

## Vectorier

Please check out Vectorier editor for making custom levels for this and the original pc version.

## Disclaimer

I do not own ANY of the games code, all credit goes to Nekki for making this incredible game.
This should not be redistributed comercially. I do not own any of the assets so neither I nor anyone else has the right to sell them.
