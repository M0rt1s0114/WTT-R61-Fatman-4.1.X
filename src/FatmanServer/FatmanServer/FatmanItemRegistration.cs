using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;

namespace FatmanServer;

[Injectable(InjectionType.Singleton, OnLoadOrder.Preload + 1)]
public sealed class FatmanItemRegistration(
    TemplateTable templateTable,
    CustomItemService customItemService,
    ISptLogger<FatmanItemRegistration> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (!templateTable.Items.ContainsKey(new MongoId(FatmanData.CloneTpl)))
        {
            logger.Error($"[Fatman] clone source {FatmanData.CloneTpl} not found, aborting item registration");
            return Task.CompletedTask;
        }

        var missingLocalizations = FatmanData.Items
            .Where(item => !FatmanData.Localizations.ContainsKey(item.Tpl))
            .Select(item => item.Tpl)
            .ToArray();
        if (missingLocalizations.Length > 0)
        {
            logger.Error($"[Fatman] missing localization data for: {string.Join(", ", missingLocalizations)}");
            return Task.CompletedTask;
        }

        var registered = 0;

        foreach (var (tpl, internalName, bundle, enName, _, discardLimit, sellOnRagfair, _) in FatmanData.Items)
        {
            try
            {
                if (templateTable.Items.ContainsKey(new MongoId(tpl)))
                {
                    logger.Warning($"[Fatman] item {tpl} already exists, skipping");
                    continue;
                }

                var localization = FatmanData.Localizations[tpl];
                var result = customItemService.CreateItemFromClone(new NewItemFromCloneDetails
                {
                    NewId = new MongoId(tpl),
                    ItemTplToClone = new MongoId(FatmanData.CloneTpl),
                    NewItemName = internalName,
                    ParentId = new MongoId(FatmanData.ParentId),
                    HandbookParentId = FatmanData.HandbookParent,
                    HandbookPriceRoubles = 650000.0,
                    FleaPriceRoubles = 858000.0,
                    AddToHandbook = true,
                    AddToFleaPriceDb = true,
                    OverrideProperties = new TemplateItemProperties
                    {
                        Name = internalName,
                        ShortName = internalName,
                        Description = localization.EnglishDescription,
                        ExaminedByDefault = true,
                        Prefab = new Prefab
                        {
                            Path = "FatmanArmor/heavy_assult_vest_" + bundle + ".bundle",
                            Rcid = string.Empty,
                        },
                        Slots = Array.Empty<Slot>(),
                        Grids = Array.Empty<Grid>(),
                        Width = 4,
                        Height = 4,
                        Weight = 23.0,
                        ArmorClass = 6,
                        ArmorColliders = FatmanData.ArmorColliders,
                        RarityPvE = "Superrare",
                        CanSellOnRagfair = sellOnRagfair,
                        Durability = FatmanData.ArmorDurability,
                        MaxDurability = FatmanData.ArmorDurability,
                        ArmorType = "Heavy",
                        ArmorMaterial = ArmorMaterial.UHMWPE,
                        BluntThroughput = 0.198,
                        SpeedPenaltyPercent = FatmanData.SpeedPenaltyPercent,
                        WeaponErgonomicPenalty = FatmanData.WeaponErgonomicPenalty,
                        DiscardLimit = discardLimit,
                    },
                    Locales = new Dictionary<string, LocaleDetails>
                    {
                        ["en"] = new()
                        {
                            Name = enName,
                            ShortName = "Fatman",
                            Description = localization.EnglishDescription,
                        },
                        ["ch"] = new()
                        {
                            Name = localization.ChineseName,
                            ShortName = "Fatman",
                            Description = localization.ChineseDescription,
                        },
                    },
                });

                if (!result.Success)
                {
                    logger.Error($"[Fatman] failed to register item {tpl}: {string.Join("; ", result.Errors ?? [])}");
                    continue;
                }

                registered++;
            }
            catch (Exception ex)
            {
                logger.Error($"[Fatman] failed to register item {tpl}", ex);
            }
        }

        logger.Info($"[Fatman] registered {registered}/{FatmanData.Items.Length} armor items");
        return Task.CompletedTask;
    }
}
