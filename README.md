<div align="center">
  <img width="806.4" height="453.6" alt="Vector-Thumbnail" src="https://github.com/user-attachments/assets/e3ccaf0a-7cfe-4d2e-b84b-6d273b867c6c" />
</div>

<div align="center">
  <h1>
    Vector Unity
  </h1>
</div>

<div align="center">
  Vector Unity is a project that aims to remove the limitations and fix some of the issues found in the original Vector games. It is mainly intended to be used alongside Vectorier, making it easier for users to modify the game, create their own mods, and add new things to the original experience.
</div>

<div align="center">
  <h3>
    <a href="https://discord.com/invite/pVRuFBVwC2">
      Discord
    </a>
    <span> | </span>
    <a href="#introduction">
      Introduction
    </a>
    <span> | </span>
    <a href="#installation">
      Downloads
    </a>
    <span> | </span>
    <a href="#contributions">
      Contributions
    </a>
  </h3>
</div>

# Introduction
Vector Unity is a community-made build of Vector that aims to remove the limits, bugs and technical issues of the original game. It is designed to be used alongside Vectorier, a level editor that lets players create and modify levels, objects, animations and other game content, making Vector Unity the place where those creations are played. This project aims to modernize the game with new options, controller support and quality of life improvements, while keeping the gameplay true to the original. Game content is loaded from external files, so custom levels, textures, sounds and other mods can be added without rebuilding the game.

The project uses [Unity Version 6000.6.8f1](https://unity.com/releases/editor/whats-new/6000.6.8f1) with the Universal Render Pipeline.

# Features

- Full Custom Modding Support
- Improved Vanilla Gameplay
- Vector 2 Movement System Support
- Controller Support
- Unhardcoded Locations
- Unhardcoded Video Cutscenes at the Start or End of a Level

Vector Unity is used alongside with Vectorier, [the repository can be found here](https://github.com/Vectorier/Vectorier)

# Installation

wip

# Project layout

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

# Contributions
This project was made possible through the collaboration and support of the following contributors. Each person has invested their time, knowledge, and effort to improve, refine, and expand this project. From identifying bugs and implementing new features, their involvement has been invaluable. The project stands as a result of shared effort and collective dedication.
- DoritoTheChips
- Supergoldapfel (sol)
- somemelguy (Mel)
- sonamenil (Vision)
- kubinka0505
- enderdude290 (Ghosted)
- FlipThoseTitle
- FloofIsFalaful (Floof)
- domnulinginer (domnul)
- mirrorsedgegamer
- zayden1325682 (Zayden)

# AI Usage
Vector Unity was built with the Unity engine, with AI tools assisting throughout development, and were used to help understand the original game’s code and behaviour, restructure and clean up the project, and develop new features and fixes. This is still a work in progress, so if you encounter any issues, feel free to open an issue right away!

# Disclaimer
This project is an independent, fan-made custom build of the game Vector, developed and published by Nekki Limited, made specifically to support modding the game. This project is not affiliated with, endorsed by, sponsored by, or approved by Nekki. Vector and all related assets, trademarks, and intellectual property belong to Nekki or their respective owners. Any Nekki assets included in this project are provided solely for modding and compatibility purposes; their inclusion does not grant any license or right to them, and they are not covered by this project’s license. A legitimate copy of Vector is expected. Nekki’s License Agreement restricts copying, distributing, modifying, or creating derivative works from its games without written agreement, and you are solely responsible for complying with it. This project’s own source code and original assets are licensed for non-commercial use only; no profit, including through donations, may be made from them. If you are a rights holder and would like any content removed, please open an issue. This software is provided for educational and personal use only, as is, and is maintained by non-paid contributors in their free time. This project is built with the Unity engine; Unity is a trademark of Unity Technologies, and this project is not affiliated with or endorsed by Unity Technologies. For full license terms, see the LICENSE file.
