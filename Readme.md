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