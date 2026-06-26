using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using System;

namespace PetOutOfMyWay
{
    internal class PetPushPatch
    {
        private static IMonitor Monitor;
        private static Func<ModConfig>? GetConfig;
        public static void Initialize(IMonitor monitor, Func<ModConfig> getConfig)
        {
            Monitor = monitor;
            GetConfig = getConfig;
        }

        public static void getTimeFarmerMustPushBeforeStartShaking_Postfix(ref int __result) 
        {
            try
            {
                __result = GetConfig!().TimeFarmerMustPushBeforeStartShaking;
                Monitor.Log($"push time before shake: {GetConfig!().TimeFarmerMustPushBeforeStartShaking}", LogLevel.Trace);
            } catch
            {
                Monitor.Log($"Modified 'push time before shake' error", LogLevel.Warn);
            }
            
        }

        public static void getTimeFarmerMustPushBeforePassingThrough_Postfix(ref int __result)
        {
            try
            {
                __result = GetConfig!().TimeFarmerMustPushBeforePassingThrough;
                Monitor.Log($"push time before shake: {GetConfig!().TimeFarmerMustPushBeforePassingThrough}", LogLevel.Trace);
            }
            catch
            {
                Monitor.Log($"Modified 'push time before pass' error", LogLevel.Warn);
            }
        }
    }
}
