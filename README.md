# Out of Season Bonuses
Adds bonuses to sell prices and friendship points when gifting for items that are out of season.

## Features
* Increases sell prices by 10% the season after item is in season, 15% two seasons after, and 20% three seasons after, as applicable
* Similarly increases friendship bonus by same percentages based on time since growth season 

## Requirements
[SMAPI](https://github.com/Pathoschild/SMAPI/releases) 1.15 or above.

## Installation 
### Normal Player
Copy the [bin/Seasonal_Items](/Seasonal_Items/bin/Seasonal_Items) folder to your Stardew Valley/Mods folder, then run Stardew Valley with SMAPI. It should run out of the box.

### Developer 
Open the repository in your IDE of choice and build.

## Compatibility
Should generally be compatible with any mod that doesn't affect crop prices, excepting a minor bug with the following:

### UI Mods that show item prices
You may notice discrepencies between chest item prices and inventory item prices if you install the mod on an existing game.

To fix this, put a **stone** in your fridge. The next day, all the chest items should be updated. (You can then take the stone out of the fridge.)

You can also just manually grab chest items then put them back to update the values.