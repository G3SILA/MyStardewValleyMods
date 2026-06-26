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
        private static ModConfig Config;
        public static void Initialize(IMonitor monitor, ModConfig config)
        {
            Config = config;
            Monitor = monitor;
        }

        public static void getTimeFarmerMustPushBeforeStartShaking_Postfix(ref int __result) 
        {
            try
            {
                __result = Config.TimeFarmerMustPushBeforeStartShaking;
                Monitor.Log($"push time before shake: {Config.TimeFarmerMustPushBeforeStartShaking}", LogLevel.Trace);
            } catch
            {
                Monitor.Log($"Modified 'push time before shake' error", LogLevel.Warn);
            }
            
        }

        public static void getTimeFarmerMustPushBeforePassingThrough_Postfix(ref int __result)
        {
            try
            {
                __result = Config.TimeFarmerMustPushBeforePassingThrough;
                Monitor.Log($"push time before shake: {Config.TimeFarmerMustPushBeforePassingThrough}", LogLevel.Trace);
            }
            catch
            {
                Monitor.Log($"Modified 'push time before pass' error", LogLevel.Warn);
            }
        }
    }
}
