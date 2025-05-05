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
    private string newCategory = "";
    private string selectedCategory = "";
    private object cachedNewItem;
    private string searchQuery = "";

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
            SelectJsonFile();
        }

        GUILayout.Label("Selected File: " + selectedJsonFile);

        GUILayout.Space(10);

        GUILayout.Label("Categories", EditorStyles.boldLabel);
        foreach (var category in dataManager.GetDataKeys())
        {
            if (GUILayout.Button(category))
            {
                SelectCategory(category);
            }
        }

        GUILayout.Space(10);

        GUILayout.Label("Add New Category", EditorStyles.boldLabel);
        newCategory = GUILayout.TextField(newCategory);
        if (GUILayout.Button("Add Category"))
        {
            AddNewCategory();
        }

        GUILayout.Space(10);

        if (!string.IsNullOrEmpty(selectedCategory) && currentData != null)
        {
            GUILayout.Label($"Editing Category: {selectedCategory}", EditorStyles.boldLabel);

            GUILayout.Label("Search Items", EditorStyles.boldLabel);
            searchQuery = GUILayout.TextField(searchQuery);

            GUILayout.Space(10);

            DrawFilteredData();

            GUILayout.Space(10);

            GUILayout.Label("Add New Item", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(selectedCategory))
            {
                DrawNewItemFields();
            }

            if (GUILayout.Button("Save Data"))
            {
                SaveData();
            }
        }
        else
        {
            GUILayout.Label("No category selected.", EditorStyles.helpBox);
        }
    }

    private void SelectJsonFile()
    {
        string path = EditorUtility.OpenFilePanel("Select JSON File", Application.persistentDataPath, "json");
        if (!string.IsNullOrEmpty(path))
        {
            selectedJsonFile = path;
            Debug.Log("Selected JSON File: " + selectedJsonFile);
            dataManager.LoadData(selectedJsonFile);
        }
    }

    private void SelectCategory(string category)
    {
        selectedCategory = category;
        currentData = dataManager.GetDataForEditing(selectedCategory);
    }

    private void AddNewCategory()
    {
        if (!string.IsNullOrEmpty(newCategory))
        {
            dataManager.SetData(newCategory, new Dictionary<int, object>());
            newCategory = "";
        }
    }

    private void DrawFilteredData()
    {
        IEnumerable<KeyValuePair<int, object>> filteredData = currentData;
        if (!string.IsNullOrEmpty(searchQuery))
        {
            filteredData = FilterDataBySearchQuery();
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        var keysToRemove = new List<int>();
        foreach (var entry in filteredData.ToList())
        {
            DrawDataEntry(entry, keysToRemove);
        }

        foreach (var key in keysToRemove)
        {
            currentData.Remove(key);
        }

        GUILayout.EndScrollView();
    }

    private IEnumerable<KeyValuePair<int, object>> FilterDataBySearchQuery()
    {
        return currentData.Where(entry =>
        {
            var entryType = entry.Value.GetType();
            foreach (var property in entryType.GetProperties())
            {
                if (property.CanRead && property.GetValue(entry.Value)?.ToString().Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }
            }
            return false;
        });
    }

    private void DrawDataEntry(KeyValuePair<int, object> entry, List<int> keysToRemove)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("ID: " + entry.Key, GUILayout.Width(100));

        DrawEntryFields(entry);

        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            keysToRemove.Add(entry.Key);
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10); // Add vertical spacing between items
    }

    private void DrawEntryFields(KeyValuePair<int, object> entry)
    {
        var entryType = entry.Value.GetType();
        if (entryType.IsClass || entryType.IsValueType && !entryType.IsPrimitive)
        {
            GUILayout.BeginVertical();

            foreach (var property in entryType.GetProperties())
            {
                if (!property.CanWrite || property.Name == "Id") continue;

                var propertyValue = property.GetValue(entry.Value)?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(searchQuery) && propertyValue.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    GUI.backgroundColor = Color.yellow; // Highlight matching property
                }
                else
                {
                    GUI.backgroundColor = Color.white; // Reset background color
                }

                DrawField(property.Name, entry.Value, property);
            }

            GUI.backgroundColor = Color.white; // Reset background color after processing all properties
            GUILayout.EndVertical();
        }
        else
        {
            DrawPrimitiveField(entry);
        }
    }

    private void DrawPrimitiveField(KeyValuePair<int, object> entry)
    {
        if (entry.Value is int intValue)
        {
            string newValue = GUILayout.TextField(intValue.ToString());
            if (int.TryParse(newValue, out int parsedValue) && parsedValue != intValue)
            {
                currentData[entry.Key] = parsedValue;
            }
        }
        else if (entry.Value is float floatValue)
        {
            string newValue = GUILayout.TextField(floatValue.ToString());
            if (float.TryParse(newValue, out float parsedValue) && parsedValue != floatValue)
            {
                currentData[entry.Key] = parsedValue;
            }
        }
        else if (entry.Value is string stringValue)
        {
            string newValue = GUILayout.TextField(stringValue);
            if (newValue != stringValue)
            {
                currentData[entry.Key] = newValue;
            }
        }
        else if (entry.Value is Enum enumValue)
        {
            string[] enumNames = Enum.GetNames(enumValue.GetType());
            int selectedIndex = Array.IndexOf(enumNames, enumValue.ToString());
            int newIndex = EditorGUILayout.Popup(selectedIndex, enumNames);
            if (newIndex != selectedIndex)
            {
                currentData[entry.Key] = Enum.Parse(enumValue.GetType(), enumNames[newIndex]);
            }
        }
        else
        {
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
        }
    }

    private void DrawNewItemFields()
    {
        var categoryType = dataManager.GetSupportedDataType(selectedCategory);
        if (categoryType != null)
        {
            if (cachedNewItem == null || cachedNewItem.GetType() != categoryType)
            {
                cachedNewItem = Activator.CreateInstance(categoryType);
            }
            var newItem = cachedNewItem;

            DrawFieldsForObject(newItem);

            if (GUILayout.Button("Add Item"))
            {
                AddNewItem(newItem, categoryType);
            }
        }
    }

    private void AddNewItem(object newItem, Type categoryType)
    {
        int newId = currentData.Keys.Count > 0 ? currentData.Keys.Max() + 1 : 1;
        var idProperty = categoryType.GetProperty("Id");
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(newItem, newId);
        }
        currentData[newId] = newItem;
    }

    private void SaveData()
    {
        dataManager.SaveData(selectedJsonFile);
    }

    private void DrawField(string label, object target, System.Reflection.PropertyInfo property)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(100));

        var propertyValue = property.GetValue(target);
        if (property.PropertyType == typeof(int))
        {
            string input = GUILayout.TextField(propertyValue?.ToString() ?? "0");
            if (int.TryParse(input, out int value))
            {
                property.SetValue(target, value);
            }
        }
        else if (property.PropertyType == typeof(float))
        {
            string input = GUILayout.TextField(propertyValue?.ToString() ?? "0.0");
            if (float.TryParse(input, out float value))
            {
                property.SetValue(target, value);
            }
        }
        else if (property.PropertyType == typeof(string))
        {
            string input = GUILayout.TextField(propertyValue?.ToString() ?? string.Empty);
            if (input != propertyValue?.ToString())
            {
                property.SetValue(target, input);
            }
        }
        else if (property.PropertyType.IsEnum)
        {
            string[] enumNames = Enum.GetNames(property.PropertyType);
            int selectedIndex = Array.IndexOf(enumNames, propertyValue?.ToString() ?? enumNames[0]);
            int newIndex = EditorGUILayout.Popup(selectedIndex, enumNames);
            if (newIndex != selectedIndex)
            {
                property.SetValue(target, Enum.Parse(property.PropertyType, enumNames[newIndex]));
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DrawFieldsForObject(object target)
    {
        var type = target.GetType();
        foreach (var property in type.GetProperties())
        {
            if (!property.CanWrite || property.Name == "Id") continue;
            DrawField(property.Name, target, property);
        }
    }
}