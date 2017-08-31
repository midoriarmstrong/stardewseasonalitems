using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using SBuilding = StardewValley.Buildings.Building;
using SObj = StardewValley.Object;

namespace SeasonalItems
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private static string[][] SeasonalItems = { Constants.Spring, Constants.Summer, Constants.Fall, Constants.Winter };
        private Dictionary<string, int> cache;
        private string currSeason;
        private string prevSeason;
        private string prevPrevSeason;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            cache = new Dictionary<string, int>();
            TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
            PlayerEvents.InventoryChanged += PlayerEvents_InventoryChanged;
        }

        /*********
        ** Private methods
        *********/

        /// <summary>Checks if a new season has started every day and updates prices if it has.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void PlayerEvents_InventoryChanged(object sender, EventArgsInventoryChanged e)
        {
            // If the save is ready and a new item has been added to the inventory
            if (Context.IsWorldReady && e.Added.Count != 0)
            {
                // For each item that has been added to the inventory, adjust its price if necessary
                foreach (ItemStackChange item in e.Added)
                {
                    UpdatePriceBasedOnSeason(item.Item);
                }
            }
        }

        /// <summary>Checks if a new season has started every day and updates prices if it has.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
        {
            // if save is loaded
            if (Context.IsWorldReady)
            {
                SDate currDate = SDate.Now();
                currSeason = currDate.Season;
                prevSeason = GetPrevSeason(currSeason);
                prevPrevSeason = GetPrevSeason(prevSeason);

                // Check if there is a stone in the fridge
                FarmHouse farmhouse = (FarmHouse)Game1.locations.First();
                Chest fridge = farmhouse.fridge;
                bool hasStone = false;

                // Check if there is a stone in the fridge
                foreach (Item item in fridge.items)
                {
                    if (item.Name == "Stone")
                    {
                        hasStone = true;
                        break;
                    }
                }

                // Update item prices everywhere if new season or there is a stone in the fridge
                if (currDate.Day == 1 || hasStone)
                {
                    // Remove outdated cache
                    cache.Clear();

                    // Update inventory item prices
                    List<Item> items = Game1.player.items;

                    if (items.Count > 0)
                    {
                        foreach (Item item in items)
                        {
                            UpdatePriceBasedOnSeason(item);
                        }
                    }

                    // Update chests in the farmhouse
                    UpdateChestsInLocation(farmhouse);

                    // Update chests outside
                    foreach (GameLocation location in Game1.locations)
                    {
                        UpdateChestsInLocation(location);
                    }

                    // Update chests inside
                    foreach (SBuilding building in Game1.getFarm().buildings)
                    {
                        UpdateChestsInLocation(building.indoors);
                    }
                }
            }
        }

        /// <summary>Gets the season prior to the given season.</summary>
        /// <param name="season">Lowercase string containing a season.</param>
        /// <returns>String containing the previous season.</returns>
        private string GetPrevSeason(string season)
        {
            int index = Array.IndexOf(Constants.SeasonOrder, season) - 1;
            if (index == -1) index = 3;
            return Constants.SeasonOrder[index];
        }

        /// <summary>Updates all items in chests in a location to have season-appropriate pricing.</summary>
        /// <param name="location">A GameLocation</param>
        private void UpdateChestsInLocation(GameLocation location)
        {
            if (location == null) return;

            IEnumerable<Chest> chests = location.Objects.Values.OfType<Chest>();
            if (chests.Count() <= 0) return;

            foreach (Chest chest in chests)
            {
                if (chest.items.Count() <= 0) continue;

                foreach (Item item in chest.items)
                {
                    UpdatePriceBasedOnSeason(item);
                }
            }
        }

        /// <summary>Updates an item's pricing based on the season.</summary>
        /// <param name="item">The item to update.</param>
        private void UpdatePriceBasedOnSeason(Item item)
        {
            if (item == null || (item.GetType() != typeof(SObj) && item.GetType() != typeof(ColoredObject))) return;

            SObj obj = (SObj)item;
            string cacheKey = obj.Name + obj.quality;

            // If item is cached, update based on cached value
            if (cache.ContainsKey(cacheKey))
            {
                obj.Price = cache[cacheKey];
                return;
            }

            List<string> seasons = new List<string>();
            int count = 0;

            // Check to see which seasons the item can be found in
            foreach (string[] items in SeasonalItems)
            {
                if (Array.IndexOf(items, obj.Name) > -1) seasons.Add(Constants.SeasonOrder[count]);
                count++;
            }

            // Revert to original price temporarily
            SObj baseObj = new SObj(item.parentSheetIndex, 1, false, -1, obj.quality);
            obj.Price = baseObj.Price;

            // If it is not a seasonal item or is in season, do not adjust price
            if (seasons.Count == 4 || seasons.Count == 0 || seasons.Contains(currSeason))
            {
                cache[cacheKey] = obj.Price;
                return;
            }

            double price = obj.Price;

            // Increase price by 10% if in season previous season
            if (seasons.Contains(prevSeason))
            {
                price *= 1.1;

                // Increase price by 15% if in season two seasons ago
            }
            else if (seasons.Contains(prevPrevSeason))
            {
                price *= 1.15;

                // Else increase price by 20% (in season three seasons ago)
            }
            else
            {
                price *= 1.2;
            }

            cache[cacheKey] = (int)price;
            obj.Price = (int)price;
        }
    }
}