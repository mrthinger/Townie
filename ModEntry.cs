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

namespace Townie
{
    public class ModEntry : Mod
    {
        public static readonly string MOD_KEY = "MrThinger.Townie";

        private ModDataLoader loader;

        public override void Entry(IModHelper helper)
        {
            loader = new ModDataLoader(helper);
            TownieOverrides.Initialize(helper, Monitor, loader);

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

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

                loader.GetTownieCrops().ForEach(townieCrop =>
                {

                    var dirt = townieCrop.TryGetCoorespondingDirt();
                    if (dirt != null)
                    {
                        if (!onlineIds.Contains(townieCrop.ownerId) && dirt.needsWatering())
                        {
                            loader.ClientIncrementTownieCropDays(townieCrop);
                        }
                    }
                });

                //remove crops
                for (int i = loader.GetTownieCrops().Count - 1; i >= 0; i--)
                {

                    var townieCrop = loader.GetTownieCrops()[i];
                    var crop = townieCrop.TryGetCoorespondingCrop();
                    if (crop != null)
                    {

                        int numSeasons = crop.seasonsToGrowIn.Count;
                        int numDaysToLive = townieCrop.extraDays + (28 * numSeasons) - townieCrop.datePlanted.Day;
                        SDate dateOfDeath = townieCrop.datePlanted.AddDays(numDaysToLive);
                        var fullyGrown = this.Helper.Reflection.GetField<NetBool>(crop, "fullyGrown").GetValue().Value;

                        if (SDate.Now() > dateOfDeath && !fullyGrown)
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
                postfix: new HarmonyMethod(typeof(TownieOverrides), nameof(TownieOverrides.Plant))
            );
            
            harmony.Patch(
                original: AccessTools.Method(typeof(HoeDirt), nameof(HoeDirt.destroyCrop)),
                prefix: new HarmonyMethod(typeof(TownieOverrides), nameof(TownieOverrides.DestroyCrop_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Crop), nameof(Crop.Kill)),
                prefix: new HarmonyMethod(typeof(TownieOverrides), nameof(TownieOverrides.KillCrop_Prefix))
            );

        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                loader.LoadDataFromDisk();
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // print button presses to the console window
            Monitor.Log($"{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);
            if (e.Button.Equals(SButton.Space))
                this.GrowCropOnPlayer();

            if (e.Button.Equals(SButton.MouseRight))
            {
                var tile = Game1.currentCursorTile;
                GameLocation location = Game1.currentLocation;
                if (location == null)
                    return;
                if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature terrainFeature))
                {
                    if (terrainFeature is HoeDirt dirt && dirt.crop is Crop crop && crop != null)
                    {
                        var cropLocation = this.Helper.Reflection.GetField<Vector2>(crop, "tilePosition").GetValue();

                        var loadedCrop = loader.GetTownieCrop(location.name.Value, cropLocation);
                        if (loadedCrop != null)
                        {
                            Monitor.Log($"{loadedCrop.ownerId}", LogLevel.Debug);
                        }
                    }
                }
            }
        }

        private void GrowCropOnPlayer()
        {
            Vector2 playerTile = Game1.player.getTileLocation();
            GameLocation location = Game1.currentLocation;
            if (location == null)
                return;

            // terrain feature
            if (location.terrainFeatures.TryGetValue(playerTile, out TerrainFeature terrainFeature))
            {
                if (terrainFeature is HoeDirt dirt)
                {

                    Crop crop = dirt.crop;
                    crop.growCompletely();
                }
            }
        }



    }

}