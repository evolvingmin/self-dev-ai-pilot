using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using ToyProject.Data;

public class DataManagerEditor : EditorWindow
{
    private DataManager dataManager;
    private EditorState state;

    private enum Tab { Data, Settings }
    private Tab currentTab = Tab.Data;

    private const float ButtonWidth = 110f;
    private const float FieldWidth = 220f;
    private const float LabelWidth = 100f;

    private class EditorState
    {
        public string selectedJsonFile = "";
        public string selectedCategory = "";
        public string newCategory = "";
        public string searchQuery = "";
        public string newCategoryTypeSearch = "";
        public int selectedTypeIndex = 0;
        public List<Type> availableTypes = new List<Type>();
        public List<string> availableTypeNames = new List<string>();
        public Dictionary<int, object> currentData;
        public object cachedNewItem;
        public Vector2 scrollPosition;
        public string newNamespaceInput = "";
    }

    [MenuItem("Window/Data Manager Editor")]
    public static void ShowWindow()
    {
        GetWindow<DataManagerEditor>("Data Manager Editor");
    }

    private void OnEnable()
    {
        dataManager = new DataManager();
        state = new EditorState();
        RefreshTypeList();
    }

    private void RefreshTypeList()
    {
        state.availableTypes = dataManager.AvailableTypes.ToList();
        state.availableTypeNames = state.availableTypes.Select(t => t.Name).ToList();
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(8);
        if (GUILayout.Toggle(currentTab == Tab.Data, "Data Manager", EditorStyles.toolbarButton, GUILayout.Width(ButtonWidth + 30))) currentTab = Tab.Data;
        GUILayout.Space(4);
        if (GUILayout.Toggle(currentTab == Tab.Settings, "Settings", EditorStyles.toolbarButton, GUILayout.Width(ButtonWidth + 30))) currentTab = Tab.Settings;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
        if (currentTab == Tab.Data)
        {
            DrawDataManagerTab(state);
        }
        else if (currentTab == Tab.Settings)
        {
            DrawSettingsTab(state);
        }
    }

    private void DrawDataManagerTab(EditorState state)
    {
        GUILayout.Label("Data Manager", EditorStyles.boldLabel);
        GUILayout.Space(8);
        // 파일 선택 영역
        DrawSectionSeparator();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Select JSON File", EditorStyles.boldLabel, GUILayout.Width(LabelWidth + 30));
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f, 1f);
        if (GUILayout.Button("Select JSON File", GUILayout.Width(ButtonWidth), GUILayout.Height(24)))
        {
            SelectJsonFile();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.Space(4);
        if (!string.IsNullOrEmpty(state.selectedJsonFile))
        {
            if (GUILayout.Button("Show in Explorer", GUILayout.Width(ButtonWidth)))
            {
                EditorUtility.RevealInFinder(state.selectedJsonFile);
            }
        }
        GUILayout.EndHorizontal();
        // 파일 경로 표시
        GUILayout.BeginHorizontal();
        GUILayout.Label("Selected File:", GUILayout.Width(LabelWidth));
        if (!string.IsNullOrEmpty(state.selectedJsonFile))
        {
            GUIStyle boldStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            GUILayout.Label(Path.GetFileName(state.selectedJsonFile), boldStyle, GUILayout.Width(FieldWidth));
        }
        else
        {
            GUILayout.Label("(No file selected)", GUILayout.Width(FieldWidth));
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
        // 카테고리 리스트
        DrawSectionSeparator();
        GUILayout.Label("Categories", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        foreach (var category in dataManager.GetDataKeys())
        {
            if (GUILayout.Button(category, GUILayout.Width(ButtonWidth)))
            {
                SelectCategory(category);
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
        DrawSectionSeparator();
        DrawNewCategorySection(state);
        GUILayout.Space(8);
        if (!string.IsNullOrEmpty(state.selectedCategory) && state.currentData != null)
        {
            DrawSectionSeparator();
            GUILayout.Label($"Editing Category: {state.selectedCategory}", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search Items", GUILayout.Width(LabelWidth));
            state.searchQuery = GUILayout.TextField(state.searchQuery, GUILayout.Width(FieldWidth));
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            DrawFilteredData(state);
            GUILayout.Space(8);
            DrawSectionSeparator();
            GUILayout.Label("Add New Item", EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(state.selectedCategory))
            {
                DrawNewItemFields(state);
            }
            GUILayout.Space(4);
            if (GUILayout.Button("Save Data", GUILayout.Width(ButtonWidth + 20)))
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
            state.selectedJsonFile = path;
            Debug.Log("Selected JSON File: " + state.selectedJsonFile);
            dataManager.LoadData(state.selectedJsonFile);
        }
    }

    private void SelectCategory(string category)
    {
        state.selectedCategory = category;
        state.currentData = dataManager.GetDataForEditing(state.selectedCategory);
    }

    private void AddNewCategory()
    {
        if (!string.IsNullOrEmpty(state.newCategory))
        {
            dataManager.SetData(state.newCategory, new Dictionary<int, object>());
            state.newCategory = "";
        }
    }

    private void DrawNewCategorySection(EditorState state)
    {
        GUILayout.Label("Add New Category", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Type Search:", GUILayout.Width(LabelWidth));
        state.newCategoryTypeSearch = GUILayout.TextField(state.newCategoryTypeSearch, GUILayout.Width(FieldWidth));
        GUILayout.EndHorizontal();
        var filteredTypes = string.IsNullOrEmpty(state.newCategoryTypeSearch)
            ? state.availableTypes
            : state.availableTypes.Where(t => t.Name.IndexOf(state.newCategoryTypeSearch, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        var filteredTypeNames = filteredTypes.Select(t => t.Name).ToArray();
        if (filteredTypeNames.Length == 0)
        {
            GUILayout.Label("No matching types.", EditorStyles.helpBox);
            return;
        }
        state.selectedTypeIndex = Mathf.Clamp(state.selectedTypeIndex, 0, filteredTypes.Count - 1);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Data Type", GUILayout.Width(LabelWidth));
        state.selectedTypeIndex = EditorGUILayout.Popup(state.selectedTypeIndex, filteredTypeNames, GUILayout.Width(FieldWidth));
        GUILayout.EndHorizontal();
        var selectedType = filteredTypes[state.selectedTypeIndex];
        string categoryName = selectedType.Name;

        GUI.enabled = !dataManager.GetDataKeys().Contains(categoryName);
        if (GUILayout.Button("Generate", GUILayout.Width(ButtonWidth)))
        {
            dataManager.SetData(categoryName, new Dictionary<int, object>());
            state.selectedCategory = categoryName;
            state.currentData = dataManager.GetDataForEditing(state.selectedCategory);
            state.cachedNewItem = null;
            state.newCategoryTypeSearch = "";
        }
        GUI.enabled = true;
    }

    private void DrawFilteredData(EditorState state)
    {
        // 카드형 그리드 레이아웃: 한 줄에 3개씩
        int cardsPerRow = 3;
        int cardCount = 0;
        IEnumerable<KeyValuePair<int, object>> filteredData = state.currentData;
        if (!string.IsNullOrEmpty(state.searchQuery))
        {
            filteredData = FilterDataBySearchQuery(state);
        }
        state.scrollPosition = GUILayout.BeginScrollView(state.scrollPosition, GUILayout.Height(320));
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        foreach (var entry in filteredData.ToList())
        {
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(220), GUILayout.Height(100));
            GUILayout.Label($"ID: {entry.Key}", EditorStyles.boldLabel);
            DrawEntryFields(entry, state);
            GUILayout.EndVertical();
            cardCount++;
            if (cardCount % cardsPerRow == 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }

    private IEnumerable<KeyValuePair<int, object>> FilterDataBySearchQuery(EditorState state)
    {
        return state.currentData.Where(entry =>
        {
            var entryType = entry.Value.GetType();
            foreach (var property in entryType.GetProperties())
            {
                if (property.CanRead && property.GetValue(entry.Value)?.ToString().Contains(state.searchQuery, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }
            }
            return false;
        });
    }

    private void DrawDataEntry(KeyValuePair<int, object> entry, List<int> keysToRemove, EditorState state)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"ID: {entry.Key}", GUILayout.Width(LabelWidth));
        DrawEntryFields(entry, state);
        if (GUILayout.Button("Delete", GUILayout.Width(ButtonWidth)))
        {
            keysToRemove.Add(entry.Key);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(6);
    }

    private void DrawEntryFields(KeyValuePair<int, object> entry, EditorState state)
    {
        var entryType = entry.Value.GetType();
        if (entryType.IsClass || entryType.IsValueType && !entryType.IsPrimitive)
        {
            GUILayout.BeginVertical();
            foreach (var property in entryType.GetProperties())
            {
                if (!property.CanWrite || property.Name == "Id") continue;

                var propertyValue = property.GetValue(entry.Value)?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(state.searchQuery) && propertyValue.Contains(state.searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    GUI.backgroundColor = Color.yellow;
                }
                else
                {
                    GUI.backgroundColor = Color.white;
                }

                DrawField(property.Name, entry.Value, property);
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndVertical();
        }
        else
        {
            DrawPrimitiveField(entry, state);
        }
    }

    private void DrawField(string label, object target, System.Reflection.PropertyInfo property)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(LabelWidth));
        var propertyValue = property.GetValue(target);
        if (property.PropertyType == typeof(int))
        {
            string input = GUILayout.TextField(propertyValue?.ToString() ?? "0", GUILayout.Width(FieldWidth));
            if (int.TryParse(input, out int value))
            {
                property.SetValue(target, value);
            }
        }
        else if (property.PropertyType == typeof(float))
        {
            string input = GUILayout.TextField(propertyValue?.ToString() ?? "0.0", GUILayout.Width(FieldWidth));
            if (float.TryParse(input, out float value))
            {
                property.SetValue(target, value);
            }
        }
        else if (property.PropertyType == typeof(string))
        {
            string input = GUILayout.TextField(propertyValue?.ToString() ?? string.Empty, GUILayout.Width(FieldWidth));
            if (input != propertyValue?.ToString())
            {
                property.SetValue(target, input);
            }
        }
        else if (property.PropertyType.IsEnum)
        {
            string[] enumNames = Enum.GetNames(property.PropertyType);
            int selectedIndex = Array.IndexOf(enumNames, propertyValue?.ToString() ?? enumNames[0]);
            int newIndex = EditorGUILayout.Popup(selectedIndex, enumNames, GUILayout.Width(FieldWidth));
            if (newIndex != selectedIndex)
            {
                property.SetValue(target, Enum.Parse(property.PropertyType, enumNames[newIndex]));
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DrawPrimitiveField(KeyValuePair<int, object> entry, EditorState state)
    {
        if (entry.Value is int intValue)
        {
            string newValue = GUILayout.TextField(intValue.ToString(), GUILayout.Width(FieldWidth));
            if (int.TryParse(newValue, out int parsedValue) && parsedValue != intValue)
            {
                state.currentData[entry.Key] = parsedValue;
            }
        }
        else if (entry.Value is float floatValue)
        {
            string newValue = GUILayout.TextField(floatValue.ToString(), GUILayout.Width(FieldWidth));
            if (float.TryParse(newValue, out float parsedValue) && parsedValue != floatValue)
            {
                state.currentData[entry.Key] = parsedValue;
            }
        }
        else if (entry.Value is string stringValue)
        {
            string newValue = GUILayout.TextField(stringValue, GUILayout.Width(FieldWidth));
            if (newValue != stringValue)
            {
                state.currentData[entry.Key] = newValue;
            }
        }
        else if (entry.Value is Enum enumValue)
        {
            string[] enumNames = Enum.GetNames(enumValue.GetType());
            int selectedIndex = Array.IndexOf(enumNames, enumValue.ToString());
            int newIndex = EditorGUILayout.Popup(selectedIndex, enumNames, GUILayout.Width(FieldWidth));
            if (newIndex != selectedIndex)
            {
                state.currentData[entry.Key] = Enum.Parse(enumValue.GetType(), enumNames[newIndex]);
            }
        }
        else
        {
            string value = JsonConvert.SerializeObject(entry.Value);
            string newValue = GUILayout.TextField(value, GUILayout.Width(FieldWidth));
            if (newValue != value)
            {
                try
                {
                    object parsedValue = JsonConvert.DeserializeObject(newValue, entry.Value.GetType());
                    state.currentData[entry.Key] = parsedValue;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to parse value: " + ex.Message);
                }
            }
        }
    }

    private void DrawNewItemFields(EditorState state)
    {
        var categoryType = dataManager.GetSupportedDataType(state.selectedCategory);
        if (categoryType != null)
        {
            if (state.cachedNewItem == null || state.cachedNewItem.GetType() != categoryType)
            {
                state.cachedNewItem = CreateInstanceSafe(categoryType);
            }
            var newItem = state.cachedNewItem;
            if (newItem == null)
            {
                GUILayout.Label($"{categoryType.Name} 인스턴스를 생성할 수 없습니다.", EditorStyles.helpBox);
                return;
            }
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(220));
            DrawFieldsForObject(newItem);
            GUILayout.EndVertical();
            GUILayout.Space(8);
            if (GUILayout.Button("새 항목", GUILayout.Width(ButtonWidth), GUILayout.Height(32)))
            {
                AddNewItem(newItem, categoryType);
            }
            GUILayout.EndHorizontal();
        }
    }

    private void AddNewItem(object newItem, Type categoryType)
    {
        int newId = state.currentData.Keys.Count > 0 ? state.currentData.Keys.Max() + 1 : 1;
        var idProperty = categoryType.GetProperty("Id");
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(newItem, newId);
        }
        state.currentData[newId] = newItem;
    }

    private void SaveData()
    {
        dataManager.SaveData(state.selectedJsonFile);
    }

    private void DrawSettingsTab(EditorState state)
    {
        GUILayout.Label("Namespace Settings", EditorStyles.boldLabel);
        GUILayout.Space(6);
        var nsList = dataManager.GetTargetNamespaces();
        for (int i = 0; i < nsList.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(nsList[i], EditorStyles.textField, GUILayout.Width(FieldWidth + 40));
            if (GUILayout.Button("Remove", GUILayout.Width(ButtonWidth)))
            {
                dataManager.RemoveTargetNamespace(nsList[i]);
                RefreshTypeList();
                GUI.FocusControl(null);
                break;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        state.newNamespaceInput = GUILayout.TextField(state.newNamespaceInput, GUILayout.Width(FieldWidth));
        GUI.enabled = !string.IsNullOrWhiteSpace(state.newNamespaceInput) && !nsList.Contains(state.newNamespaceInput);
        if (GUILayout.Button("Add Namespace", GUILayout.Width(ButtonWidth + 10)))
        {
            dataManager.AddTargetNamespace(state.newNamespaceInput.Trim());
            RefreshTypeList();
            state.newNamespaceInput = "";
            GUI.FocusControl(null);
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
        if (GUILayout.Button("Reload Types (Apply)", GUILayout.Width(ButtonWidth + 40)))
        {
            dataManager.SetTargetNamespaces(dataManager.GetTargetNamespaces());
            RefreshTypeList();
            GUI.FocusControl(null);
        }
        GUILayout.Space(8);
        GUILayout.Label("현재 네임스페이스에 포함된 타입만 데이터 타입 선택에 노출됩니다.", EditorStyles.helpBox);
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

    private void DrawFieldsForObject(object target)
    {
        var type = target.GetType();
        foreach (var property in type.GetProperties())
        {
            if (!property.CanWrite || property.Name == "Id") continue;
            DrawField(property.Name, target, property);
        }
    }

    private void DrawSectionSeparator()
    {
        GUILayout.Space(6);
        var rect = GUILayoutUtility.GetRect(1, 2, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f, 1f));
        GUILayout.Space(6);
    }
}