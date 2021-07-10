using System;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using Harmony;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using Microsoft.Xna.Framework;

namespace Townie
{
    internal class TownieOverrides
    {

        private static IModHelper Helper;
        private static IMonitor Monitor;
        private static ModDataLoader Loader;

        public static void Initialize(IModHelper helper, IMonitor monitor, ModDataLoader loader)
        {
            Helper = helper;
            Monitor = monitor;
            Loader = loader;
        }

        public static void Plant(ref HoeDirt __instance, ref bool __result, int index, int tileX, int tileY, Farmer who, bool isFertilizer, GameLocation location)
        {

            Monitor.Log("Plant hit", LogLevel.Debug);
            Monitor.Log($"{who.Name}-{who.uniqueMultiplayerID}", LogLevel.Debug);
            Monitor.Log($"{__instance.crop.netSeedIndex.Value}", LogLevel.Debug);
            Monitor.Log($"{location.name.Value}", LogLevel.Debug);



            if (__result && __instance.crop != null)
            {
                var crop = new TownieCrop(location.name.Value, new Vector2(tileX, tileY), who.uniqueMultiplayerID.Value, SDate.Now());
                Loader.ClientAddCrop(crop);
                __instance.crop.growCompletely();
            }

        }

        public static bool DestroyCrop_Prefix(ref HoeDirt __instance, Vector2 tileLocation, bool showAnimation, GameLocation location)
        {

            if (__instance.crop != null)
            {
                Loader.ClientRemoveCrop(location.name.Value, tileLocation);
            }
            return true;

        }

        public static bool KillCrop_Prefix(ref Crop __instance)
        {

            var cropLocation = Helper.Reflection.GetField<Vector2>(__instance, "tilePosition").GetValue();

            var townieCrop = Loader.GetTownieCrop("Farm", cropLocation);

            if (townieCrop != null) return false;

            return true;

        }
    }
}