using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.IO;
using TownieShared.API;

namespace OfflineProgress
{
    public class ModEntry : Mod
    {
        private IJsonAssetsApi Ja;
        public int RareCandy => this.Ja.GetObjectId("Rare Candy");

        public override void Entry(IModHelper helper)
        {

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var api = this.Helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets");
            if (api == null)
            {
                return;
            }
            this.Ja = api;

            api.LoadAssets(Path.Combine(this.Helper.DirectoryPath, "assets"));
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;



            if (e.Button.Equals(SButton.Space))
            {
                Game1.player.addItemByMenuIfNecessary((Item)new StardewValley.Object(RareCandy, 1));
            }

            if (e.Button.Equals(SButton.MouseRight) && Game1.player.ActiveObject is Item item && item != null && item.ParentSheetIndex == RareCandy)
            {
                var tile = Game1.currentCursorTile;
                GameLocation location = Game1.currentLocation;
                if (location == null)
                    return;
                if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature terrainFeature))
                {
                    if (terrainFeature is HoeDirt dirt && dirt.crop is Crop crop && crop != null)
                    {
                        crop.newDay(HoeDirt.watered, HoeDirt.fertilizerHighQuality, (int)tile.X, (int)tile.Y, location);
                    }
                }

                Game1.player.removeItemFromInventory(item);
            }
        }

    }
}
