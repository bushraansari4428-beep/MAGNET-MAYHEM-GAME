# MAGNET MAYHEM 🧲⚡

Complete Unity 2D mobile game built with procedural neon art, dynamic audio, magnetic physics, and automatic cloud builds via GitHub Actions GameCI.

## Target Platform
- **Target OS:** Android (APK / AAB)
- **Reference Resolution:** 1080 x 1920 Portrait (9:16)
- **Frame Rate Target:** 60 FPS

## Project Architecture
- `Assets/Scripts/LevelDatabase.cs` - Level configs, stages, and obstacle definitions.
- `Assets/Scripts/NeonArtGenerator.cs` - Procedural neon 2D sprites, particle textures, and glowing graphics.
- `Assets/Scripts/ProceduralAudioController.cs` - Synthesized sound effects and ambient soundtrack.
- `Assets/Scripts/GameplayEntities.cs` - Player magnet, polarity switching, magnetic field interactions, collectables, and hazards.
- `Assets/Scripts/MagnetMayhemGame.cs` - Core game loop, UI management, score tracking, progression, and mobile touch input.
- `Assets/Scripts/Editor/MagnetMayhemEditorBuilder.cs` - Editor automation and scene setup tooling.
