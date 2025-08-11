using BepInEx;
using SpaceCraft;
using HarmonyLib;
using UnityEngine;
using BepInEx.Logging;
using BepInEx.Configuration;

namespace CheatDroneSpeed
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static ConfigEntry<bool> isEnabled;
        static ConfigEntry<float> droneSpeed;
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            isEnabled = Config.Bind("General", "Enabled", true, "Is the mod enabled?");
            droneSpeed = Config.Bind("General", "DroneSpeed", 21f, "Speed of Drones");

            Harmony.CreateAndPatchAll(typeof(Plugin));
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Drone), "MoveToTarget")]
        static void Drone_MoveToTarget(Drone __instance)
        {
            if (!isEnabled.Value)
            {
                return;
            }
            __instance.forwardSpeed = droneSpeed.Value;
            __instance.rotationSpeed = 100f;
        }
    }
}
