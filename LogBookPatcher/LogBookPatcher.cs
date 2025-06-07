using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using RoR2.UI.LogBook;
using UnityEngine;
using System.Linq;
using RoR2;

namespace LogBookPatcher
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LogBookPatcher : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Dnarok";
        public const string PluginName = "LogBookPatcher";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            Log.Init(Logger);

            IL.RoR2.UI.LogBook.LogBookPage.SetEntry += (il) =>
            {
                ILCursor cursor = new(il);
                ILLabel label = null;
                if (!cursor.TryGotoNext
                    (
                        x => x.MatchLdarg(0),
                        x => x.MatchLdfld(typeof(LogBookPage), nameof(LogBookPage.modelPanel))
                    ) ||
                    !cursor.TryGotoNext
                    (
                        x => x.MatchBeq(out label)
                    ) ||
                    !cursor.TryGotoPrev(x => x.MatchLdarg(0))
                )
                {
                    Log.Error("Failed to hook LogBookPage.SetEntry, cannot apply fix.");
                    return;
                }

                // cursed? you haven't seen cursed yet...
                cursor.RemoveRange(8);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate((LogBookPage page, Entry entry) =>
                {
                    return page.modelPanel.modelPrefabAddress == null ||
                           page.modelPanel.modelPrefabAddress.RuntimeKey != entry.modelPrefabAddress.RuntimeKey;
                });
                cursor.Emit(OpCodes.Brfalse, label);
            };

            // the IL of BuildPickupEntries is completely indecipherable.
            // fucking IEnumerable... select... where... orderby... too many functors.
            // so fuck it, we're doing something highly illegal.
            On.RoR2.UI.LogBook.LogBookController.BuildPickupEntries += (/*fuck orig*/ _, availability) =>
            {
                new Entry
                {
                    nameToken = "TOOLTIP_WIP_CONTENT_NAME",
                    color = Color.white,
                    iconTexture = LogBookController.CommonAssets.wipIcon,
                    getStatusImplementation = LogBookController.GetUnimplemented,
                    getTooltipContentImplementation = LogBookController.GetWIPTooltipContent
                };
                IEnumerable<Entry> first = from pickupDef in PickupCatalog.allPickups
                                           select ItemCatalog.GetItemDef(pickupDef.itemIndex) into itemDef
                                           where LogBookController.CanSelectItemEntry(itemDef, availability)
                                           orderby (int)(itemDef.tier + ((itemDef.tier == ItemTier.Lunar) ? 100 : 0))
                                           select new Entry
                                           {
                                               nameToken = itemDef.nameToken,
                                               color = ColorCatalog.GetColor(itemDef.darkColorIndex),
                                               iconTexture = itemDef.pickupIconTexture,
                                               bgTexture = itemDef.bgIconTexture,
                                               extraData = PickupCatalog.FindPickupIndex(itemDef.itemIndex),
                                               modelPrefab = itemDef.pickupModelPrefab,
                                               modelPrefabAddress = itemDef.pickupModelReference,
                                               getStatusImplementation = LogBookController.GetPickupStatus,
                                               getTooltipContentImplementation = LogBookController.GetPickupTooltipContent,
                                               pageBuilderMethod = PageBuilder.SimplePickup,
                                               isWIPImplementation = LogBookController.IsEntryPickupItemWithoutLore
                                           };
                IEnumerable<Entry> second = from pickupDef in PickupCatalog.allPickups
                                            select EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex) into equipmentDef
                                            where LogBookController.CanSelectEquipmentEntry(equipmentDef, availability)
                                            orderby !equipmentDef.isLunar
                                            select new Entry
                                            {
                                                nameToken = equipmentDef.nameToken,
                                                color = ColorCatalog.GetColor(equipmentDef.colorIndex),
                                                iconTexture = equipmentDef.pickupIconTexture,
                                                bgTexture = equipmentDef.bgIconTexture,
                                                extraData = PickupCatalog.FindPickupIndex(equipmentDef.equipmentIndex),
                                                modelPrefab = equipmentDef.pickupModelPrefab,

                                                // THIS IS THE NEW LINE.
                                                // ALL OF THIS FOR A SINGLE LINE.
                                                modelPrefabAddress = equipmentDef.pickupModelReference,
                                                // FUCK ME, GEARBOX.

                                                getStatusImplementation = LogBookController.GetPickupStatus,
                                                getTooltipContentImplementation = LogBookController.GetPickupTooltipContent,
                                                pageBuilderMethod = PageBuilder.SimplePickup,
                                                isWIPImplementation = LogBookController.IsEntryPickupEquipmentWithoutLore
                                            };
                return first.Concat(second).ToArray();

                // I'm gonna throw up.
            };
        }
    }
}