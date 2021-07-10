using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Townie
{
    public class ModData
    {
        public List<TownieCrop> crops { get; set; } = new List<TownieCrop>();

    }

    public class TownieCrop
    {
        public string cropLocationName;
        public Vector2 cropLocationTile;
        public long ownerId;
        public SDate datePlanted;

        public int extraDays;


        public TownieCrop(string cropLocationName, Vector2 cropLocationTile, long ownerId, SDate datePlanted, int extraDays = 0)
        {
            this.cropLocationName = cropLocationName;
            this.cropLocationTile = cropLocationTile;
            this.ownerId = ownerId;
            this.datePlanted = datePlanted;
            this.extraDays = extraDays;
        }


        public void IncrementExtraDays() {
            extraDays++;
        }

        public bool equalsCrop(string cropLocation, Vector2 cropPosition) {
            return cropLocation.Equals(cropLocationName) && cropLocationTile.Equals(cropPosition);
        }

        public Crop TryGetCoorespondingCrop() {
            var location = Game1.getLocationFromName(cropLocationName);
            if (location.terrainFeatures.TryGetValue(cropLocationTile, out TerrainFeature terrainFeature))
            {
                if (terrainFeature is HoeDirt dirt && dirt != null && dirt.crop is Crop crop && crop != null)
                {
                    return crop;
                }
            }
            return null;
        }

        public HoeDirt TryGetCoorespondingDirt()
        {
            var location = Game1.getLocationFromName(cropLocationName);
            if (location.terrainFeatures.TryGetValue(cropLocationTile, out TerrainFeature terrainFeature))
            {
                if (terrainFeature is HoeDirt dirt)
                {
                    return dirt;
                }
            }
            return null;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }


            if (obj is TownieCrop otherCrop) {

                return this.cropLocationName.Equals(otherCrop.cropLocationName)
                    && this.cropLocationTile.Equals(otherCrop.cropLocationTile)
                    && this.ownerId == otherCrop.ownerId
                    && this.datePlanted.Equals(otherCrop.datePlanted);
            }

            return false;
        }

    }
}
