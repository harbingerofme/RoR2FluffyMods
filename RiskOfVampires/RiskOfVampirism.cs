﻿using BepInEx;
using BepInEx.Configuration;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;

namespace RiskOfVampirism
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.FluffyMods.RiskOfVampirism", "RiskOfVampirism", "1.0.0")]
    public class RiskOfVampirism : BaseUnityPlugin
    {
        private static ConfigWrapper<int> DecayTime { get; set; }
        private static ConfigWrapper<int> LeechCount { get; set; }

        public void Awake()
        {
            DecayTime = Config.Wrap<int>(
                "VampireSetup",
                "DecayTime",
                "The time(s) it takes for health of vampires to degenerate",
                60);
            LeechCount = Config.Wrap<int>(
                "VampireSetup",
                "LeechCount",
                "The amount of leech given to vampires",
                5);

            On.RoR2.Console.Awake += (orig, self) =>
            {
                CommandHelper.RegisterCommands(self);
                orig(self);
            };

            //On.RoR2.Run.Start += Run_Start;
            On.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManager_OnHitEnemy;
            IL.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
        }

        private void GlobalEventManager_OnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if (!damageInfo.procChainMask.HasProc(ProcType.HealOnHit))
            {
                var attacker = damageInfo.attacker.GetComponent<CharacterBody>();  HealthComponent hc = attacker.GetComponent<HealthComponent>();
                if ((bool)((UnityEngine.Object)hc))
                {
                    ProcChainMask procChainMask = damageInfo.procChainMask;
                    procChainMask.AddProc(ProcType.HealOnHit);
                    double num = (double)hc.Heal(LeechCount.Value * damageInfo.procCoefficient, procChainMask, true);
                    Logger.LogInfo($"LeechINFO: attacker={attacker.name}, num={num}, leechcount={LeechCount.Value}, procCoef={damageInfo.procCoefficient}");
                    Logger.LogInfo($"RegenINFO: attacker={attacker.name}, level= {attacker.level}, baseReg={attacker.baseRegen}, lvlReg={attacker.levelRegen}, reg={attacker.regen}");
                }               
            }
            orig(self, damageInfo, victim);
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);
            foreach (var cm in RoR2.PlayerCharacterMasterController.instances)
            {
                cm.master.inventory.GiveItem(ItemIndex.HealthDecay, DecayTime.Value);
            }
        }

        private void CharacterBody_RecalculateStats(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(out FieldReference fr1),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(out FieldReference fr2),
                x => x.MatchLdloc(27),
                x => x.MatchMul(),
                x => x.MatchAdd(),
                x => x.MatchStloc(36)
                );
            c.Index -= 1;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, CharacterBody, float>>((a, body) =>
            {
                var levelCoeff = body.level - 1;
                var maxHealth = body.maxHealth;
                return -1f * maxHealth/(60 + 5*levelCoeff);
            });
        }

        #region ConsoleCommands
        /// <summary>
        /// Sets the number of leech items given at start of run.
        /// </summary>
        /// <param name="args">args\[0\]=Value(int)</param>
        [ConCommand(commandName = "vampire_leech", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets the number of leech items given at start of run. args[0]=Value(int).")]
        private static void VampireLeechSet(ConCommandArgs args)
        {
            try
            {
                LeechCount.Value = Int32.Parse(args[0]);
                Debug.Log($"Leech count set to {args[0]}");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        /// <summary>
        /// Sets the decay time given at start of run.
        /// </summary>
        /// <param name="args">args\[0\]=Value(int)</param>
        [ConCommand(commandName = "vampire_decay", flags = ConVarFlags.ExecuteOnServer, helpText = "Sets decay time given at start of run. args[0]=Value(int).")]
        private static void VampireDecaySet(ConCommandArgs args)
        {
            try
            {
                DecayTime.Value = Int32.Parse(args[0]);
                Debug.Log($"decay time set to {args[0]}");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }
        #endregion
    }
}
