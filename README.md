# Scav Equipment Remover — SPT 4.0.13

Server-side SPT mod for generated AI Scavs.

## What it does

The mod patches SPT 4.0.13's `BotInventoryGenerator.GenerateInventory()` and removes equipment categories from completed Scav inventories according to `config/config.json`.

Weapon attachments are intentionally NOT configurable. If `weapons` is true, SPT keeps the complete generated weapon and its existing attachments. If `weapons` is false, the weapon root and its child attachments are removed together.

It does not modify PMCs or the player's own PlayerScav profile.

## Install

1. Build the project for `net9.0`.
2. Copy the resulting `ScavEquipmentRemover.dll` and the `config` folder into:

   `SPT\user\mods\ScavEquipmentRemover\`

The final folder should look like:

```
SPT\user\mods\ScavEquipmentRemover\
    ScavEquipmentRemover.dll
    config\
        config.json
```

The bundled `lib` DLLs are the four SPT 4.0.13 assemblies supplied for this build/reference project; they are not part of the installed mod.

## Config semantics

`true` = leave that category alone.

`false` = remove that category.

Default config is intentionally set to remove armor/helmet/rig/backpack/headset/eyewear/face cover/armband while leaving weapons and the smaller inventory slots alone.
