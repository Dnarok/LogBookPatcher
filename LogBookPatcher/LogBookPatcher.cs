using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.UI.LogBook;
using RoR2;

namespace LogBookPatcher
{
    public static class Extension
    {
        public static bool MatchAny(this Instruction instruction, out Instruction param)
        {
            param = instruction;
            return true;
        }
    }

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LogBookPatcher : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Dnarok";
        public const string PluginName = "LogBookPatcher";
        public const string PluginVersion = "1.0.1";
        public void Awake()
        {
            Log.Init(Logger);

            IL.RoR2.UI.LogBook.LogBookPage.SetEntry += (il) =>
            {
                ILCursor cursor = new(il);
                ILLabel after_if = null;
                Instruction inside_if = null;
                if (!cursor.TryGotoNext
                    (
                        x => x.MatchLdarg(0),
                        x => x.MatchLdfld(typeof(LogBookPage), nameof(LogBookPage.modelPanel))
                    ) ||
                    !cursor.TryGotoNext
                    (
                        x => x.MatchBeq(out after_if),
                        x => x.MatchAny(out inside_if)
                    )
                )
                {
                    Log.Error("Failed to hook LogBookPage.SetEntry, cannot apply fix.");
                    return;
                }

                cursor.GotoPrev(x => x.MatchLdarg(0));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate((LogBookPage self) =>
                {
                    return self.modelPanel.modelPrefabAddress == null;
                });
                cursor.Emit(OpCodes.Brtrue, inside_if);
            };

            // angelic attempt #2
            On.RoR2.UI.LogBook.LogBookController.BuildPickupEntries += (/*love orig*/ orig, availability) =>
            {
                var entryArray = orig(availability);
                foreach (var entry in entryArray)
                {
                    if (entry.extraData is PickupIndex)
                    {
                        var pickup = ((PickupIndex)entry.extraData).pickupDef;
                        if (pickup.equipmentIndex != EquipmentIndex.None)
                        {
                            var equip = EquipmentCatalog.GetEquipmentDef(pickup.equipmentIndex);
                            if (equip)
                            {
                                entry.modelPrefabAddress = equip.pickupModelReference;
                            }
                        }
                    }
                }
                return entryArray;
            };
        }
    }
}