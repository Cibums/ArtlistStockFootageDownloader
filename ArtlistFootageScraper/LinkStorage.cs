using Newtonsoft.Json;
using System.IO;

namespace ArtlistFootageScraper
{
    public class LinkStorage
    {
        private const string StorageFilePath = "linkStorage.json";

        public Storage LoadStorage()
        {
            if (File.Exists(StorageFilePath))
            {
                string json = File.ReadAllText(StorageFilePath);
                return JsonConvert.DeserializeObject<Storage>(json);
            }
            return new Storage();
        }

        public void SaveStorage(Storage storage)
        {
            string json = JsonConvert.SerializeObject(storage);
            File.WriteAllText(StorageFilePath, json);
        }
    }
}
