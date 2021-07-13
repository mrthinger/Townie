using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CropSaver
{
    class ModDataLoader
    {
        private static readonly string CROP_ADDED_EVENT = "CROP_ADDED_EVENT";
        private static readonly string CROP_REMOVED_EVENT = "CROP_REMOVED_EVENT";
        private static readonly string CROP_EXTRA_DAY_INCREMENT_EVENT = "CROP_EXTRA_DAY_INCREMENT_EVENT";

        public static readonly string MOD_DATA_KEY = "MrThinger.Townie.data";
        private ModData data = new ModData();

        private IModHelper helper;
        public ModDataLoader(IModHelper helper)
        {
            this.helper = helper;
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
        }

        private void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            data.crops.ForEach(crop =>
            {
                this.helper.Multiplayer.SendMessage(crop, CROP_ADDED_EVENT, playerIDs: new[] { e.Peer.PlayerID });
            });
        }

        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.Type == CROP_ADDED_EVENT)
            {
                SaverCrop crop = e.ReadAs<SaverCrop>();
                OnCropAdded(crop);
            }

            if (e.Type == CROP_REMOVED_EVENT)
            {
                SaverCrop crop = e.ReadAs<SaverCrop>();
                OnCropRemoved(crop);
            }

            if (e.Type == CROP_EXTRA_DAY_INCREMENT_EVENT)
            {
                SaverCrop crop = e.ReadAs<SaverCrop>();
                onIncrementSaverCropDays(crop);
            }
        }


        private void onIncrementSaverCropDays(SaverCrop cropToUpdate)
        {
            var crop = data.crops.Find(c => c.Equals(cropToUpdate));

            if (crop != null)
            {
                crop.IncrementExtraDays();
            }
        }
        public void ClientIncrementSaverCropDays(SaverCrop crop)
        {
            onIncrementSaverCropDays(crop);
            this.helper.Multiplayer.SendMessage(crop, CROP_EXTRA_DAY_INCREMENT_EVENT);
        }

        private void OnCropAdded(SaverCrop crop)
        {
            data.crops.Add(crop);
        }
        public void ClientAddCrop(SaverCrop crop)
        {
            OnCropAdded(crop);
            this.helper.Multiplayer.SendMessage(crop, CROP_ADDED_EVENT);
        }

        private void OnCropRemoved(SaverCrop crop)
        {
            data.crops.Remove(crop);
        }
        public void ClientRemoveCrop(string locationName, Vector2 tileLocation)
        {

            var crop = GetSaverCrop(locationName, tileLocation);
            if (crop != null)
            {
                OnCropRemoved(crop);
                this.helper.Multiplayer.SendMessage(crop, CROP_REMOVED_EVENT);
            }
        }

        public SaverCrop GetSaverCrop(string locationName, Vector2 tileLocation)
        {
            int i = data.crops.FindIndex((crop) => crop.equalsCrop(locationName, tileLocation));
            if (i != -1)
            {
                return data.crops.ElementAt(i);
            }

            return null;
        }

        public List<SaverCrop> GetSaverCrops()
        {
            return data.crops;
        }

        public void LoadDataFromDisk()
        {
            this.data = this.helper.Data.ReadSaveData<ModData>(MOD_DATA_KEY) ?? new ModData();
        }

        public void SaveDataToDisk()
        {
            this.helper.Data.WriteSaveData(MOD_DATA_KEY, this.data);
        }



    }
}
