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

    private string newCategoryTypeSearch = "";
    private int selectedTypeIndex = 0;
    private List<Type> availableTypes = new List<Type>();
    private List<string> availableTypeNames = new List<string>();

    private enum Tab { Data, Settings }
    private Tab currentTab = Tab.Data;
    private string newNamespaceInput = "";

    [MenuItem("Window/Data Manager Editor")]
    public static void ShowWindow()
    {
        GetWindow<DataManagerEditor>("Data Manager Editor");
    }

    private void OnEnable()
    {
        dataManager = new DataManager();
        availableTypes = dataManager.AvailableTypes.ToList();
        availableTypeNames = availableTypes.Select(t => t.Name).ToList();
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == Tab.Data, "Data Manager", EditorStyles.toolbarButton)) currentTab = Tab.Data;
        if (GUILayout.Toggle(currentTab == Tab.Settings, "Settings", EditorStyles.toolbarButton)) currentTab = Tab.Settings;
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
        if (currentTab == Tab.Data)
        {
            DrawDataManagerTab();
        }
        else if (currentTab == Tab.Settings)
        {
            DrawSettingsTab();
        }
    }

    private void DrawDataManagerTab()
    {
        GUILayout.Label("Data Manager", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 파일 선택 영역 개선
        GUILayout.BeginHorizontal();
        GUILayout.Label("Select JSON File", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f, 1f); // 파란색 강조
        if (GUILayout.Button("Select JSON File", GUILayout.Width(160), GUILayout.Height(28)))
        {
            SelectJsonFile();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // 파일 경로 표시 (길면 축약)
        string fileLabel = string.IsNullOrEmpty(selectedJsonFile) ? "(No file selected)" : Path.GetFileName(selectedJsonFile);
        string fullPath = selectedJsonFile;
        if (!string.IsNullOrEmpty(selectedJsonFile) && selectedJsonFile.Length > 60)
        {
            int keep = 25;
            fileLabel = selectedJsonFile.Substring(0, keep) + "..." + selectedJsonFile.Substring(selectedJsonFile.Length - keep);
        }
        GUILayout.BeginHorizontal();
        GUILayout.Label("Selected File:", GUILayout.Width(90));
        if (!string.IsNullOrEmpty(selectedJsonFile))
        {
            GUIStyle boldStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            GUILayout.Label(fileLabel, boldStyle);
            if (GUILayout.Button("Show in Explorer", GUILayout.Width(120)))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
        }
        else
        {
            GUILayout.Label("(No file selected)");
        }
        GUILayout.EndHorizontal();

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

        DrawNewCategorySection();

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

    private void DrawNewCategorySection()
    {
        GUILayout.Label("Add New Category", EditorStyles.boldLabel);
        // 타입 검색 필드
        GUILayout.BeginHorizontal();
        GUILayout.Label("Type Search:", GUILayout.Width(80));
        newCategoryTypeSearch = GUILayout.TextField(newCategoryTypeSearch);
        GUILayout.EndHorizontal();
        // 타입 리스트 필터링
        var filteredTypes = string.IsNullOrEmpty(newCategoryTypeSearch)
            ? availableTypes
            : availableTypes.Where(t => t.Name.IndexOf(newCategoryTypeSearch, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        var filteredTypeNames = filteredTypes.Select(t => t.Name).ToArray();
        if (filteredTypeNames.Length == 0)
        {
            GUILayout.Label("No matching types.", EditorStyles.helpBox);
            return;
        }
        // 타입 드롭다운
        selectedTypeIndex = Mathf.Clamp(selectedTypeIndex, 0, filteredTypes.Count - 1);
        selectedTypeIndex = EditorGUILayout.Popup("Data Type", selectedTypeIndex, filteredTypeNames);
        var selectedType = filteredTypes[selectedTypeIndex];
        string categoryName = selectedType.Name;
        GUILayout.BeginHorizontal();
        GUILayout.Label("Category Name:", GUILayout.Width(100));
        GUILayout.Label(categoryName, EditorStyles.boldLabel);
        GUILayout.EndHorizontal();
        // 생성 버튼
        GUI.enabled = !dataManager.GetDataKeys().Contains(categoryName);
        if (GUILayout.Button("Generate"))
        {
            dataManager.SetData(categoryName, new Dictionary<int, object>());
            selectedCategory = categoryName;
            currentData = dataManager.GetDataForEditing(selectedCategory);
            cachedNewItem = null;
            newCategoryTypeSearch = "";
        }
        GUI.enabled = true;
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
                cachedNewItem = CreateInstanceSafe(categoryType);
            }
            var newItem = cachedNewItem;
            if (newItem == null)
            {
                GUILayout.Label($"{categoryType.Name} 인스턴스를 생성할 수 없습니다.", EditorStyles.helpBox);
                return;
            }
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

    private object CreateInstanceSafe(Type type)
    {
        try
        {
            // 기본 생성자가 있으면 사용
            return Activator.CreateInstance(type);
        }
        catch (MissingMethodException)
        {
            // 매개변수 없는 생성자가 없을 때, FormatterServices로 생성 (필드 초기화 X)
            try
            {
                return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            }
            catch (Exception ex)
            {
                Debug.LogError($"인스턴스 생성 실패: {type.FullName} - {ex.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"인스턴스 생성 실패: {type.FullName} - {ex.Message}");
            return null;
        }
    }

    private void DrawSettingsTab()
    {
        GUILayout.Label("Namespace Settings", EditorStyles.boldLabel);
        GUILayout.Space(6);
        var nsList = dataManager.GetTargetNamespaces();
        for (int i = 0; i < nsList.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(nsList[i], EditorStyles.textField);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                dataManager.RemoveTargetNamespace(nsList[i]);
                availableTypes = dataManager.AvailableTypes.ToList();
                availableTypeNames = availableTypes.Select(t => t.Name).ToList();
                GUI.FocusControl(null);
                break;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        newNamespaceInput = GUILayout.TextField(newNamespaceInput);
        GUI.enabled = !string.IsNullOrWhiteSpace(newNamespaceInput) && !nsList.Contains(newNamespaceInput);
        if (GUILayout.Button("Add Namespace", GUILayout.Width(120)))
        {
            dataManager.AddTargetNamespace(newNamespaceInput.Trim());
            availableTypes = dataManager.AvailableTypes.ToList();
            availableTypeNames = availableTypes.Select(t => t.Name).ToList();
            newNamespaceInput = "";
            GUI.FocusControl(null);
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
        if (GUILayout.Button("Reload Types (Apply)", GUILayout.Width(180)))
        {
            dataManager.SetTargetNamespaces(dataManager.GetTargetNamespaces());
            availableTypes = dataManager.AvailableTypes.ToList();
            availableTypeNames = availableTypes.Select(t => t.Name).ToList();
            GUI.FocusControl(null);
        }
        GUILayout.Space(8);
        GUILayout.Label("현재 네임스페이스에 포함된 타입만 데이터 타입 선택에 노출됩니다.", EditorStyles.helpBox);
    }
}