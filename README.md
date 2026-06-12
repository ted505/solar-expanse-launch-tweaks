# Solar Expanse Launch Tweaks
This is a mod for the game Solar Expanse which fixes issues with the base-game Mission Planner.

## Features

All features can be individually toggled in `config.yaml`.

### Payload & Mass Corrections

**LV Payload Check** (`lvPayloadCheck`)
- The stock game only counts cargo weight when checking if a payload fits on a launch vehicle. This means a 10T Sparrow can launch a Stratos loaded with 1KT of fuel into orbit without complaint. This fix includes the spacecraft's dry mass and fuel in the payload check, so overloaded LVs are correctly flagged.

**LV Dry Mass** (`lvDryMass`)
- Launch vehicles in stock are treated as massless -- only their propellant is consumed. In reality, the rocket's own structure has to be lifted too. This adds configurable structural mass (as a percentage of propellant) to each LV's launch cost. Bigger rockets waste more fuel lifting themselves.

**Supply Mass in Fuel** (`supplyMassInFuel`)
- The stock game ignores supply mass when computing how much transfer fuel a mission needs. Missions carrying heavy supplies burn the same fuel as empty ones. This fix includes supply mass in the Tsiolkovsky calculation, so heavier loads require proportionally more fuel.

### Self-Launch Fixes

**Self-Launch Fuel Check** (`selfLaunchFuelCheck`)
- Spacecraft that self-launch (e.g. Stratos from Mars without an LV) can plan missions where the fuel needed for launch + transfer exceeds their tank capacity. For instance, a Stratos with 1KT tanks could plan a mission needing 1.4KT. This fix blocks the mission if the tanks can't hold enough.

**Self-Launch Cost Accounting** (`selfLaunchCost`)
- The stock game computes self-launch fuel via the rocket equation but then charges a different abstracted formula as the actual cost. Fuel "vanishes" between the two calculations. This fix makes the accounting consistent -- total fuel charged matches the fuel slider.

**Self-Launch Fuel Scaling** (`selfLaunchDv`)
- When self-launching, the stock game computes launch cost using a fixed mass based on the minimum transfer fuel, regardless of how much fuel is actually loaded. Extra fuel rides to orbit for free. This fix replaces it with a proper two-stage rocket equation: the launch cost now scales with the total loaded mass. More fuel on board means a heavier ship means more fuel burned reaching orbit.

### LV Launch Fixes

**LV Orbit Transfer Fix** (`lvOrbitTransfer`)
- When an LV launches a spacecraft to its parent body's own orbit (e.g. Earth to Earth Orbit with an Eagle), the stock game still charges the spacecraft a transfer burn as if it needs to fly somewhere after the LV delivers it. The LV already completed the trip -- there's nothing left to fly. This fix zeroes out the phantom delta-V so the spacecraft keeps all its fuel.

**Orbit Fuel Credit** (`orbitFuelCredit`)
- When an LV launches from a surface body, the spacecraft ends up in orbit. But the stock game draws all mission fuel from surface stockpiles. This fix lets the spacecraft draw fuel from orbital reserves instead, making fuel staging in orbit a viable strategy. Pre-positioning fuel in orbit significantly extends a spacecraft's effective range -- critical for round trips from high-gravity bodies like Earth.

### Atmosphere

**Atmosphere Penalty** (`atmospherePenalty`)
- Kinetic launchers (railguns, spin launchers) should perform worse in atmosphere due to drag, but the stock game gives them full payload capacity regardless. This fix scales their effective payload with atmospheric pressure -- at 1 atm, a railgun delivers only a fraction of its vacuum capacity. Penalties are configurable per LV type.

### Quality of Life

**Detailed Tooltips** (`detailedTooltips`)
- Adds fuel breakdowns to mission planner tooltips, showing flight cost, launch cost, leftover fuel, delta-V, and supply mass.

**Detailed Blockers**
- Shows exactly what numbers are preventing a launch from occurring. If there is too much payload loaded, the mod tells you the total loaded mass and the LV's capacity. This also applies to thrust, needed supplies, and any other blockers.

**SC Dry Mass Tooltip** (`scDryMassTooltip`)
- Shows spacecraft dry mass in spacecraft tooltips.

**Fuel Slider Default** (`fuelSliderDefault`)
- Defaults the fuel slider to the minimum required fuel instead of maximum, so you're not always dragging it down.

## Configuration

All features are enabled by default. Edit `BepInEx/plugins/launchfix/config.yaml` to disable individual features or tune parameters.

The config file also includes:
- **Per-LV atmosphere penalties** for kinetic launchers
- **Per-LV dry mass ratios** as a percentage of propellant mass

## Installation

1. This plugin uses BepInEx 5.4 to inject code into the Solar Expanse exe. Install it here: https://docs.bepinex.dev/articles/user_guide/installation/index.html
2. Once BepInEx is installed, run it once to generate `Solar Expanse/BepInEx/plugins`
3. Download the latest release of this mod.
4. Move the `launchfix` folder from the ZIP into the `BepInEx/plugins` folder.
