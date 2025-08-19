using System;
using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using Tomlet;
using SpaceCraft;
using Tomlet.Models;
using System.IO;
using System.Reflection;
using Tomlet.Attributes;
using BepInEx.Logging;
using MonoMod.Cil;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace CraftingEnabler
{
    class SpaceCraftGroups
    {
        public bool isItem;
        public GroupDataItem item;
        public GroupDataConstructible construct;
        public List<GroupDataItem> recipe;
        public DataConfig.CraftableIn craftable_at;
    }

    class ConfigFileEntry
    {
        public ConfigFileEntry()
        {
            recipe = [];
        }

        public override string ToString()
        {
            return $"name = {name}, craftedAt: {craftedAt.ToString()}, recipe: [ {string.Join(", ", recipe)} ]";
        }

        public string name { get; set; }

        [TomlProperty("crafted_at")]
        public DataConfig.CraftableIn craftedAt { get; set; }

        public List<string> recipe;

        [NonSerialized]
        public bool created = false;
    }

    class ConfigFile
    {
        public ConfigFile() {
            items = [];
         }
        public List<ConfigFileEntry> items;
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static ConfigEntry<bool> pluginEnabled;
        static bool isEnabled = true;
        static ConfigFile config;
        static ManualLogSource logger;

        private static SpaceCraftGroups GetGroupsForEntry(ConfigFileEntry entry, List<GroupData> groupsData)
        {
            bool isItem = false;
            var recipe_groups = new List<GroupDataItem> { };

            // can be either GroupDataItem or GroupDataConstructable (other?)
            GroupDataItem item_group = null;
            GroupDataConstructible item_construct = null;
            try
            {
                item_group = (GroupDataItem)groupsData.Find((GroupData data) => data.id == entry.name);
                isItem = true;
            } catch (InvalidCastException)
            {
                logger.LogWarning($"{entry.name} is not an item, checking for a constructible");
                try
                {
                    item_construct = (GroupDataConstructible)groupsData.Find((GroupData data) => data.id == entry.name);
                } catch (InvalidCastException e)
                {
                    logger.LogFatal($"{entry.name} is not a constructible either - PLEAST FILE A BUG!: {e}");
                    return null;
                }
            }
            if (isItem && item_group == null) {
                logger.LogError($"Cannot find GroupDataItem for target item '{entry.name}'");
                return null;
            }
            foreach (var item in entry.recipe)
            {
                // I really hope that folks are only specifying GroupDataItems for recipe ingredients
                // Please file a bug if you want to use something exotic
                GroupDataItem dataItem = null;
                try
                {
                    dataItem = (GroupDataItem)groupsData.Find((GroupData data) => data.id == item);
                } catch (InvalidCastException) {
                    logger.LogError($"Ingredient '{item}' is not a GroupDataItem! Please file a bug if this is intentional.");
                    return null;
                }
                if (dataItem == null)
                {
                    logger.LogError($"Error cannot find recipe ingredient '{item}' while processing '{entry.name}'");
                    return null;
                }
                recipe_groups.Add(dataItem);
            }
            //logger.LogError($"Found groups for target item '{entry.name}'");
            if (isItem)
            {
                return new SpaceCraftGroups { isItem = isItem, item = item_group, construct = null, craftable_at = entry.craftedAt, recipe = recipe_groups };
            } else {
                return new SpaceCraftGroups { isItem = isItem, item = null, construct = item_construct, craftable_at = DataConfig.CraftableIn.Null, recipe = recipe_groups };
            }
        }

        private bool LoadConfigFile()
        {
            String configFilePath;

            logger = Logger;

            config = new ConfigFile();

            try
            {
                Assembly me = Assembly.GetExecutingAssembly();
                string my_dir = Path.GetDirectoryName(me.Location);
                configFilePath = Path.Combine(my_dir, "items.toml");
            }
            catch (Exception e)
            {
                logger.LogError($"Could not construct local config file name: {e}");
                return false;
            }
            logger.LogInfo($"Loading configuration file {configFilePath}");
            try
            {
                // read our config file from the plugin directory
                if (!File.Exists(configFilePath))
                {
                    logger.LogWarning($"Creating empty configuration file");
                    var file = File.CreateText(configFilePath);
                    try
                    {
                        var toml = TomletMain.TomlStringFrom(config);
                        //logger.LogInfo($"Creating file with: {toml}");
                        file.Write(toml);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Unable to create default config file: {ex}");
                        return false;
                    }
                    file.Close();
                }

                try
                {
                    TomlDocument toml = TomlParser.ParseFile(configFilePath);
                    config = TomletMain.To<ConfigFile>(toml);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error parsing config file: {ex}");
                    isEnabled = false;
                    logger.LogInfo($"Plugin is loaded with no config, disabling.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not read config items: {e}");
                return false;
            }
            return true;
        }

        protected void Awake()
        {
            logger = Logger;

            // Plugin startup logic           
            pluginEnabled = Config.Bind("General", "Enabled", true, "Is the mod enabled?");

            if (!pluginEnabled.Value)
            {
                Logger.LogFatal("Bailing out, our plugin is disabled");
                return;
            }
            if (!LoadConfigFile()) { 
                isEnabled = false;
                Logger.LogInfo($"Plugin is loaded with no config, disabling.");
                return;
            }

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Logger.LogWarning($"Possible values for crafted_at: [ {string.Join(", ", Enum.GetNames(typeof(DataConfig.CraftableIn)))} ]");
            if (config.items.Count == 0)
            {
                isEnabled = false;
                Logger.LogWarning("Disabling plugin: no recipes to setup.");
                return;
            }
            Logger.LogInfo($"Parsed {config.items.Count} items");
            config.items.ForEach(delegate (ConfigFileEntry entry)
            {
                Logger.LogInfo($"\t{entry}");
            });
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        // Add our item to the game
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
        private static void StaticDataHandler_LoadStaticData(List<GroupData> ___groupsData)
        {
            //logger.LogInfo("In StaticDataHandler_LoadStaticData");
            if (!pluginEnabled.Value || !isEnabled) {
                logger.LogWarning("Bailing out of StaticDataHandler_LoadStaticData as we're disabled");
                return;
            }

            foreach (var entry in config.items)
            {
                SpaceCraftGroups groups;
                try
                {
                    groups = GetGroupsForEntry(entry, ___groupsData);
                    if (groups == null)
                    {
                        logger.LogError($"Skipping corrupted recipe for {entry.name}");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError($"Caught exception while finding groups for {entry.name}: {e}");
                    continue;
                }
                logger.LogInfo($"Constructing recipe for {entry.name}");
                // don't need to remove and re-add again later
                //___groupsData.Remove(groups.item);

                // setup the item and its recipe
                // TODO: allow the user to specify how this gets unlocked
                if (groups.isItem)
                {
                    if (groups.craftable_at == DataConfig.CraftableIn.Null)
                    {
                        logger.LogInfo($"crafted_at missing, defaulting to craft {entry.name} at CraftStationT2");
                        groups.craftable_at = DataConfig.CraftableIn.CraftStationT2;
                    }
                    groups.item.craftableInList.Clear();
                    groups.item.craftableInList.Add(groups.craftable_at);
                    groups.item.recipeIngredients.Clear();
                    foreach (var recipe_item in groups.recipe)
                    {
                        groups.item.recipeIngredients.Add(Instantiate(recipe_item));
                    }
                    logger.LogInfo($"{entry.name} can now be made at {groups.craftable_at} with {string.Join(", ", entry.recipe)}");
                } else
                {
                    if (entry.craftedAt != DataConfig.CraftableIn.Null)
                    {
                        logger.LogWarning($"Ignoring crafted_at '{entry.craftedAt}' for non-item");
                    }
                    // Do we need to unhide this? Seemed right?
                    if (groups.construct.hideInCrafter)
                    {
                        logger.LogInfo("Unhiding item at crafter");
                        groups.construct.hideInCrafter = false;
                    }
                    // TODO: allow user to specify the category
                    if (groups.construct.groupCategory == DataConfig.GroupCategory.Null)
                    {
                        groups.construct.groupCategory = DataConfig.GroupCategory.Misc;
                    }
                    groups.construct.recipeIngredients.Clear();
                    foreach (var recipe_item in groups.recipe)
                    {
                        groups.construct.recipeIngredients.Add(Instantiate(recipe_item));
                    }
                    logger.LogInfo($"{entry.name} can now be found built from the Misc category with {string.Join(", ", entry.recipe)}");
                }
                // don't need to add, as it's already there
                //___groupsData.Add(groups.item);
                
                entry.created = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnlockedGroupsHandler), "SetUnlockedGroups")]
        private static void UnlockedGroupsHandler_SetUnlockedGroups(NetworkList<int> ____unlockedGroups)
        {
            //logger.LogInfo("In UnlockedGroupsHandler_SetUnlockedGroups");
            if (!pluginEnabled.Value || !isEnabled)
            {
                logger.LogWarning("Bailing out of UnlockedGroupsHandler_SetUnlockedGroups as we're disabled");
                return;
            }

            foreach (var entry in config.items)
            {
                if (entry.created)
                {
                    // TODO: allow the user to specify how this gets unlocked up in StaticDataHandler_LoadStaticData
                    ____unlockedGroups.Add(entry.name.GetStableHashCode());
                    logger.LogInfo($"Unlocked {entry.name}");
                }
            }
        }
    }
}
