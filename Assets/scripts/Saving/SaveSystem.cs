using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Diagnostics;

public class SaveSystem : MonoBehaviour
{
    static public SaveSystem instance;
    private void Awake()
    {
        instance = this;
    }

    public void SaveSection<T>(string saveFolder, string sectionName, T data)
    {
        UnityEngine.Debug.Log($"Saving section: {sectionName} in folder: {saveFolder}");
        string dir = Path.Combine(Application.persistentDataPath, saveFolder);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, sectionName + ".json");
        File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
    }

    public T LoadSection<T>(string saveFolder, string sectionName)
    {
        string path = Path.Combine(Application.persistentDataPath, saveFolder, sectionName + ".json");
        if (File.Exists(path))
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        return default;
    }
}

public class TestData
{
    public string text;
    public int number;
    public float floatNumber;
}
