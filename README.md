<div align="center">
  <h1>
      WouldYouRather Blueprint World
  </h1>
  <p>
     VRChat world blueprint for making a would-you-rather game.
  </p>
  <p>
     Made by <a href="https://markcreator.net/">Markcreator</a>
  </p>
  
  <br />
</div>

### Requirements
[VRChat SDK3 - Worlds Unity package](https://vrchat.com/download/sdk3-worlds)
[UdonSharp Unity package](https://github.com/MerlinVR/UdonSharp/releases)
[CyanEmu](https://github.com/CyanLaser/CyanEmu/releases/) (Optional, but useful for testing)

Be sure to import all the required packages into your Unity project before importing the blueprint package.

[Download](https://github.com/Markcreator/WouldYouRatherBlueprint/releases/download/v1.0/WouldYouRather.Blueprint.World.v1.0.unitypackage)

### Configuration
The example scene is located at: 'Assets/RatherGame/RatherGame.unity'.

I recommend duplicating the example scene and working inside that.
In the new scene you can then safely build and design your own world.

To configure the game, open the '- Configuration -' tab in the scene hierarchy.
In there will be a Choices object in there that contains all the settings for the game.

### Settings
Default Gamemode: Which game should start by default.
 - 0 = Yes/No game
 - 1 = Would you rather game

Prompt Mode: What kind of things should be shown.
 - 0 = Pictures from the Texture Prompts list
 - 1 = Text from the Text Prompts list

