using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace FatmanServer;

[Injectable(InjectionType.Singleton)]
public sealed class FatmanTraderRegistration(
    TradersTable tradersTable,
    TemplateTable templateTable,
    ISptLogger<FatmanTraderRegistration> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (!tradersTable.TryGetValue(new MongoId(FatmanData.RagmanId), out var ragman)
            || ragman?.Assort is null)
        {
            logger.Error("[Fatman] Ragman trader/assort not found, Fatman armor will not be sold");
            return Task.CompletedTask;
        }

        var assort = ragman.Assort;
        var added = 0;

        foreach (var (tpl, _, _, _, assortId, _, _, _) in FatmanData.Items)
        {
            if (assortId is null)
            {
                continue;
            }

            if (!templateTable.Items.ContainsKey(new MongoId(tpl)))
            {
                logger.Warning($"[Fatman] item template {tpl} is missing, skipping Ragman offer");
                continue;
            }

            var rootId = new MongoId(assortId);
            if (assort.Items.Any(item => item.Id == rootId))
            {
                continue;
            }

            assort.Items.Add(new Item
            {
                Id = rootId,
                Template = new MongoId(tpl),
                ParentId = "hideout",
                SlotId = "hideout",
                Upd = new Upd
                {
                    UnlimitedCount = true,
                    StackObjectsCount = 6.0,
                    Repairable = new UpdRepairable
                    {
                        Durability = 1200.0,
                        MaxDurability = 1200.0,
                    },
                },
            });

            assort.BarterScheme[rootId] =
            [
                [
                    new BarterScheme
                    {
                        Count = 800000.0,
                        Template = new MongoId(FatmanData.Roubles),
                    },
                ],
            ];
            assort.LoyalLevelItems[rootId] = 4;
            added++;
        }

        logger.Info($"[Fatman] Ragman assort updated ({added} armor variants, 800k roubles, loyalty 4)");
        return Task.CompletedTask;
    }
}
