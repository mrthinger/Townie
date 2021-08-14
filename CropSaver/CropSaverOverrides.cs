using System;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using Harmony;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using Microsoft.Xna.Framework;

namespace CropSaver
{
    internal class CropSaverOverrides
    {

        private static IModHelper Helper;
        private static ModDataLoader Loader;

        public static void Initialize(IModHelper helper, ModDataLoader loader)
        {
            Helper = helper;
            Loader = loader;
        }

        public static void Plant(ref HoeDirt __instance, ref bool __result, int index, int tileX, int tileY, Farmer who, bool isFertilizer, GameLocation location)
        {


            if (__result && __instance.crop != null && location.Name.Equals("Farm"))
            {
                SaverCrop crop = new SaverCrop(location.Name, new Vector2(tileX, tileY), who.UniqueMultiplayerID, SDate.Now());
                Loader.ClientAddCrop(crop);
            }

        }

        public static bool DestroyCrop_Prefix(ref HoeDirt __instance, Vector2 tileLocation, bool showAnimation, GameLocation location)
        {

            if (__instance.crop != null)
            {
                Loader.ClientRemoveCrop(location.Name, tileLocation);
            }
            return true;

        }

        public static bool KillCrop_Prefix(ref Crop __instance)
        {

            var cropLocation = Helper.Reflection.GetField<Vector2>(__instance, "tilePosition").GetValue();

            var townieCrop = Loader.GetSaverCrop("Farm", cropLocation);

            if (townieCrop != null) return false;

            return true;

        }
    }
}