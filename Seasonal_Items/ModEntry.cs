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
        private Dictionary<string, int> priceCache;
        private Dictionary<string, List<string>> seasonsCache;
        private Dictionary<string, int[]> giftsGiven;
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
            priceCache = new Dictionary<string, int>();
            seasonsCache = new Dictionary<string, List<string>>();
            giftsGiven = new Dictionary<string, int[]>();

            TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
            PlayerEvents.InventoryChanged += PlayerEvents_InventoryChanged;
            SaveEvents.AfterReturnToTitle += SaveEvents_Clear;
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
            if (Context.IsWorldReady)
            {
                // If there are added items, update their price
                if (e.Added.Count != 0)
                {
                    // For each item that has been added, adjust its price if necessary
                    foreach (ItemStackChange item in e.Added)
                    {
                        UpdatePriceBasedOnSeason(item.Item);
                    }
                }

                // If only one item (or item stack) has been removed, update friendships
                if (e.Removed.Count == 1)
                {
                    Item removedItem = e.Removed.First().Item;
                    List<string> seasons = inSeasonFor(removedItem);
                    double multiplier = GetSeasonalMultiplier((SObj)removedItem, seasons);

                    // If the item was not a seasonal item or is in season, return
                    if (multiplier == 1) return;

                    // Check to see if any NPCs have been given a new gift
                    foreach (KeyValuePair<string, int[]> NPC in Game1.player.friendships)
                    {
                        if (NPC.Key == null || NPC.Value == null) continue;

                        // If NPC is a new friend
                        if (!giftsGiven.ContainsKey(NPC.Key))
                        {
                            giftsGiven[NPC.Key] = new int[] { NPC.Value[0], NPC.Value[1] };
                            if (NPC.Value[0] <= 0) return; // Only add bonus if friendship was gained

                            Game1.player.changeFriendship((int)((NPC.Value[0] * (multiplier - 1))), Game1.getCharacterFromName(NPC.Key));
                            break;
                        }

                        // If NPC has been given a new gift
                        if (giftsGiven[NPC.Key][1] < NPC.Value[1])
                        {
                            int diff = NPC.Value[0] - giftsGiven[NPC.Key][0];
                            giftsGiven[NPC.Key][0] = NPC.Value[0];
                            giftsGiven[NPC.Key][1] = NPC.Value[1];

                            if (diff <= 0) return; // Only add the bonus if gift was not disliked/hated
                            
                            Game1.player.changeFriendship((int)((diff * (multiplier - 1))), Game1.getCharacterFromName(NPC.Key));
                            break; // You can't give gifts to more than one person at a time
                        }
                    }
                }
            }
        }

        /// <summary>Clears all caches on return to title.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveEvents_Clear(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Clears all caches.</summary>
        private void Clear()
        {
            priceCache.Clear();
            seasonsCache.Clear();
        }

        /// <summary>Checks if a new season has started every day and updates prices if it has.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
        {
            // if save is loaded
            if (Context.IsWorldReady)
            {
                giftsGiven.Clear();
                foreach (KeyValuePair<string, int[]> NPC in Game1.player.friendships)
                {
                    if (NPC.Key == null || NPC.Value == null) continue;
                    giftsGiven[NPC.Key] = new int[] { NPC.Value[0], NPC.Value[1] };
                }
                
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
                    Clear();

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

        /// <summary>Gets the multiplier for prices/friendship based on the season.</summary>
        /// <param name="obj">Item to calculate multiplier for.</param>
        /// <param name="seasons">Seasons for item.</param>
        /// <returns>Multiplier value</returns>
        private double GetSeasonalMultiplier(SObj obj, List<string> seasons)
        {
            if (seasons.Count == 0 || seasons.Contains(currSeason)) return 1;
            if (seasons.Contains(prevSeason)) return 1.1;
            if (seasons.Contains(prevPrevSeason)) return 1.15;

            return 1.25;
        }

        /// <summary>Finds the seasons where an Item is in season for.</summary>
        /// <param name="item">Item to check.</param>
        /// <returns>List of in-season seasons.</returns>
        private List<string> inSeasonFor(Item item)
        {
            if (seasonsCache.ContainsKey(item.Name)) return seasonsCache[item.Name];

            List<string> seasons = new List<string>();

            if (IsSObj(item))
            {
                SObj obj = (SObj)item;
                int count = 0;

                // Check to see which seasons the item can be found in
                foreach (string[] items in SeasonalItems)
                {
                    if (items.Contains(obj.Name)) seasons.Add(Constants.SeasonOrder[count]);
                    count++;
                }
            }

            seasonsCache[item.Name] = seasons;
            return seasons;
        }

        /// <summary>Checks whether an Item is a StardewValley.Object that can be casted as a SObj.</summary>
        /// <param name="item">Item to check</param>
        /// <returns>Whether it is not an SObj.</returns>
        private bool IsSObj(Item item)
        {
            if (item == null || (item.GetType() != typeof(SObj) && item.GetType() != typeof(ColoredObject)))
                return false;
            return true;
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
            if (!IsSObj(item)) return; // Return if item is not a StardewValley.Object

            SObj obj = (SObj)item;
            string cacheKey = obj.Name + obj.quality;

            // If item is cached, update based on cached value
            if (priceCache.ContainsKey(cacheKey))
            {
                obj.Price = priceCache[cacheKey];
                return;
            }

            // Revert to original price temporarily
            SObj baseObj = new SObj(item.parentSheetIndex, 1, false, -1, obj.quality);
            obj.Price = baseObj.Price;

            // Calculate new price
            double price = obj.Price * GetSeasonalMultiplier(obj, inSeasonFor(item));
            priceCache[cacheKey] = (int)price;
            obj.Price = (int)price;
        }
    }
}