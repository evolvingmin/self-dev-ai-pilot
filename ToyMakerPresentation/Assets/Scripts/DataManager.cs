using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using ToyProject.Data;
using Newtonsoft.Json.Linq;
using System.Text;

public class DataManager
{
    private Dictionary<string, Dictionary<int, object>> dataStore = new ();

    private Dictionary<string, Type> supportedTypes = new ();

    public DataManager()
    {
        LoadSupportedDataTypes();
        //LoadData("game_data.json");
    }

    private string ResolveFilePath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    public void SaveData(string fileName)
    {
        string filePath = ResolveFilePath(fileName);
        string json = JsonConvert.SerializeObject(dataStore, Formatting.Indented);
        File.WriteAllText(filePath, json, new UTF8Encoding(true));
        Debug.Log("Data saved to: " + filePath);
    }

    public void LoadData(string fileName)
    {
        string filePath = ResolveFilePath(fileName);
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath, new UTF8Encoding(true));
            JObject root = JObject.Parse(json);

            foreach (var category in root)
            {
                string typeName = category.Key;
                Type targetType = Type.GetType($"ToyProject.Data.{typeName}");

                if (targetType == null)
                {
                    Debug.LogWarning($"Type not found for: {typeName}");
                    continue;
                }

                Dictionary<int, object> categoryDict = new();

                // 객체로 처리
                if (category.Value is JObject objItems)
                {
                    foreach (var item in objItems)
                    {
                        if (!int.TryParse(item.Key, out int id))
                        {
                            Debug.LogWarning($"Invalid key format in category '{typeName}': {item.Key}");
                            continue;
                        }

                        object parsed = item.Value.ToObject(targetType);
                        categoryDict[id] = parsed;
                    }
                }
                else
                {
                    Debug.LogWarning($"Category '{typeName}' is not an object.");
                    continue;
                }

                dataStore[typeName] = categoryDict;
            }
        }
    }

    
    public T GetData<T>(int key) where T : class
    {
        if (dataStore.TryGetValue(typeof(T).Name, out var categoryData) && categoryData.TryGetValue(key, out var item))
        {
            return item as T;
        }

        Debug.LogWarning($"Data not found for category '{typeof(T).Name}' and key '{key}'.");
        return null;
    }

    public void SetData(string category, Dictionary<int, object> data)
    {
        if (dataStore.ContainsKey(category))
        {
            Debug.LogWarning($"Category '{category}' already exists. Overwriting data.");
        }
        dataStore[category] = data;
    }

    public IEnumerable<string> GetDataKeys()
    {
        return dataStore.Keys;
    }

    private void LoadSupportedDataTypes()
    {
        var assembly = typeof(DataManager).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type.Namespace == "ToyProject.Data" && (type.IsClass || type.IsValueType) && !type.IsAbstract)
            {
                supportedTypes[type.Name] = type;
            }
        }
    }

    public IEnumerable<Type> GetSupportedDataTypes()
    {
        return supportedTypes.Values;
    }

    public Type GetSupportedDataType(string typeString)
    {
        var targetType = Type.GetType($"ToyProject.Data.{typeString}");
        
        return targetType;
    }

    public Dictionary<int, object> GetDataForEditing(string category)
    {
        if (dataStore.TryGetValue(category, out var categoryData))
        {
            return categoryData; // 기존 데이터를 직접 반환하여 수정 사항이 반영되도록 변경
        }

        Debug.LogWarning($"Category '{category}' not found in data store.");
        return null;
    }
}