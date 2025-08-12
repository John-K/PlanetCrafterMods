using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;

namespace MachineOptimizerTweak
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin {
        static ConfigEntry<bool> isEnabled;
        static ConfigEntry<int> machine_count;
        static ConfigEntry<float> machine_range;
        private static ManualLogSource logger;

        private void Awake() {
            logger = Logger;
            isEnabled = Config.Bind("General", "Enabled", true, "Is the mod enabled?");
            machine_count = Config.Bind("General", "machine_count", 8, "Maximum number of machines to boost");
            machine_range = Config.Bind("General", "machine_range", 250f, "Boost machines within this range");

            // Plugin startup logic
            Logger.LogInfo($"v{PluginInfo.PLUGIN_VERSION} by {PluginInfo.PLUGIN_AUTHOR} is loaded!");
            if (isEnabled.Value) {
                Logger.LogInfo($"Configured for {machine_count.Value} machines within a range of {machine_range.Value}");
            } else {
                Logger.LogInfo($"Plugin is disabled by configuration");
            }

            // subscribe to settings changed events so we can poke the game
            Config.SettingChanged += new System.EventHandler<SettingChangedEventArgs>(this.ConfigChanged);

            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        // Populate initial values
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MachineOptimizer), "WaitBeforeInit")]
        static void MachineOptimizer_WaitBeforeInit(MachineOptimizer __instance) {
            if (isEnabled.Value) {
                __instance.maxWorldObjectPerFuse = machine_count.Value;
                __instance.range = machine_range.Value;
            }
        }

        // Poke all of the MachineOptimizers with the new value after 0.25s if no other updates occur in the meantime
        private static IEnumerator UpdateOptimizers(SettingChangedEventArgs e, int count, float range) {
            yield return new WaitForSeconds(0.25f);
            var optimizers = UnityEngine.Object.FindObjectsByType<MachineOptimizer>(FindObjectsSortMode.None);
            logger.LogInfo($"Got config changed notification: '{e.ChangedSetting.Definition.Key}' changed to '{e.ChangedSetting.BoxedValue}', poking {optimizers.Length} MachineOptimizers");
            // tell all MachineOptimizers to update
            foreach (MachineOptimizer m in optimizers) {
                m.maxWorldObjectPerFuse = count;
                m.range = range;
                m.ChangeMultiplierDependingOnFuses(false);
            }
        }

        // Update the game whenever our configuration value changes
        // MachineOptimizers will only re-parse their config when their inventory changes, or something pokes them.
        // This implements some rate-limiting so we don't update them for every keystroke in the Mod Menu
        private void ConfigChanged(object sender, SettingChangedEventArgs e) {
            /*if (!isEnabled.Value) {
                return;
            }*/

            int count = machine_count.Value;
            float range = machine_range.Value;

            if (e.ChangedSetting.Definition.Key == "Enabled") {
                var state = isEnabled.Value ? "enabled" : "disabled, reverting to default values";
                logger.LogInfo($"Plugin is now {state}");
                if (!isEnabled.Value) {
                    // revert to default values
                    count = (int)machine_count.DefaultValue;
                    range = (float)machine_range.DefaultValue;
                }
            }

            // Stop any pending updates
            this.StopAllCoroutines();

            // Schedule a new update to happen in the near future
            this.StartCoroutine(UpdateOptimizers(e, count, range));
        }
    }
}
