# GravityMod
What it does: It manipulates gravity inside the game

Game Version: 1.24+

Mod Version: 1.2.0

Dependencies: PolyTech Framework 0.7.5+

To install: Place this .dll in the ...\Poly Bridge 2\BepInEx\plugins folder


# Settings
- Enable/Disable Mod: Enables/Disables the mod

- Gravity Modifier: You can change the normal gravity with this

- Rigidbodies Affected - This controls if the mod will affect rigidbodies
- Bridge Pieces Affected - This controls if the mod will affect bridge pieces

- Gravity Type: Switch between different types of gravity
  - Normal: Normal gravity
  - CenterPoint: Gravity is centered at setting "Center Point"
  - CenterShape: Gravity becomes centered at the middle of a custom shape
  - CenterShapeStaticPins: Gravity becomes centered at the middle of the static pins of a custom shape
  - LineBetweenStaticPins: Gravity becomes a line between the static pins of a custom shape (@BeljihnWahfl made this grav type)

- Center Point: Gravity is centered at this point when "CenterPoint" is the gravity type

- Custom Shape Color: Controls wich shapes have gravity when "CenterShape(StaticPins)" is the gravity type (If blank all shapes have gravity)

- Ignore Own Gravity: Custom shapes will ignore there own gravity if they have gravity

- Gravity Distance Type: Switch between different types of distance based gravity
  - Not Distance Based: No distanced based gravity
  - Number Based Normal: Distanced based gravity where there is normal gravity at setting "Normal Gravity Distance"
  - Mass Based Normal: Distanced based gravity where there is normal gravity at "mass of the custom shape" (only works when "CenterShape(StaticPins)" is the gravity type)

- Normal Gravity Distance: Where gravity is normal if "Number Based Normal" is the gravity distance type

- Gravity Distance Smoothness: Controls how much gravity changes over an distance (Can't be 0)

- Only Nearest Center: Objects will only have the gravity of the nearest center if enabled

- Load Custom Layout: It will load a campaign layout in "...\Poly Bridge 2\Poly Bridge 2_Data\StreamingAssets\Levels\GravityMod" with the same name (It will load the normal level if it doesn't exist)

- Rotate Camera: Rotate the camere based on targets gravity

- Follow Camera: The camera will follow the target

- Camera Move Speed: How fast the camera rotates/moves

- Change Target: Wich button will change the target (It will go trough the vehicles one by one, at the end it will not target anything)




Some other info:
Gravity distance is based on the formula (1/2)^((Distance - Mass/Number) / Smoothness)
Simulations can be a bit random when using center gravity! (Mostly when using distance based gravity and changing the speed)