# Simple Save System for the Flax Engine
This is a simple to use save system for the Flax Game Engine that includes save slots, encryption, hashing, and data caching. This is implemented in C# and is not by default exposed to C++.

Enjoy this plugin. If you want to donate, here is a link to donate on [ko-fi](https://ko-fi.com/tryibion).

## Installation
To add this plugin project to your game, follow the instructions in the [Flax Engine documentation](https://docs.flaxengine.com/manual/scripting/plugins/plugin-project.html) for adding a plugin project automatically using git or manually.

## Usage
The goal of this plugin is to be a simple and straight forward to use save system.

The save location is the same as where the Flax Engine stores logs during runtime which is the local folder on each platform. Ex. Windows: C:\Users\[YourUsername]\AppData\Local\[Company Name]\[Game Name]\[Save folder name set in `SimpleSaveSettings`]. There is a button to open this location in the `SimpleSaveSettings` to get to the location quickly.

The majority of the save system is in the `SimpleSave` static class. The save data is broken up between two parts. One is the default cache and the other are save slots.

Default Cache:
There is only one default cache per game. If the game is small, this cache might be the only one used. It is mainly used to store overhead data that could be loaded when the game first loads, such as the names of the save slots, or initial character data. The `DefaultCache` can be added to by calling `AddToDefaultCache` or retrieved by calling `TryGetFromDefaultCache`. Both of these will take a string name that will be the identifier for the data that is saved. Once data is in the default cache, calling `SaveAll` or `SaveDefault` methods will save the default data to disk. To load the data from the disk, calling `LoadAll` or`LoadDefault` will bring the data back into the cache for retrieval.

Save Slots:
This save system can handle save slots. These slots can be used for all sorts of data. They could be used to allow the player to save different gameplays, or could be used to break up large amounts of data such as enemy save data, player save data, or world save data. How these are used is up to the developer and the needs of the game. To add data to a specific slot cache, the `AddToSlotCache` method is used. This will require a slot name, a file name, and a string key to define. To retrieve data from a slot cache, the `TryGetFromSlotCache` method is used. To save slot data to the disk, `SaveSlot` or `SaveAllSlots` or `SaveAll` methods can be used. To load data from disk, `LoadSlot`, `LoadSlotFile`, `LoadAllSlots`, or `LoadAll` methods can be used. For games that want to use the slot system as different gameplays, an `ActiveSlot` can be set. Similar methods exist as the other slot methods for the active slot, but this allows a layer of abstration if a specific slot is needed a lot.

## Important Classes
- `SimpleSave` - this is a static class that contains the functionality of caching, hashing, encryption, save, and loading data.
