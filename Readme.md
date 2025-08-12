# PlanetCrafterMods
BepInEx+Harmony mods for the Unity/Steam game The Planet Crafter

Available from:
 - [Steam](https://store.steampowered.com/app/1284190/The_Planet_Crafter/)
 - [GoG](https://www.gog.com/en/game/the_planet_crafter)

Special thanks to:
 - [Planet Crafter Wiki](https://planet-crafter.fandom.com/wiki/Developing_CSharp_Mods) for their notes on getting started with plugin writing
 - [akarnokd](https://github.com/akarnokd) for his excellent [ThePlanetCrafterMods repo](https://github.com/akarnokd/ThePlanetCrafterMods)

## Version <a href='https://github.com/John-K/PlanetCrafterMods/releases'><img src='https://img.shields.io/github/v/release/John-K/PlanetCrafterMods' alt='Latest GitHub Release Version'/></a>

[![Github All Releases](https://img.shields.io/github/downloads/John-K/PlanetCrafterMods/total.svg)](https://github.com/John-K/PlanetCrafterMods/releases)

:arrow_down_small: Download files from the releases: https://github.com/John-K/PlanetCrafterMods/releases/latest

## Supported Game Version: 1.526 or later

With or without the DLC.

This repo only supports the very latest Steam or GoG releases.

# Mods

### Cheats
 - [Drone Speed](#cheat-drone-speed)

### Features
 - [Crafting Enabler](#feature-crafting-enabler)

## (Cheat) Drone Speed

Sets the drone speed as configured. Default is 21

### Configuration
`JohnHedge.CheatDroneSpeed.cfg`
```
[General]

## Is the mod enabled?
# Setting type: Boolean
# Default value: true
Enabled = true

## Speed of Drones
# Setting type: Single
# Default value: 21
DroneSpeed = 21
```

## (Feature) Crafting Enabler

Allows the player to specify the recipe for objects and in the case of items, where they can be crafted.

Any object that is modified by this plugin will immediately become craftable.

Note: limited testing has been performed, so please report and issues that you discover.

### Configuration

This plugin reads its config from a file named `items.toml` in the same directory as the plugin DLL (typically `JohnHedge - (Feature) Crafting Enabler`). An empty file will be created at runtime if the file does not exist.

Possible values for `crafted_at` are `[Null, CraftStationT1, CraftStationT2, CraftStationT3, CraftRocket, CraftBioLab, CraftGeneticT1, CraftIncubatorT1, CraftDroneT1, CraftOvenT1, CraftVehicleT1, CraftQuartzT1, CraftDeparturePlatform]`

The `name` and `recipe` strings must be a valid object name known to `StaticDataHandler`

Sample `items.toml`:
```
items = [
    { name = "SeedGold", crafted_at = "CraftStationT2", recipe = ["TreeRoot", "Sulfur"] },
    { name = "GoldenContainer",  recipe = ["Iron", "Rod-osmium", "Rod-alloy"]}
 ]
```