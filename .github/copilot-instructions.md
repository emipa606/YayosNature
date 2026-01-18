# GitHub Copilot Instructions for RimWorld Modding Project

## Mod Overview and Purpose
This mod aims to enhance the dynamic aspects of RimWorld's environment by introducing systems that allow biomes and terrains on the map to change gradually over time. The goal is to simulate natural processes such as erosion, vegetation growth, and climatic shifts, providing a richer and more immersive gameplay experience.

## Key Features and Systems
- **Dynamic Biome Shifts:** The `mapData` class manages the transitional changes between biomes, allowing for gradual transformation of the map's environment.
- **Terrain Management:** Methods for setting and adjusting terrain types dynamically, responding to environmental factors or player intervention.
- **Natural Roof and Scatter Mechanics:** Functions to simulate natural roof formation and scatter resources/objects randomly across the map, influenced by environmental conditions.
- **World Data Integration:** Through the `worldData` class, the mod ties in global environmental changes to specific map instances, ensuring consistency and realism across different game sessions.
- **Debugging Tools:** The mod includes debugging methods to allow developers to test biome and terrain changes instantly.

## Coding Patterns and Conventions
- **Sealed Classes:** The use of `sealed` for the `mapData` class ensures that it cannot be inherited, which can help optimize performance and maintain the integrity of core systems.
- **IExposable Interface:** Both `mapData` and `worldData` implement `IExposable`, allowing them to save/load important data across game sessions.
- **Private Helper Methods:** Helper functions like `changeTile` and `TerrainFrom` streamline complex operations, promoting code reuse and readability.

## XML Integration
While specifics about XML integration are not provided, it is assumed that XML files define the data structures for biomes and terrains used by the C# components. This might include biome definitions, terrain types, and other world parameters crucial for the mod's functionality.

## Harmony Patching
The `HarmonyPatches` class suggests that the mod employs the Harmony library for modifying the game's C# codebase, allowing for the alteration of game behavior without directly modifying RimWorld’s source. This enables:
- **Non-intrusive Changes:** Implementing modifications that don't conflict with other mods or the base game code.
- **Specific Function Overrides:** Targeting specific methods, such as those involved in terrain generation or environmental updates, for substitution with the mod’s custom logic.

## Suggestions for Copilot
- **Generate Helper Methods:** Define additional utility methods in `DataUtility` for common operations, reducing redundancy across classes.
- **Extend Debug Functions:** Expand on `DebugActionsMisc` to include advanced logging options that provide insights into biome and terrain changes during development.
- **Enhance XML Parsing:** Use Copilot to assist in writing robust XML parsing functions if new data structures are added to the mod.
- **Write Tests:** Develop unit tests for key systems using Copilot to ensure mod reliability and facilitate easier debugging and maintenance.
- **Documentation and Comments:** Encourage Copilot to generate detailed comments for complex logic blocks, improving code clarity and future maintainability.
