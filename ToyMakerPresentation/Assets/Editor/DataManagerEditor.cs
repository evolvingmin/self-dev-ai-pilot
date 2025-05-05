using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

public class DataManagerEditor : EditorWindow
{
    private DataManager dataManager;
    private string selectedJsonFile = "";
    private Vector2 scrollPosition;
    private Dictionary<int, object> currentData;
    private string newKey = "";
    private string newValue = "";
    private string newCategory = "";
    private string selectedCategory = "";

    [MenuItem("Window/Data Manager Editor")]
    public static void ShowWindow()
    {
        GetWindow<DataManagerEditor>("Data Manager Editor");
    }

    private void OnEnable()
    {
        dataManager = new DataManager();
    }

    private void OnGUI()
    {
        GUILayout.Label("Data Manager", EditorStyles.boldLabel);

        GUILayout.Space(10);

        GUILayout.Label("Select JSON File", EditorStyles.boldLabel);
        if (GUILayout.Button("Select JSON File"))
        {
            string path = EditorUtility.OpenFilePanel("Select JSON File", Application.persistentDataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                selectedJsonFile = path;
                Debug.Log("Selected JSON File: " + selectedJsonFile);
                dataManager.LoadData(selectedJsonFile);
            }
        }

        GUILayout.Label("Selected File: " + selectedJsonFile);

        GUILayout.Space(10);

        GUILayout.Label("Categories", EditorStyles.boldLabel);
        foreach (var category in dataManager.GetDataKeys())
        {
            if (GUILayout.Button(category))
            {
                selectedCategory = category;
                currentData = dataManager.GetDataForEditing(selectedCategory);
            }
        }

        GUILayout.Space(10);

        GUILayout.Label("Add New Category", EditorStyles.boldLabel);
        newCategory = GUILayout.TextField(newCategory);
        if (GUILayout.Button("Add Category"))
        {
            if (!string.IsNullOrEmpty(newCategory))
            {
                dataManager.SetData(newCategory, new Dictionary<int, object>());
                newCategory = "";
            }
        }

        GUILayout.Space(10);

        if (!string.IsNullOrEmpty(selectedCategory) && currentData != null)
        {
            GUILayout.Label($"Editing Category: {selectedCategory}", EditorStyles.boldLabel);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            var keysToRemove = new List<int>(); // 삭제할 키를 추적합니다.
            foreach (var entry in currentData.ToList()) // ToList()로 복사본을 순회합니다.
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("ID: " + entry.Key, GUILayout.Width(100));
                string value = JsonConvert.SerializeObject(entry.Value);
                string newValue = GUILayout.TextField(value);

                if (newValue != value)
                {
                    try
                    {
                        object parsedValue = JsonConvert.DeserializeObject(newValue, entry.Value.GetType());
                        currentData[entry.Key] = parsedValue;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Failed to parse value: " + ex.Message);
                    }
                }

                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    keysToRemove.Add(entry.Key); // 삭제할 키를 추가합니다.
                }

                GUILayout.EndHorizontal();
            }

            // 루프가 끝난 후 삭제 작업을 수행합니다.
            foreach (var key in keysToRemove)
            {
                currentData.Remove(key);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            GUILayout.Label("Add New Item", EditorStyles.boldLabel);
            newKey = GUILayout.TextField(newKey, GUILayout.Width(100));
            newValue = GUILayout.TextField(newValue);

            if (GUILayout.Button("Add Item"))
            {
                if (int.TryParse(newKey, out int key))
                {
                    try
                    {
                        object parsedValue = JsonConvert.DeserializeObject(newValue);
                        currentData[key] = parsedValue;
                        newKey = "";
                        newValue = "";
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Failed to add item: " + ex.Message);
                    }
                }
                else
                {
                    Debug.LogWarning("Invalid key format. Key must be an integer.");
                }
            }

            if (GUILayout.Button("Save Data"))
            {
                dataManager.SaveData(selectedJsonFile);
            }
        }
        else
        {
            GUILayout.Label("No category selected.", EditorStyles.helpBox);
        }
    }
}