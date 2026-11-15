# Third-party notices

CelesteAndroid is built on the following components. Each one keeps its own license.

| Component | Used for | License | Where |
|---|---|---|---|
| [FNA](https://github.com/FNA-XNA/FNA) | XNA 4 reimplementation the game runs on | Ms-PL | `external/FNA` (submodule) |
| [FNA3D](https://github.com/FNA-XNA/FNA3D) | Graphics (Vulkan via SDL_GPU, OpenGL ES) | zlib | `external/FNA/lib/FNA3D` |
| [FAudio](https://github.com/FNA-XNA/FAudio) | FNA audio backend | zlib | `external/FNA/lib/FAudio` |
| [MojoShader](https://github.com/icculus/mojoshader) | Shader translation (inside FNA3D) | zlib | `external/FNA/lib/FNA3D/MojoShader` |
| [SDL 3](https://github.com/libsdl-org/SDL) | Window, input, Android activity/lifecycle | zlib | `external/SDL` (submodule) |
| [MonoMod](https://github.com/MonoMod/MonoMod) | Patching `Celeste.exe` into `Celeste.dll` | MIT | NuGet |
| [Mono.Cecil](https://github.com/jbevain/cecil) | Assembly reading/writing (through MonoMod) | MIT | NuGet |
| [.NET for Android](https://github.com/dotnet/android) | Runtime and Android bindings | MIT | .NET SDK |
| [Pixelify Sans](https://github.com/eifetx/Pixelify-Sans) | Launcher font | SIL OFL 1.1 | `src/Celeste.Android/Resources/font/pixelify_sans.ttf` |
| **FMOD Studio 1.10.14** | Game audio (the game's sound banks require it) | **Proprietary** (Firelight Technologies) | Not in this repository; bundled in the released APK |

## Pixelify Sans

Copyright 2021 The Pixelify Sans Project Authors (https://github.com/eifetx/Pixelify-Sans). Licensed under the SIL Open Font License, Version 1.1: https://openfontlicense.org. The font file is redistributed unmodified.

## FMOD

**FMOD Studio by Firelight Technologies Pty Ltd.**

FMOD isn't open source. The FMOD runtime libraries (`libfmod.so`, `libfmodstudio.so`, `fmod.jar`) are **not** part of this repository and are **not** covered by its MIT license. To build from source, download the *FMOD Engine* Android package, version **1.10.14**, from [fmod.com](https://www.fmod.com/download) (free account; listed under "Older") and accept Firelight's EULA.

## Celeste

Celeste is © Maddy Makes Games Inc. **None** of its code or assets are included in this repository. The public APK contains only launcher artwork: the Celeste logo and an animated Madeline background (fan use; not in this repository) and a pixel-art strawberry icon (also in `docs/media/icon.png`). Everything else, game code, content and key art, is loaded at runtime from the user's own legally obtained copy.
