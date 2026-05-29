# Solar Expanse Launch Tweaks
This is a  mod for the game Solar Expanse which fixes issues with the base-game Mission Planner.

## Features

**LV Wet Mass Fix**
- By default, the game does not account for the wet or dry mass of the spacecraft being launched when determining whether a LV can launch a certain mission. This allows, for instance, a 10T Sparrow to launch a Stratos loaded with 1KT of fuel into orbit. This mod fixes that issue, including both the spacecraft's fuel and dry mass  in the calculation.

**Self-launch Spacecraft Fixes**
- Spacecraft that can launch from a body without a LV (for example, Stratos) can consume more than their fuel capacity when launching from another planet (for instance, Stratos could consume 1.4kT to go from Mars to Earth, despite only having 1KT of fuel capacity). This mod fixes that issue and clamps the total fuel use to the ship's fuel capacity.

**Detailed Blockers**
- This patch allows you to see exacty what numbers are preventing a launch from occuring. For instance, if there is too much payload loaded, this mod tells you exactly how much the ship has loaded in total, as well as the capacity of the LV. This also applies to thrust, needed supplies, and any other blockers.


# Installation

1. This plugin uses BepInEx 5.4 to inject code into the Solar Expanse exe. Install it here: https://docs.bepinex.dev/articles/user_guide/installation/index.html
2. Once BepInEx is installed, run it once to generate ```Solar Expanse/BepInEx/Plugins```
3. Download the latest release of this mod.
4. Move the ```Launch Tweaks``` folder in the ZIP into the ```BepInEx/Plugins``` folder.
