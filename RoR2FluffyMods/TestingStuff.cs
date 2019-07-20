﻿using BepInEx;
using BepInEx.Configuration;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace RoR2FluffyMods
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.FluffyMods.TestingStuff", "TestingStuff", "0.0.0")]
    public class TestingStuff : BaseUnityPlugin
    {
        public void Awake()
        {
            IL.RoR2.ChestBehavior.ItemDrop += ChestBehavior_ItemDrop;            
        }

        private void ChestBehavior_ItemDrop(ILContext il)
        {
            var c = new ILCursor(il);
            //c.GotoNext(
            //    x => x.MatchLdarg(0),
            //    x => x.MatchLdsfld(out FieldReference fr1),
            //    x => x.MatchLdsfld(out FieldReference fr2),
            //    x => x.MatchCall(out MethodReference mr1));
            //c.Index += 2;

            c.GotoNext(x => x.MatchRet());
            Debug.Log(c);
            c.Index += 3;
            Debug.Log(c);

            c.EmitDelegate<Func<PickupIndex, PickupIndex>>((dropPickup) =>
            {
                if (dropPickup.itemIndex != ItemIndex.None)
                {
                    var item = dropPickup.itemIndex.ToString();
                    Message.SendToAll($"Motherfucker it's a {item}!!", Colours.LightBlue);
                }
                return dropPickup;
            });
        }
    }
}
