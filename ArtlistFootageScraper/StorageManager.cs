using Newtonsoft.Json;
using System.IO;

namespace ArtlistFootageScraper
{
    public static class StorageManager
    {
        private const string StorageFilePath = "storage.json";

        public static Storage? LoadStorage()
        {
            if (File.Exists(StorageFilePath))
            {
                string json = File.ReadAllText(StorageFilePath);
                return JsonConvert.DeserializeObject<Storage>(json);
            }
            return new Storage();
        }

        public static void SaveStorage(Storage storage)
        {
            string json = JsonConvert.SerializeObject(storage);
            File.WriteAllText(StorageFilePath, json);
        }
    }
}
