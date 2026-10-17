using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace FatmanServer;

[Injectable(InjectionType.Singleton, OnLoadOrder.TraderRegistration + 1)]
public sealed class FatmanKillaEquipService(
    BotTable botTable,
    TemplateTable templateTable,
    ISptLogger<FatmanKillaEquipService> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (!templateTable.Items.ContainsKey(new MongoId(FatmanData.KillaTpl)))
        {
            logger.Error("[Fatman] Killa armor template is missing, Killa variant will not drop");
            return Task.CompletedTask;
        }

        if (!botTable.Types.TryGetValue(FatmanData.BossKilla, out var bossKilla)
            || bossKilla?.BotInventory is null)
        {
            logger.Error("[Fatman] bosskilla bot type not found, Killa variant will not drop");
            return Task.CompletedTask;
        }

        if (bossKilla.BotInventory.Equipment is null
            || !bossKilla.BotInventory.Equipment.TryGetValue(EquipmentSlots.ArmorVest, out var armorPool)
            || armorPool is null)
        {
            logger.Error("[Fatman] bosskilla ArmorVest pool not found");
            return Task.CompletedTask;
        }

        if (armorPool.TryAdd(new MongoId(FatmanData.KillaTpl), 1.0))
        {
            logger.Info("[Fatman] Killa armor variant added to bosskilla ArmorVest pool");
        }
        else
        {
            logger.Warning("[Fatman] Killa armor variant already present in bosskilla ArmorVest pool");
        }

        return Task.CompletedTask;
    }
}
