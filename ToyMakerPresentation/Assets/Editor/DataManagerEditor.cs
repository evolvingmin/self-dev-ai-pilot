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
    private object cachedNewItem;

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

                // entry.Value가 클래스나 구조체인지 확인
                var entryType = entry.Value.GetType();
                if (entryType.IsClass || entryType.IsValueType && !entryType.IsPrimitive)
                {
                    GUILayout.BeginVertical();

                    // 클래스의 프로퍼티를 순회
                    foreach (var property in entryType.GetProperties())
                    {
                        if (!property.CanRead || !property.CanWrite) continue; // 읽기/쓰기 가능한 프로퍼티만 처리

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(property.Name, GUILayout.Width(100));

                        var propertyValue = property.GetValue(entry.Value);
                        if (propertyValue is int intValue)
                        {
                            string newValue = GUILayout.TextField(intValue.ToString());
                            if (int.TryParse(newValue, out int parsedValue) && parsedValue != intValue)
                            {
                                property.SetValue(entry.Value, parsedValue);
                            }
                        }
                        else if (propertyValue is float floatValue)
                        {
                            string newValue = GUILayout.TextField(floatValue.ToString());
                            if (float.TryParse(newValue, out float parsedValue) && parsedValue != floatValue)
                            {
                                property.SetValue(entry.Value, parsedValue);
                            }
                        }
                        else if (propertyValue is string stringValue)
                        {
                            string newValue = GUILayout.TextField(stringValue);
                            if (newValue != stringValue)
                            {
                                property.SetValue(entry.Value, newValue);
                            }
                        }
                        else if (propertyValue is Enum enumValue)
                        {
                            string[] enumNames = Enum.GetNames(enumValue.GetType());
                            int selectedIndex = Array.IndexOf(enumNames, enumValue.ToString());
                            int newIndex = EditorGUILayout.Popup(selectedIndex, enumNames);
                            if (newIndex != selectedIndex)
                            {
                                property.SetValue(entry.Value, Enum.Parse(enumValue.GetType(), enumNames[newIndex]));
                            }
                        }
                        else
                        {
                            string value = JsonConvert.SerializeObject(propertyValue);
                            string newValue = GUILayout.TextField(value);
                            if (newValue != value)
                            {
                                try
                                {
                                    object parsedValue = JsonConvert.DeserializeObject(newValue, propertyValue.GetType());
                                    property.SetValue(entry.Value, parsedValue);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError("Failed to parse value: " + ex.Message);
                                }
                            }
                        }

                        GUILayout.EndHorizontal();
                    }

                    // 클래스의 필드를 순회
                    foreach (var field in entryType.GetFields())
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(field.Name, GUILayout.Width(100));

                        var fieldValue = field.GetValue(entry.Value);
                        if (fieldValue is int intValue)
                        {
                            string newValue = GUILayout.TextField(intValue.ToString());
                            if (int.TryParse(newValue, out int parsedValue) && parsedValue != intValue)
                            {
                                field.SetValue(entry.Value, parsedValue);
                            }
                        }
                        else if (fieldValue is float floatValue)
                        {
                            string newValue = GUILayout.TextField(floatValue.ToString());
                            if (float.TryParse(newValue, out float parsedValue) && parsedValue != floatValue)
                            {
                                field.SetValue(entry.Value, parsedValue);
                            }
                        }
                        else if (fieldValue is string stringValue)
                        {
                            string newValue = GUILayout.TextField(stringValue);
                            if (newValue != stringValue)
                            {
                                field.SetValue(entry.Value, newValue);
                            }
                        }
                        else if (fieldValue is Enum enumValue)
                        {
                            string[] enumNames = Enum.GetNames(enumValue.GetType());
                            int selectedIndex = Array.IndexOf(enumNames, enumValue.ToString());
                            int newIndex = EditorGUILayout.Popup(selectedIndex, enumNames);
                            if (newIndex != selectedIndex)
                            {
                                field.SetValue(entry.Value, Enum.Parse(enumValue.GetType(), enumNames[newIndex]));
                            }
                        }
                        else
                        {
                            string value = JsonConvert.SerializeObject(fieldValue);
                            string newValue = GUILayout.TextField(value);
                            if (newValue != value)
                            {
                                try
                                {
                                    object parsedValue = JsonConvert.DeserializeObject(newValue, fieldValue.GetType());
                                    field.SetValue(entry.Value, parsedValue);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError("Failed to parse value: " + ex.Message);
                                }
                            }
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();
                }
                else
                {
                    // 기존 단일 타입 처리 로직 유지
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

            if (!string.IsNullOrEmpty(selectedCategory))
            {
                var categoryType = dataManager.GetSupportedDataType(selectedCategory);
                if (categoryType != null)
                {
                    if (cachedNewItem == null || cachedNewItem.GetType() != categoryType)
                    {
                        cachedNewItem = Activator.CreateInstance(categoryType);
                    }
                    var newItem = cachedNewItem;

                    foreach (var property in categoryType.GetProperties())
                    {
                        if (!property.CanWrite || property.Name == "Id") continue; // Skip Id property

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(property.Name, GUILayout.Width(100));

                        if (property.PropertyType == typeof(int))
                        {
                            string input = GUILayout.TextField(property.GetValue(newItem)?.ToString() ?? "0");
                            if (int.TryParse(input, out int value))
                            {
                                property.SetValue(newItem, value);
                            }
                        }
                        else if (property.PropertyType == typeof(string))
                        {
                            string tempInput = property.GetValue(newItem)?.ToString() ?? string.Empty;
                            string input = GUILayout.TextField(tempInput);
                            if (input != tempInput)
                            {
                                property.SetValue(newItem, input);
                            }
                        }
                        else if (property.PropertyType.IsEnum)
                        {
                            string[] enumNames = Enum.GetNames(property.PropertyType);
                            int selectedIndex = Array.IndexOf(enumNames, property.GetValue(newItem)?.ToString() ?? enumNames[0]);
                            int newIndex = EditorGUILayout.Popup(selectedIndex, enumNames);
                            property.SetValue(newItem, Enum.Parse(property.PropertyType, enumNames[newIndex]));
                        }
                        // Add more type handling as needed

                        GUILayout.EndHorizontal();
                    }

                    if (GUILayout.Button("Add Item"))
                    {
                        int newId = currentData.Keys.Count > 0 ? currentData.Keys.Max() + 1 : 1;
                        var idProperty = categoryType.GetProperty("Id");
                        if (idProperty != null && idProperty.CanWrite)
                        {
                            idProperty.SetValue(newItem, newId);
                        }
                        currentData[newId] = newItem;
                    }
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