using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// ToyMaker 작업 로그 검색 결과 창
/// </summary>
public class SearchResultsWindow : EditorWindow
{
    private List<ToyMakerWorkspaceWindow.SearchResult> searchResults;
    private string searchQuery;
    private Vector2 scrollPosition;
    private ToyMakerWorkspaceWindow parentWindow;
    
    public static void OpenWindow(List<ToyMakerWorkspaceWindow.SearchResult> results, string query, ToyMakerWorkspaceWindow parent)
    {
        var window = GetWindow<SearchResultsWindow>("검색 결과");
        window.searchResults = results;
        window.searchQuery = query;
        window.parentWindow = parent;
        window.minSize = new Vector2(500, 300);
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"'{searchQuery}' 검색 결과 ({searchResults.Count}건)", EditorStyles.boldLabel);
        GUILayout.EndVertical();
        
        if (searchResults.Count == 0)
        {
            EditorGUILayout.HelpBox("검색 결과가 없습니다.", MessageType.Info);
            return;
        }
        
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < searchResults.Count; i++)
        {
            var result = searchResults[i];
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 로그 날짜 및 제목 - 클릭 가능한 헤더
            EditorGUILayout.BeginHorizontal();
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize += 1;
            titleStyle.normal.textColor = new Color(0.2f, 0.5f, 0.9f);
            
            if (GUILayout.Button(result.logItem.title, titleStyle))
            {
                // 해당 로그로 이동
                parentWindow.OpenLogFromPath(result.logItem.path);
                
                // 내용 검색의 경우 하이라이트를 위한 정보 전달
                if (result.matchType == ToyMakerWorkspaceWindow.SearchMatchType.Content)
                {
                    parentWindow.HighlightSearchMatch(result.matchPosition, searchQuery.Length);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 매치 타입 정보
            string matchTypeText = result.matchType == ToyMakerWorkspaceWindow.SearchMatchType.Title ? 
                "제목 일치" : "내용 일치";
            GUIStyle matchTypeStyle = new GUIStyle(EditorStyles.miniLabel);
            matchTypeStyle.normal.textColor = result.matchType == ToyMakerWorkspaceWindow.SearchMatchType.Title ? 
                new Color(0.0f, 0.6f, 0.0f) : new Color(0.8f, 0.4f, 0.0f);
            
            EditorGUILayout.LabelField(matchTypeText, matchTypeStyle);
            
            // 매치된 텍스트 표시 (하이라이트 적용)
            if (!string.IsNullOrEmpty(result.matchText))
            {
                EditorGUILayout.BeginVertical(EditorStyles.textArea);
                
                // 하이라이트된 텍스트 렌더링을 위한 리치 텍스트 스타일 생성
                GUIStyle richTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                richTextStyle.richText = true;
                richTextStyle.wordWrap = true;
                
                // 매치된 텍스트에서 검색어를 하이라이트
                string highlightedText = HighlightSearchQuery(result.matchText, searchQuery);
                EditorGUILayout.LabelField(highlightedText, richTextStyle);
                
                EditorGUILayout.EndVertical();
            }
            
            // 로그 바로 열기 버튼
            if (GUILayout.Button("이 로그 열기", GUILayout.Height(25)))
            {
                parentWindow.OpenLogFromPath(result.logItem.path);
                
                // 내용 검색의 경우 하이라이트를 위한 정보 전달
                if (result.matchType == ToyMakerWorkspaceWindow.SearchMatchType.Content)
                {
                    parentWindow.HighlightSearchMatch(result.matchPosition, searchQuery.Length);
                }
                
                // 창 닫기
                Close();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    // 검색어를 하이라이트하는 헬퍼 메서드
    private string HighlightSearchQuery(string text, string query)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
            return text;
        
        try
        {
            // 대소문자 구분 없이 검색어 찾기 (정규식이 아닌 원시 문자열 검색 사용)
            string pattern = Regex.Escape(query);
            string replacement = $"<color=#FF6000FF><b>{query}</b></color>";
            
            // 정규식을 사용하여 대소문자 구분 없이 검색어 하이라이트
            return Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase);
        }
        catch (Exception ex)
        {
            Debug.LogError($"검색어 하이라이트 중 오류 발생: {ex.Message}");
            return text;
        }
    }
}
