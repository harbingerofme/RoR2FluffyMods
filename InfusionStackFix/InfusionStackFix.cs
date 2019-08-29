﻿using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;

namespace InfusionStackFix
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.FluffyMods.InfusionStackFix", "InfusionStackFix", "1.0.1")]
    public class InfusionStackFix : BaseUnityPlugin
    {
        private static ConfigWrapper<int> InfusionMaximum;
        //private static ConfigWrapper<bool> TurretsGiveEngineerStacks;
        private static ConfigWrapper<bool> TurretsReceiveBonusFromEngineer;

        public void Awake()
        {
            #region ConfigWrappers
            InfusionMaximum = Config.Wrap(
                "Infusion",
                "MaxHpPerStack",
                "Set the maximum health that each infusion gives you (int from 1-500)",
                100
                );

            //TurretsGiveEngineerStacks = Config.Wrap<bool>(
            //    "Engineer",
            //    "Turret",
            //    "Set to true to give Engineer infusion stacks from turret",
            //    false
            //    );

            TurretsReceiveBonusFromEngineer = Config.Wrap<bool>(
                "Engineer",
                "TurretReceivesBonusFromEngineer",
                "If set to true then turrets will receive the current infusion bonus of the Engi on creation",
                true
                );
            #endregion

            IL.RoR2.GlobalEventManager.OnCharacterDeath += GlobalEventManager_OnCharacterDeath;
            On.RoR2.Inventory.AddInfusionBonus += Inventory_AddInfusionBonus;
            On.RoR2.CharacterMaster.AddDeployable += CharacterMaster_AddDeployable;
        }

        private void CharacterMaster_AddDeployable(On.RoR2.CharacterMaster.orig_AddDeployable orig, 
            CharacterMaster self, 
            Deployable deployable, 
            DeployableSlot slot)
        {            
            orig(self, deployable, slot);
            if (TurretsReceiveBonusFromEngineer.Value && slot == DeployableSlot.EngiTurret)
            {
                var ownerMasterBonus = deployable.ownerMaster.inventory.infusionBonus;
                var turretMaster = deployable.GetComponent<CharacterMaster>();
                turretMaster.inventory.AddInfusionBonus(ownerMasterBonus);
            }
        }

        private void Inventory_AddInfusionBonus(On.RoR2.Inventory.orig_AddInfusionBonus orig, Inventory self, uint value)
        {
            var max = self.GetItemCount(ItemIndex.Infusion) * Maximum;
            if(self.infusionBonus >= max)
            {
                return;
            }
            uint diff = (uint)max - self.infusionBonus;
            if (diff < self.GetItemCount(ItemIndex.Infusion))
            {
                value = diff > 0 ? diff : 0;
            }
            orig(self, value);
        }

        private void TurretInfuseOwner(Inventory inv, uint value)
        {
            try
            {
                var master = inv.GetComponent<CharacterMaster>().GetComponent<Deployable>();
                
            }
        }
        
        private void GlobalEventManager_OnCharacterDeath(MonoMod.Cil.ILContext il)
        {            
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdloc(29),
                x => x.MatchLdcI4(100),
                x => x.MatchMul()
                );
            c.Index += 1;
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, Maximum);

            c.GotoNext(
                x => x.MatchLdloc(51),
                x => x.MatchLdcI4(1),
                x => x.MatchStfld(out FieldReference fr1)
                );
            c.Index += 1;
            c.Remove(); //Remove the 1
            c.Emit(OpCodes.Ldloc, (short)29);  //Infusion Count
            c.Emit(OpCodes.Ldloc, (short)22);  //Inventory
            c.Emit(OpCodes.Callvirt, typeof(Inventory).GetProperty("infusionBonus").GetGetMethod());
            c.EmitDelegate<Func<int, uint, int>>((infusionCount, bonus) =>
            {
                int maximumBonus = infusionCount * 100;
                int currentBonus = (int)bonus;
                if(maximumBonus - currentBonus > infusionCount)
                {
                    return infusionCount;
                }
                else
                {
                    return maximumBonus - currentBonus > 0 ? maximumBonus - currentBonus : 0;
                }
            });
        }

        private int Maximum
        {
            get
            {
                try
                {
                    if (InfusionMaximum.Value < 1)
                    {
                        return 1;
                    }
                    if (InfusionMaximum.Value > 500)
                    {
                        return 500;
                    }
                    return InfusionMaximum.Value;
                }
                catch
                {
                    return 100;
                }
            }
        }
    }
}
