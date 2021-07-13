using System;
using System.Collections.Generic;
using Harmony;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace CropSaver
{
    public class ModEntry : Mod
    {
        public static readonly string MOD_KEY = "MrThinger.Townie";

        private ModDataLoader loader;

        public override void Entry(IModHelper helper)
        {
            loader = new ModDataLoader(helper);
            CropSaverOverrides.Initialize(helper, loader);

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.Saving += OnSaving;

            helper.Events.GameLoop.DayEnding += OnDayEnd;
        }

        private void OnDayEnd(object sender, DayEndingEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                //prolong crops
                HashSet<long> onlineIds = new HashSet<long>();

                foreach (Farmer farmer in Game1.getOnlineFarmers())
                {
                    onlineIds.Add(farmer.uniqueMultiplayerID);
                }

                loader.GetSaverCrops().ForEach(townieCrop =>
                {

                    var dirt = townieCrop.TryGetCoorespondingDirt();
                    if (dirt != null)
                    {
                        if (!onlineIds.Contains(townieCrop.ownerId) && dirt.needsWatering())
                        {
                            loader.ClientIncrementSaverCropDays(townieCrop);
                        }
                    }
                });

                //remove crops
                for (int i = loader.GetSaverCrops().Count - 1; i >= 0; i--)
                {

                    var townieCrop = loader.GetSaverCrops()[i];
                    var crop = townieCrop.TryGetCoorespondingCrop();
                    if (crop != null)
                    {

                        int numSeasons = crop.seasonsToGrowIn.Count - (crop.seasonsToGrowIn.IndexOf(townieCrop.datePlanted.Season));
                        
                        int numDaysToLive = townieCrop.extraDays + (28 * numSeasons) - townieCrop.datePlanted.Day;
                        SDate dateOfDeath = townieCrop.datePlanted.AddDays(numDaysToLive);

                        var currentPhase = this.Helper.Reflection.GetField<NetInt>(crop, "currentPhase").GetValue().Value;
                        var phaseDays = this.Helper.Reflection.GetField<NetIntList>(crop, "phaseDays").GetValue();


                        var fullyGrown = (currentPhase >= phaseDays.Count - 1);

                        if (SDate.Now() > dateOfDeath && !(fullyGrown && onlineIds.Contains(townieCrop.ownerId)))
                        {
                            loader.ClientRemoveCrop(townieCrop.cropLocationName, townieCrop.cropLocationTile);
                            var dead = this.Helper.Reflection.GetField<NetBool>(crop, "dead").GetValue();
                            var raisedSeeds = this.Helper.Reflection.GetField<NetBool>(crop, "raisedSeeds").GetValue();

                            dead.Value = true;
                            raisedSeeds.Value = false;
                        }
                    }

                }


            }

        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                loader.SaveDataToDisk();
            }
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var harmony = HarmonyInstance.Create(MOD_KEY);

            harmony.Patch(
                original: AccessTools.Method(typeof(HoeDirt), nameof(HoeDirt.plant)),
                postfix: new HarmonyMethod(typeof(CropSaverOverrides), nameof(CropSaverOverrides.Plant))
            );
            
            harmony.Patch(
                original: AccessTools.Method(typeof(HoeDirt), nameof(HoeDirt.destroyCrop)),
                prefix: new HarmonyMethod(typeof(CropSaverOverrides), nameof(CropSaverOverrides.DestroyCrop_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Crop), nameof(Crop.Kill)),
                prefix: new HarmonyMethod(typeof(CropSaverOverrides), nameof(CropSaverOverrides.KillCrop_Prefix))
            );

        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                loader.LoadDataFromDisk();
            }
        }

    }

}