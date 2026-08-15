using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.Reflection.Patching;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Constants;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace ScavEquipmentRemover;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.clankerdude.scav-equipment-remover";
    public override string Name { get; init; } = "Scav Equipment Remover";
    public override string Author { get; init; } = "Clanker Dude";
    public override List<string>? Contributors { get; init; } = null;
    public override Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("~4.0.13");
    public override List<string>? Incompatibilities { get; init; } = null;
    public override Dictionary<string, Range>? ModDependencies { get; init; } = null;
    public override string? Url { get; init; } = null;
    public override bool? IsBundleMod { get; init; } = false;
    public override string License { get; init; } = "MIT";
}

public sealed class ScavEquipmentConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("scavs")]
    public ScavEquipmentToggles Scavs { get; set; } = new();
}

public sealed class ScavEquipmentToggles
{
    public bool Weapons { get; set; } = true;
    public bool Armor { get; set; } = true;
    public bool Helmet { get; set; } = true;
    public bool TacticalVest { get; set; } = true;
    public bool Backpack { get; set; } = true;
    public bool Headset { get; set; } = true;
    public bool Eyewear { get; set; } = true;
    public bool FaceCover { get; set; } = true;
    public bool ArmBand { get; set; } = true;
    public bool Pockets { get; set; } = true;
    public bool Holster { get; set; } = true;
    public bool Scabbard { get; set; } = true;
}

[Injectable]
public sealed class ScavEquipmentRemoverLoader(PatchManager patchManager) : IOnLoad
{
    public Task OnLoad()
    {
        var modDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                           ?? AppContext.BaseDirectory;
        var configPath = System.IO.Path.Combine(modDirectory, "config", "config.json");
        var config = LoadConfig(configPath);

        if (!config.Enabled)
        {
            Console.WriteLine("[ScavEquipmentRemover] Disabled in config.");
            return Task.CompletedTask;
        }

        patchManager.PatcherName = "ScavEquipmentRemover";
        patchManager.AddPatch(new GenerateInventoryPatch(config));
        patchManager.EnablePatches();

        Console.WriteLine("[ScavEquipmentRemover] Loaded for SPT 4.0.13.");
        return Task.CompletedTask;
    }

    private static ScavEquipmentConfig LoadConfig(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"[ScavEquipmentRemover] Config not found: {path}. Using defaults.");
                return new ScavEquipmentConfig();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ScavEquipmentConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) ?? new ScavEquipmentConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScavEquipmentRemover] Failed to read config: {ex.Message}");
            Console.WriteLine("[ScavEquipmentRemover] Falling back to defaults.");
            return new ScavEquipmentConfig();
        }
    }
}

public sealed class GenerateInventoryPatch : AbstractPatch
{
    private static ScavEquipmentConfig _config = new();

    public GenerateInventoryPatch(ScavEquipmentConfig config) : base("ScavEquipmentRemover.GenerateInventory")
    {
        _config = config;
    }

    protected override MethodBase? GetTargetMethod()
    {
        return typeof(BotInventoryGenerator).GetMethod(
            nameof(BotInventoryGenerator.GenerateInventory),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types:
            [
                typeof(MongoId),
                typeof(MongoId),
                typeof(BotType),
                typeof(BotGenerationDetails)
            ],
            modifiers: null
        );
    }

    [PatchPostfix]
    public static void Postfix(BotBaseInventory __result, BotGenerationDetails botGenerationDetails)
    {
        if (!_config.Enabled || __result?.Items is null || botGenerationDetails is null)
        {
            return;
        }

        // Only affect AI Savage/Scav generation, not PMCs or the player's own PlayerScav.
        if (botGenerationDetails.IsPmc || botGenerationDetails.IsPlayerScav)
        {
            return;
        }

        if (!string.Equals(botGenerationDetails.Side, Sides.Savage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var slotsToRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!_config.Scavs.Weapons)
        {
            slotsToRemove.Add("FirstPrimaryWeapon");
            slotsToRemove.Add("SecondPrimaryWeapon");
        }

        if (!_config.Scavs.Armor) slotsToRemove.Add("ArmorVest");
        if (!_config.Scavs.Helmet) slotsToRemove.Add("Headwear");
        if (!_config.Scavs.TacticalVest) slotsToRemove.Add("TacticalVest");
        if (!_config.Scavs.Backpack) slotsToRemove.Add("Backpack");
        if (!_config.Scavs.Headset) slotsToRemove.Add("Earpiece");
        if (!_config.Scavs.Eyewear) slotsToRemove.Add("Eyewear");
        if (!_config.Scavs.FaceCover) slotsToRemove.Add("FaceCover");
        if (!_config.Scavs.ArmBand)
        {
            slotsToRemove.Add("Armband");
            slotsToRemove.Add("ArmBand");
        }
        if (!_config.Scavs.Pockets) slotsToRemove.Add("Pockets");
        if (!_config.Scavs.Holster) slotsToRemove.Add("Holster");
        if (!_config.Scavs.Scabbard) slotsToRemove.Add("Scabbard");

        if (slotsToRemove.Count == 0)
        {
            return;
        }

        var removed = RemoveEquipmentTrees(__result.Items, slotsToRemove);
        if (removed > 0)
        {
            Console.WriteLine($"[ScavEquipmentRemover] Removed {removed} inventory item(s) from generated Scav.");
        }
    }

    private static int RemoveEquipmentTrees(List<Item> items, HashSet<string> slotsToRemove)
    {
        var rootIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (item.SlotId is null || !slotsToRemove.Contains(item.SlotId))
            {
                continue;
            }

            rootIds.Add(item.Id.ToString());
        }

        if (rootIds.Count == 0)
        {
            return 0;
        }

        // Keep expanding until every child/grandchild of a removed root is marked.
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var item in items)
            {
                if (item.ParentId is null)
                {
                    continue;
                }

                if (rootIds.Contains(item.ParentId.ToString()) && rootIds.Add(item.Id.ToString()))
                {
                    changed = true;
                }
            }
        }

        var before = items.Count;
        items.RemoveAll(item => rootIds.Contains(item.Id.ToString()));
        return before - items.Count;
    }
}
