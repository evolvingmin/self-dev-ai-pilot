using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Linq;

public class DataManager
{
    private Dictionary<string, Dictionary<int, object>> dataStore = new ();

    private Dictionary<string, Type> supportedTypes = new ();

    private List<string> targetNamespaces = new List<string> { "ToyProject.Data" };

    public DataManager() : this(new List<string> { "ToyProject.Data" }) { }

    public DataManager(List<string> namespaces)
    {
        targetNamespaces = namespaces ?? new List<string> { "ToyProject.Data" };
        LoadSupportedDataTypes();
    }

    private string ResolveFilePath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    public void SaveData(string fileName)
    {
        string filePath = fileName;
        if (!Path.IsPathRooted(fileName))
        {
            filePath = ResolveFilePath(fileName);
        }
        string json = JsonConvert.SerializeObject(dataStore, Formatting.Indented);
        File.WriteAllText(filePath, json, new UTF8Encoding(true));
        Debug.Log($"DataManager: 데이터 저장 완료 - {filePath}");
    }

    public void LoadData(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("DataManager: 파일 이름이 비어 있습니다.");
            return;
        }
            
        try
        {
            string filePath = fileName;
            if (!Path.IsPathRooted(fileName))
            {
                filePath = ResolveFilePath(fileName);
            }
            
            Debug.Log($"DataManager: 파일 로드 시도 - {filePath}");
            
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"DataManager: 파일이 존재하지 않음 - {filePath}");
                return;
            }
            
            string json = null;
            try
            {
                json = File.ReadAllText(filePath, new UTF8Encoding(true));
                Debug.Log($"DataManager: JSON 파일 크기 - {json.Length} 바이트");
            }
            catch (Exception fileEx)
            {
                Debug.LogError($"DataManager: 파일 읽기 오류 - {fileEx.Message}");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("DataManager: 파일이 비어 있습니다. 기본 빈 데이터를 생성합니다.");
                dataStore.Clear();
                return;
            }
            
            JObject root = null;
            try
            {
                root = JObject.Parse(json);
                if (root.Type != JTokenType.Object)
                {
                    Debug.LogError("DataManager: JSON 파일의 최상위 구조가 객체({})가 아닙니다. 지원하지 않는 형식입니다.");
                    return;
                }
                Debug.Log($"DataManager: JSON 파싱 성공 - 최상위 속성 수: {root.Count}");
            }
            catch (JsonException jex)
            {
                Debug.LogError($"DataManager: JSON 파싱 오류 - {jex.Message}");
                return;
            }
            
            // 파싱에 성공했으면 기존 데이터 지우기
            dataStore.Clear();
            
            // 각 카테고리 처리
            foreach (var category in root)
            {
                try 
                {
                    string typeName = category.Key;
                    Debug.Log($"DataManager: 카테고리 '{typeName}' 처리 시작");
                    
                    // 타입 확인
                    Type targetType = GetSupportedDataType(typeName);
                    if (targetType == null)
                    {
                        Debug.LogWarning($"DataManager: 카테고리 '{typeName}'에 대한 타입을 찾을 수 없습니다.");
                        continue;
                    }
                    
                    Dictionary<int, object> categoryDict = new Dictionary<int, object>();
                    
                    // 카테고리가 객체인지 확인
                    if (category.Value is JObject objItems)
                    {
                        foreach (var item in objItems)
                        {
                            try
                            {
                                if (!int.TryParse(item.Key, out int id))
                                {
                                    Debug.LogWarning($"DataManager: '{typeName}'의 키 '{item.Key}'가 유효한 ID가 아닙니다.");
                                    continue;
                                }
                                
                                object parsed = item.Value.ToObject(targetType);
                                if (parsed != null)
                                {
                                    // ID 중복 체크
                                    if (categoryDict.ContainsKey(id))
                                    {
                                        Debug.LogWarning($"DataManager: '{typeName}'에 중복된 ID {id}가 있습니다. 덮어씁니다.");
                                    }
                                    
                                    categoryDict[id] = parsed;
                                    Debug.Log($"DataManager: '{typeName}'에 ID {id} 항목 추가됨");
                                }
                            }
                            catch (Exception itemEx)
                            {
                                Debug.LogError($"DataManager: 항목 파싱 오류 - {itemEx.Message}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"DataManager: 카테고리 '{typeName}'가 객체 형식이 아닙니다.");
                        continue;
                    }
                    
                    // 카테고리에 항목이 있는 경우에만 추가
                    if (categoryDict.Count > 0)
                    {
                        dataStore[typeName] = categoryDict;
                        Debug.Log($"DataManager: 카테고리 '{typeName}'에 {categoryDict.Count}개 항목 로드 완료");
                    }
                    else
                    {
                        Debug.LogWarning($"DataManager: 카테고리 '{typeName}'에는 유효한 항목이 없습니다.");
                    }
                }
                catch (Exception catEx)
                {
                    Debug.LogError($"DataManager: 카테고리 처리 오류 - {catEx.Message}");
                }
            }
            
            Debug.Log($"DataManager: 데이터 로드 완료 - 카테고리: {string.Join(", ", dataStore.Keys)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"DataManager: 데이터 로드 중 오류 발생 - {ex.Message}\n{ex.StackTrace}");
        }
    }

    public T GetData<T>(int key) where T : class
    {
        if (dataStore.TryGetValue(typeof(T).Name, out var categoryData) && categoryData.TryGetValue(key, out var item))
        {
            return item as T;
        }

        Debug.LogWarning($"DataManager: 카테고리 '{typeof(T).Name}', 키 '{key}'에 대한 데이터를 찾을 수 없습니다.");
        return null;
    }

    public void SetData(string category, Dictionary<int, object> data)
    {
        if (dataStore.ContainsKey(category))
        {
            Debug.LogWarning($"DataManager: 카테고리 '{category}'가 이미 존재합니다. 데이터를 덮어씁니다.");
        }
        dataStore[category] = data;
    }

    public IEnumerable<string> GetDataKeys()
    {
        return dataStore.Keys;
    }

    // 네임스페이스 관리용 메서드
    public void SetTargetNamespaces(List<string> namespaces)
    {
        targetNamespaces = namespaces ?? new List<string> { "ToyProject.Data" };
        LoadSupportedDataTypes();
    }

    public List<string> GetTargetNamespaces() => new List<string>(targetNamespaces);

    public void AddTargetNamespace(string ns)
    {
        if (!targetNamespaces.Contains(ns))
        {
            targetNamespaces.Add(ns);
            LoadSupportedDataTypes();
        }
    }

    public void RemoveTargetNamespace(string ns)
    {
        if (targetNamespaces.Remove(ns))
        {
            LoadSupportedDataTypes();
        }
    }

    private void LoadSupportedDataTypes()
    {
        supportedTypes.Clear();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Namespace != null && targetNamespaces.Contains(type.Namespace) && (type.IsClass || type.IsValueType) && !type.IsAbstract)
                    {
                        supportedTypes[type.Name] = type;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"DataManager: 어셈블리 {assembly.FullName}에서 타입 로딩 오류 - {ex.Message}");
            }
        }
        Debug.Log($"DataManager: {supportedTypes.Count}개의 지원되는 데이터 타입 로드됨:");
        foreach (var type in supportedTypes)
        {
            Debug.Log($"- {type.Key}: {type.Value.FullName}");
        }
    }

   public Type GetSupportedDataType(string typeString)
    {
        try
        {
            // 1. 예외 처리 추가
            if (string.IsNullOrEmpty(typeString))
            {
                Debug.LogWarning("DataManager: 빈 타입 문자열이 전달되었습니다.");
                return null;
            }
            
            // 2. 캐시된 supportedTypes에서 먼저 검색
            if (supportedTypes.TryGetValue(typeString, out Type cachedType))
            {
                return cachedType;
            }
            
            // 3. 네임스페이스가 포함된 경우 처리
            if (typeString.Contains("."))
            {
                // 전체 타입 이름으로 직접 찾기
                Type directType = Type.GetType(typeString);
                if (directType != null)
                {
                    supportedTypes[typeString] = directType; // 캐시에 추가
                    return directType;
                }
                
                // 마지막 부분만 추출하여 다시 시도
                string[] parts = typeString.Split('.');
                string typeName = parts[parts.Length - 1];
                if (supportedTypes.TryGetValue(typeName, out Type typeByName))
                {
                    supportedTypes[typeString] = typeByName; // 캐시에 추가
                    return typeByName;
                }
            }
            
            // 4. 모든 어셈블리의 모든 타입 중에서 이름이 일치하는 것 찾기
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == typeString)
                        {
                            Debug.Log($"타입을 찾았습니다: {type.FullName}");
                            supportedTypes[typeString] = type;
                            return type;
                        }
                    }
                }
                catch
                {
                    // 어셈블리 검색 오류 무시
                }
            }
            
            // 6. 타입을 찾을 수 없는 경우
            Debug.LogWarning($"DataManager: 타입 '{typeString}'을 찾을 수 없습니다.");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"DataManager: GetSupportedDataType 오류 - {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }


    public Dictionary<int, object> GetDataForEditing(string category)
    {
        if (dataStore.TryGetValue(category, out var categoryData))
        {
            return categoryData; // 기존 데이터를 직접 반환하여 수정 사항이 반영되도록 변경
        }

        Debug.LogWarning($"DataManager: 데이터 스토어에서 카테고리 '{category}'를 찾을 수 없습니다.");
        return null;
    }
}