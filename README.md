# Continuous Load Ammo
You can now continuously load and unload ammo while outside your inventory!

**Continuous Load Ammo** introduces the ability to load or unload your magazine outside your inventory. Gone are the days of staring at the inventory screen while topping up!

___

### Features
- Freedom: Walk freely while waiting for your magazines to top up or unload
- Realism and balancing: The ammo and magazine should only be in reachable places, this includes your vest, pockets, and secure container. Your walking speed will be limited while loading ammo outside the inventory and pulling your weapon out will cancel loading
- Intuitive controls: You can cancel loading by clicking the `left/right mouse buttons`. You can quickly load your magazine using a hotkey.
- Magazine Presets: You can load magazine presets in-raid using the context menu or by using the quick load hotkey. The loading process also resumes from a partially loaded magazine. Be aware that loading through the inventory screen then closing might cancel the loading process if the ammo or magazine is not in a reachable place.
- Quick loading: You can quickly load ammo from outside the inventory by using the hotkey `K`. By default, this loads your last selected magazine preset in-raid. If none has been selected yet, the best penetrating ammo for your current weapon. While holding this hotkey, `mouse scrollwheel up/down` to choose which ammo to load into a magazine.
- Configurable: Configuration is available in the BepInEx configuration manager

### Reachable Places
- When loading through the inventory (drag and drop or context menu), reachable places include magazines and ammo in vests, pockets, and secure containers, and also in any _**nested containers**_ found inside them
- When using quick load, reachable places include _**only**_ magazines and ammo inside vests, pockets, and secure containers, _**NOT**_ including nested containers

### Installation
Extract the contents of the .zip archive into your SPT directory.
<details>
  <summary>Demonstration</summary>

![Installation](https://i.imgur.com/3N6gTe2.gif)
Thank you [DrakiaXYZ](https://forge.sp-tarkov.com/user/27605/drakiaxyz) for the gif
</details>

### Recommended Mods
- Mods that alter loading speed, to balance at your own preference.
- [UIFixes](https://forge.sp-tarkov.com/mod/1342/ui-fixes) by [Tyfon](https://forge.sp-tarkov.com/user/46005/tyfon). Load or unload multiple magazines!

### Configuration
<details>
  <summary>Configuration</summary>

In the configuration manager (`F12`)

- The speed limit, as a percentage of the walk speed, set to the player while un/loading ammo. Default is `30%`
- Allow loading ammo outside the inventory only when Magazine and Ammo is in your Vest, Pockets, or Secure Container. Default is `true`
- Do not interrupt un/loading ammo when switching inventory tabs (maps tab, tasks tab, etc.) Default is `true`
- Key used to load ammo outside the inventory. Default is `K`
- When using Quick Load, choose ammo that has the highest penetration power, else prioritize the same ammo in the weapon's magazine. Default is `true`
- When using Quick Load, notify the player of the ammo being loaded. Default is `true`
</details>

### Issues
- Touching a barbed wire can conflict with the speed limits set by this mod.

<br></br>
_**Disclaimer:** I will not be held responsible for any injuries that may occur, including but not limited to - tripping over obstacles, bumping into walls, falling into a pit, dying from a scav, etc., whilst loading ammo. Please proceed with caution and watch your step._
