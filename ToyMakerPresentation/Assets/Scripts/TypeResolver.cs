using System;
using System.Collections.Generic;
using UnityEngine;

public class TypeResolver
{
    private readonly Dictionary<string, Type> _typeCache = new();
    private List<string> _targetNamespaces;

    public TypeResolver(List<string> targetNamespaces)
    {
        _targetNamespaces = targetNamespaces ?? new List<string>();
    }

    public void SetTargetNamespaces(List<string> namespaces)
    {
        _targetNamespaces = namespaces ?? new List<string>();
        _typeCache.Clear();
    }

    public List<string> GetTargetNamespaces() => new List<string>(_targetNamespaces);

    public Type GetSupportedType(string typeString)
    {
        try
        {
            if (string.IsNullOrEmpty(typeString))
            {
                Debug.LogWarning("TypeResolver: 빈 타입 문자열이 전달되었습니다.");
                return null;
            }
            if (_typeCache.TryGetValue(typeString, out var cachedType))
                return cachedType;

            // 네임스페이스가 포함된 경우
            if (typeString.Contains("."))
            {
                var directType = Type.GetType(typeString);
                if (directType != null)
                {
                    _typeCache[typeString] = directType;
                    return directType;
                }
                var parts = typeString.Split('.');
                var typeName = parts[^1];
                if (_typeCache.TryGetValue(typeName, out var typeByName))
                {
                    _typeCache[typeString] = typeByName;
                    return typeByName;
                }
            }

            // 타겟 네임스페이스 우선 검색
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (_targetNamespaces.Contains(type.Namespace) && type.Name == typeString)
                        {
                            _typeCache[typeString] = type;
                            return type;
                        }
                    }
                }
                catch { }
            }

            // 모든 어셈블리에서 이름 일치 검색
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == typeString)
                        {
                            _typeCache[typeString] = type;
                            return type;
                        }
                    }
                }
                catch { }
            }

            Debug.LogWarning($"TypeResolver: 타입 '{typeString}'을 찾을 수 없습니다.");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"TypeResolver: GetSupportedType 오류 - {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    public IReadOnlyList<Type> GetAvailableTypes()
    {
        var result = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (_targetNamespaces.Contains(type.Namespace) && (type.IsClass || type.IsValueType) && !type.IsAbstract)
                    {
                        result.Add(type);
                    }
                }
            }
            catch { }
        }
        return result;
    }
}
