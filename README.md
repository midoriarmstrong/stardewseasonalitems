# Seasonal Items
Adjusts sell prices of seasonal items in Stardew Valley based on whether they are in season.

## Requirements
[SMAPI](https://github.com/Pathoschild/SMAPI/releases) 1.15 or above.

## Installation 
### Normal Player
Copy the [bin/Seasonal_Items](/Seasonal_Items/bin/Seasonal_Items) folder to your Stardew Valley/Mods folder, then run Stardew Valley with SMAPI. It should run out of the box.

### Developer 
Open the repository in your IDE of choice and build.

## Use
By default, the mod updates the price values of items as they go into your inventory, but it also updates the price values of all items in all chests once a month on the 1st.

Thus if you use a UI mod that shows you item prices, you may notice inconsistencies between inventory items and chest items. 

Placing these items in your inventory will update the prices accordingly, but you can also place a **stone in your fridge** to auto-trigger the price update for all items on the next day, regardless of whether it is the 1st.
