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
/// ToyMakerPresentation 작업 일지/진도 관리 도구
/// </summary>
public class ToyMakerWorkspaceWindow : OdinEditorWindow
{    private const string PREFS_KEY = "ToyMakerWorkspace_";
    private const string LOGS_PATH = "WorkLogs";
    
    // 상대 경로를 절대 경로로 변환하는 헬퍼 메서드
    private string GetAbsoluteLogsPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.GetFullPath(Path.Combine(projectRoot, LOGS_PATH));
    }
    
    [Serializable]
    public class WorkTask
    {
        [HorizontalGroup("Task")]
        [ToggleLeft, GUIColor("GetStatusColor")]
        public bool completed;
        
        [HorizontalGroup("Task", Width = 0.7f)]
        [LabelText("작업 내용")]
        public string description;
        
        [HorizontalGroup("Task")]
        [LabelText("기록일")]
        public string date = DateTime.Now.ToString("MM/dd");
        
        public Color GetStatusColor()
        {
            return completed ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.4f, 0.3f);
        }
    }
    
    [Serializable]
    public class WorkLog
    {
        [LabelText("기록일"), ReadOnly, HideLabel]
        [TitleGroup("$date", Alignment = TitleAlignments.Centered, HorizontalLine = true)]
        public string date = DateTime.Now.ToString("yyyy-MM-dd");
        
        [TextArea(5, 10), HideLabel]
        [TitleGroup("$date")]
        public string content = "";
    }
    
    [TabGroup("작업관리", "작업관리"), PropertyOrder(-10)]
    [HideLabel, LabelText("검색")]
    [DelayedProperty]
    public string searchQuery = "";
    
    [TabGroup("작업관리", "작업관리")]
    [TitleGroup("작업관리/오늘의 작업", Alignment = TitleAlignments.Centered)]
    [LabelText("작업 중인 항목"), PropertySpace(8)]
    [ListDrawerSettings(ShowFoldout = false, DefaultExpandedState = true, DraggableItems = true)]
    [ShowIf("ShouldShowTaskList")]
    [PropertyOrder(10)]
    public List<WorkTask> currentTasks = new List<WorkTask>();
    
    [TabGroup("작업관리", "작업관리")]
    [Button(ButtonSizes.Large), PropertyOrder(15)]
    [GUIColor(0.3f, 0.7f, 0.9f)]
    [LabelText("새 작업 추가")]
    private void AddNewTask()
    {
        currentTasks.Add(new WorkTask { description = "새 작업" });
    }
    
    [TabGroup("작업관리", "작업관리")]
    [PropertySpace(8)]
    [TitleGroup("작업관리/개인 작업 노트", Alignment = TitleAlignments.Centered)]
    [PropertyOrder(20)]
    [ListDrawerSettings(ShowFoldout = false, OnTitleBarGUI = "AddNewLogButton")]
    [ShowIf("ShouldShowLogList")]
    public List<WorkLog> workLogs = new List<WorkLog>();
    
    [TabGroup("작업관리", "작업관리")]    [Button(ButtonSizes.Medium), PropertySpace(8), PropertyOrder(30)]
    [GUIColor(0.4f, 0.5f, 0.9f)]
    [LabelText("작업로그 저장")]    private void SaveWorkLogs()
    {
        try
        {
            SaveData();
            
            // 절대 경로로 변환
            string absolutePath = GetAbsoluteLogsPath();
            
            // 디렉토리 존재 확인 및 생성
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
                AssetDatabase.Refresh();
            }
            
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd");
            string logContent = "# ToyMaker 작업로그: " + timestamp + "\n\n";
            
            logContent += "## 현재 작업 항목\n\n";
            if (currentTasks != null && currentTasks.Count > 0)
            {
                foreach (var task in currentTasks)
                {
                    logContent += $"- [{(task.completed ? "x" : " ")}] {task.description} ({task.date})\n";
                }
            }
            else
            {
                logContent += "- 현재 작업 항목이 없습니다.\n";
            }
            
            logContent += "\n## 작업 노트\n\n";
            if (workLogs != null && workLogs.Count > 0)
            {
                foreach (var log in workLogs)
                {
                    if (log != null)
                    {
                        logContent += $"### {log.date}\n\n{log.content}\n\n";
                    }
                }
            }
            else
            {
                logContent += "작업 노트가 없습니다.\n";
            }
 
            string filePath = Path.Combine(GetAbsoluteLogsPath(), $"WorkLog_{timestamp}.md");
            File.WriteAllText(filePath, logContent);
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent($"작업로그가 저장되었습니다\n{filePath}"));
            
            // 새 로그가 생성되었으므로 기록 뷰어 초기화
            InitializeLogViewer();
        }
        catch (Exception ex)
        {
            Debug.LogError($"작업로그 저장 중 오류 발생: {ex.Message}");
            EditorUtility.DisplayDialog("오류", $"작업로그 저장 중 오류가 발생했습니다.\n{ex.Message}", "확인");
        }
    }
    
    // 기록 뷰어 관련 필드
    [TabGroup("기록 뷰어", "기록 뷰어")]
    [TitleGroup("기록 뷰어/저장된 기록들", Alignment = TitleAlignments.Centered)]
    [PropertyOrder(100)]
    [ListDrawerSettings(ShowFoldout = false, DefaultExpandedState = true)]
    [HideInInspector, HideLabel]
    private List<LogViewItem> savedLogs = new List<LogViewItem>();
      [TabGroup("기록 뷰어", "기록 뷰어")]
    [LabelText("기록 선택")]
    [PropertyOrder(101)]
    [ShowInInspector]
    // ValueDropdown과 OnValueChanged 조합 대신 일반 필드로 변경하고 별도 UI 구현
    private int selectedLogIndex = -1;
    
    // 로그 경로 데이터 - 직접 UI에서 참조되지 않도록 숨김 처리
    [HideInInspector]
    private string selectedLogPath = "";
      // 커스텀 로그 선택 UI - EditorGUI.Popup 사용하여 안전하게 구현
    [TabGroup("기록 뷰어", "기록 뷰어")]
    [PropertyOrder(101)]
    [OnInspectorGUI]
    private void DrawCustomLogSelector()
    {
        try
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("로그 선택", GUILayout.Width(80));
            
            // 저장된 로그가 없으면 메시지 표시
            if (savedLogs == null || savedLogs.Count == 0)
            {
                EditorGUILayout.LabelField("저장된 작업 로그 없음");
                EditorGUILayout.EndHorizontal();
                return;
            }
            
            // 로그 목록 구성 - 배열 복사하여 안전하게 처리
            List<LogViewItem> orderedLogs;
            try 
            {
                orderedLogs = new List<LogViewItem>(savedLogs);
                orderedLogs = orderedLogs.OrderByDescending(l => l.date).ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"로그 정렬 중 오류: {ex.Message}");
                orderedLogs = new List<LogViewItem>(savedLogs); // 정렬 실패 시 원본 사용
            }
            
            string[] logTitles = new string[orderedLogs.Count];
            for (int i = 0; i < orderedLogs.Count; i++)
            {
                logTitles[i] = orderedLogs[i]?.title ?? $"로그 {i}";
            }
            
            // 현재 선택된 로그의 인덱스 찾기 (직접 루프로 구현)
            int currentIndex = -1;
            if (!string.IsNullOrEmpty(selectedLogPath))
            {
                for (int i = 0; i < orderedLogs.Count; i++)
                {
                    if (orderedLogs[i].path == selectedLogPath)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }
              // 드롭다운 구현 (EditorGUI.BeginChangeCheck 대신 이전 값 비교로 변경 감지)
            int newIndex = -1;
            
            // GUI 이벤트 최소화하여 제한된 영역에서만 FitWindowRectToScreen 호출 위험이 있는 코드 실행
            // 팝업을 직접 그리지 않고 버튼과 메뉴로 대체
            if (GUILayout.Button(currentIndex >= 0 && currentIndex < logTitles.Length ? 
                                logTitles[currentIndex] : 
                                "로그 선택", 
                                EditorStyles.popup, 
                                GUILayout.Width(300)))
            {
                try
                {
                    // 팝업 메뉴 표시 - EditorUtility.DisplayCustomMenu 사용하여 팝업 처리
                    var menu = new GenericMenu();
                    
                    for (int i = 0; i < logTitles.Length; i++)
                    {
                        int index = i; // 클로저 문제 해결을 위한 로컬 변수
                        menu.AddItem(new GUIContent(logTitles[i]), index == currentIndex, () => {
                            try
                            {
                                selectedLogPath = orderedLogs[index].path;
                                selectedLogIndex = index;
                                OnSelectedLogChanged();
                                this.Repaint(); // UI 갱신
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"로그 선택 중 오류 발생: {ex.Message}");
                            }
                        });
                    }
                    
                    menu.ShowAsContext();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"로그 선택 메뉴 표시 중 오류: {ex.Message}");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        catch (Exception ex)
        {
            Debug.LogError($"로그 선택 UI 표시 중 오류: {ex.Message}");
            EditorGUILayout.EndHorizontal(); // 수평 레이아웃 종료 보장
        }
    }
      [TabGroup("기록 뷰어", "기록 뷰어")]
    [PropertyOrder(102)]
    [ShowInInspector, ReadOnly]
    [HideLabel]
    [TextArea(20, 40)]
    private string selectedLogContent = "";
    
    [Serializable]
    private class LogViewItem
    {
        public string title;
        public string path;
        public string content;
        public DateTime date;
          public LogViewItem(string filePath)
        {
            try
            {
                path = filePath; // 원본 경로 저장
                
                // 파일 존재 여부 확인 및 내용 읽기
                if (File.Exists(filePath))
                {
                    try
                    {
                        content = File.ReadAllText(filePath);
                    }
                    catch (Exception ex)
                    {
                        content = $"(파일을 읽는 중 오류 발생: {ex.Message})";
                        Debug.LogWarning($"로그 파일 읽기 오류: {filePath}, {ex.Message}");
                    }
                }
                else
                {
                    content = "(파일을 찾을 수 없습니다)";
                    Debug.LogWarning($"로그 파일이 존재하지 않습니다: {filePath}");
                }
                
                // 안전한 파일명 추출
                string fileName = Path.GetFileName(filePath) ?? "";
                
                // 파일명에서 날짜 추출
                var match = Regex.Match(fileName, @"WorkLog_(\d{4}-\d{2}-\d{2})");
                if (match.Success)
                {
                    // 안전한 날짜 파싱
                    if (DateTime.TryParse(match.Groups[1].Value, out var fileDate))
                    {
                        date = fileDate;
                        title = $"[{fileDate.ToString("yyyy-MM-dd")}] 작업 기록";
                    }
                    else
                    {
                        // 날짜 파싱 실패시 현재 시간 사용
                        date = DateTime.Now;
                        title = fileName;
                        Debug.LogWarning($"로그 파일 날짜 파싱 실패: {fileName}");
                    }
                }
                else
                {
                    // 파일 수정 시간으로 대체
                    try
                    {
                        date = File.GetLastWriteTime(filePath);
                    }
                    catch
                    {
                        date = DateTime.Now;
                    }
                    title = Path.GetFileNameWithoutExtension(filePath) ?? "알 수 없는 로그";
                }
            }
            catch (Exception ex)
            {
                // 어떤 예외가 발생하더라도 안전하게 처리
                Debug.LogError($"로그 항목 생성 중 오류 발생: {ex.Message}");
                path = filePath;
                content = $"(파일 읽기 오류: {ex.Message})";
                date = DateTime.Now;
                title = "오류 항목";
            }
        }
    }      private void OnSelectedLogChanged()
    {
        // 이 메서드 전체를 try-catch로 감싸서 예외가 밖으로 전파되지 않도록 보호
        try
        {
            // 로그 경로가 비어있으면 내용 초기화
            if (string.IsNullOrEmpty(selectedLogPath)) 
            {
                selectedLogContent = "";
                return;
            }
            
            // 로그 목록이 초기화되었는지 확인
            if (savedLogs == null)
            {
                savedLogs = new List<LogViewItem>();
                selectedLogContent = "(로그 목록이 초기화되지 않았습니다)";
                return;
            }
            
            // Unity의 기본 UI에서는 다른 스레드의 예외가 캐치되지 않을 수 있으므로 여기서 명시적으로 처리
            LogViewItem log = null;
            
            try 
            {
                // 로그 파일이 직접 존재하는지 먼저 확인
                if (File.Exists(selectedLogPath))
                {
                    // 선택된 로그 항목 찾기 - LINQ 사용하지 않고 직접 순회
                    foreach (var item in savedLogs)
                    {
                        if (item.path == selectedLogPath)
                        {
                            log = item;
                            break;
                        }
                    }
                    
                    // 로그를 못 찾은 경우 새 항목 생성
                    if (log == null)
                    {
                        log = new LogViewItem(selectedLogPath);
                        savedLogs.Add(log);
                    }
                    
                    // 파일 내용 읽기
                    string fileContent = File.ReadAllText(selectedLogPath);
                    selectedLogContent = fileContent;
                    log.content = fileContent; // 캐시 업데이트
                }
                else
                {
                    selectedLogContent = "(선택한 로그 파일을 찾을 수 없습니다)";
                    
                    // 리스트에서 존재하지 않는 로그 제거
                    for (int i = savedLogs.Count - 1; i >= 0; i--)
                    {
                        if (savedLogs[i].path == selectedLogPath)
                        {
                            savedLogs.RemoveAt(i);
                            break;
                        }
                    }
                    
                    // 로그 목록 다시 초기화
                    InitializeLogViewer();
                }
            }
            catch (Exception searchEx)
            {
                Debug.LogError($"로그 항목 검색 중 오류: {searchEx.Message}");
                selectedLogContent = "(로그 항목 검색 중 오류가 발생했습니다)";
                return;
            }
            
            // UI 갱신 - 에디터 갱신 요청
            EditorUtility.SetDirty(this);
            Repaint();
        }
        catch (Exception ex)
        {
            // 최종 안전망 - 모든 예외 캐치
            Debug.LogError($"로그 선택 중 예상치 못한 오류: {ex.Message}\n{ex.StackTrace}");
            selectedLogContent = $"(로그 로딩 오류: {ex.Message})";
        }
    }    private void InitializeLogViewer()
    {
        try
        {
            string absolutePath = GetAbsoluteLogsPath();
            
            if (!Directory.Exists(absolutePath))
            {
                try
                {
                    Directory.CreateDirectory(absolutePath);
                    AssetDatabase.Refresh();
                    Debug.Log($"로그 디렉토리 생성됨: {absolutePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"로그 디렉토리 생성 실패: {ex.Message}");
                }
            }
            
            // 기존 로그 목록 초기화
            if (savedLogs == null) savedLogs = new List<LogViewItem>();
            else savedLogs.Clear();
            
            // 디렉토리가 존재하지 않으면 종료
            if (!Directory.Exists(absolutePath))
            {
                Debug.LogError("로그 디렉토리를 찾을 수 없습니다: " + absolutePath);
                selectedLogContent = "로그 저장 디렉토리를 찾을 수 없습니다.";
                return;
            }
            
            try
            {
                // 로그 파일 로드 시도
                string[] logFiles = Directory.GetFiles(absolutePath, "*.md");
                Debug.Log($"{logFiles.Length}개의 로그 파일을 찾았습니다.");
                
                // 파일이 없는 경우 예시 파일 생성 시도
                if (logFiles.Length == 0)
                {
                    try
                    {
                        string exampleDate = DateTime.Now.ToString("yyyy-MM-dd");
                        string examplePath = Path.Combine(absolutePath, $"WorkLog_{exampleDate}.md");
                        string exampleContent = "# ToyMaker 작업로그 예시\n\n여기에 오늘의 작업 내용이 표시됩니다.\n작업 관리 탭에서 작업을 기록하고 저장해보세요!";
                        
                        // 예시 파일이 존재하지 않을 때만 생성
                        if (!File.Exists(examplePath))
                        {
                            File.WriteAllText(examplePath, exampleContent);
                            AssetDatabase.Refresh();
                            logFiles = new string[] { examplePath };
                        }
                    }
                    catch (Exception exampleEx)
                    {
                        Debug.LogWarning($"예시 로그 파일 생성 실패: {exampleEx.Message}");
                    }
                }
                
                // 각 파일을 개별적으로 처리하여 하나의 파일에서 오류가 발생해도 전체가 실패하지 않도록 함
                foreach (var file in logFiles)
                {
                    try 
                    {
                        savedLogs.Add(new LogViewItem(file));
                    }
                    catch (Exception fileEx) 
                    {
                        Debug.LogWarning($"로그 파일 읽기 오류: {file}, {fileEx.Message}");
                    }
                }
                
                // 로그 항목을 날짜 기준으로 정렬 (가장 최근 항목이 먼저 오도록)
                try
                {
                    // 정렬된 새 리스트 생성
                    var sortedLogs = new List<LogViewItem>();
                    
                    // 원본 리스트를 복사하여 정렬
                    foreach (var item in savedLogs)
                    {
                        sortedLogs.Add(item);
                    }
                    
                    // 수동 정렬 (버블 정렬 사용)
                    for (int i = 0; i < sortedLogs.Count - 1; i++)
                    {
                        for (int j = 0; j < sortedLogs.Count - i - 1; j++)
                        {
                            if (sortedLogs[j].date < sortedLogs[j + 1].date)
                            {
                                var temp = sortedLogs[j];
                                sortedLogs[j] = sortedLogs[j + 1];
                                sortedLogs[j + 1] = temp;
                            }
                        }
                    }
                    
                    savedLogs = sortedLogs;
                }
                catch (Exception sortEx)
                {
                    Debug.LogError($"로그 정렬 중 오류: {sortEx.Message}");
                }
            }
            catch (Exception ioEx)
            {
                Debug.LogError($"로그 파일 검색 중 오류: {ioEx.Message}");
            }
            
            // 첫 번째 로그 선택 (안전하게 처리)
            if (savedLogs.Count > 0)
            {
                try
                {
                    // 가장 최근 로그 선택
                    selectedLogPath = savedLogs[0].path;
                    selectedLogIndex = 0;
                    selectedLogContent = "로그를 선택하는 중...";
                    
                    // 로그 내용 로드 - 별도의 try-catch 블록으로 보호
                    try
                    {
                        OnSelectedLogChanged();
                    }
                    catch (Exception selEx)
                    {
                        Debug.LogError($"로그 선택 중 오류: {selEx.Message}");
                        selectedLogContent = "로그 선택 중 오류가 발생했습니다.";
                    }
                }
                catch (Exception idxEx)
                {
                    Debug.LogError($"로그 인덱스 접근 중 오류: {idxEx.Message}");
                    selectedLogContent = "로그 인덱스 접근 중 오류가 발생했습니다.";
                }
            }
            else
            {
                selectedLogContent = "저장된 작업 로그가 없습니다.\n작업 관리 탭에서 작업을 기록하고 저장해주세요.";
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"로그 뷰어 초기화 중 오류 발생: {ex.Message}");
            selectedLogContent = "로그 뷰어 초기화 중 오류가 발생했습니다.";
        }
        
        // UI 갱신
        EditorUtility.SetDirty(this);
        Repaint();
    }
      [Button(ButtonSizes.Small), PropertyOrder(35)]
    [HorizontalGroup("QuickLinks", Width = 0.33f)]
    [GUIColor(0.3f, 0.6f, 0.4f)]
    private void OpenReadme()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string readmePath = Path.Combine(projectRoot, "Assets/Readme.md");
        EditorUtility.OpenWithDefaultApp(readmePath);
    }
    
    [Button(ButtonSizes.Small), PropertyOrder(35)]
    [HorizontalGroup("QuickLinks", Width = 0.33f)]
    [GUIColor(0.3f, 0.6f, 0.4f)]
    private void OpenProjectGuide()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string guidePath = Path.Combine(projectRoot, "Assets/Docs/ProjectGuide.md");
        EditorUtility.OpenWithDefaultApp(guidePath);
    }
    
    [Button(ButtonSizes.Small), PropertyOrder(35)]
    [HorizontalGroup("QuickLinks", Width = 0.33f)]
    [GUIColor(0.3f, 0.6f, 0.4f)]
    private void RefreshLogViewer()
    {
        InitializeLogViewer();
        ShowNotification(new GUIContent("기록 뷰어가 새로고침되었습니다"));
    }    [MenuItem("Tools/ToyMaker/작업공간", priority = 1)]
    public static void OpenWindow()
    {
        try
        {
            // 더 안전한 방식으로 창을 열기
            InternalOpenWindow();
        }
        catch (Exception ex)
        {
            // 오류가 발생하면 로그와 다이얼로그로 사용자에게 알림
            Debug.LogError($"ToyMaker 작업공간 창을 열 수 없습니다: {ex.Message}");
            
            // EditorApplication.delayCall을 통해 UI 스레드에서 안전하게 다이얼로그 표시
            EditorApplication.delayCall += () => {
                EditorUtility.DisplayDialog("오류", 
                    $"ToyMaker 작업공간 창을 열 수 없습니다.\n오류: {ex.Message}\n\n" +
                    "에디터를 재시작하거나 Window > Panels > Tools 메뉴를 통해 다시 시도해보세요.", 
                    "확인");
            };
        }
    }
    
    // 더 안전한 방식으로 창을 여는 내부 메서드
    private static void InternalOpenWindow()
    {
        try
        {
            // 기존 윈도우 찾기
            var windows = Resources.FindObjectsOfTypeAll<ToyMakerWorkspaceWindow>();
            ToyMakerWorkspaceWindow window = null;
            
            if (windows != null && windows.Length > 0)
            {
                window = windows[0];
                window.Focus(); // 이미 존재하는 창에 포커스
            }
            else
            {
                // 새 창 생성 - 직접 방식 사용
                window = ScriptableObject.CreateInstance<ToyMakerWorkspaceWindow>();
                
                // 화면 중앙에 위치 - 화면 크기 기준으로 적절히 계산
                float width = Mathf.Min(Screen.currentResolution.width * 0.8f, 1200);
                float height = Mathf.Min(Screen.currentResolution.height * 0.8f, 800);
                Vector2 size = new Vector2(width, height);
                Vector2 screenCenter = new Vector2(Screen.currentResolution.width / 2, Screen.currentResolution.height / 2);
                window.position = new Rect(screenCenter - size/2, size);
                
                // 제목 설정 및 표시 - 유틸리티 윈도우로 표시하여 오류 가능성 최소화
                window.titleContent = new GUIContent("ToyMaker 작업공간");
                window.Show(true); // true = 유틸리티 윈도우로 표시 (독립 창)
                
                // 최소/최대 크기 제한
                window.minSize = new Vector2(500, 600);
            }
            
            // 안전한 초기화 - 창이 표시된 후 약간의 지연을 두고 초기화
            EditorApplication.delayCall += () => {
                try {
                    if (window != null)
                    {
                        window.Repaint();
                    }
                }
                catch (Exception ex) {
                    Debug.LogError($"윈도우 초기화 실패: {ex.Message}");
                }
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"작업공간 창 생성 실패: {ex.Message}\n{ex.StackTrace}");
            throw; // 상위 예외 처리로 전달
        }
    }
      protected override void OnEnable()
    {
        try
        {
            // 로그 저장 폴더 확인
            string logsPath = GetAbsoluteLogsPath();
            if (!Directory.Exists(logsPath))
            {
                try
                {
                    Directory.CreateDirectory(logsPath);
                    Debug.Log($"ToyMaker: 로그 디렉토리 생성 완료 - {logsPath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ToyMaker: 로그 디렉토리 생성 실패 - {ex.Message}");
                }
            }
            
            base.OnEnable();
            LoadData();
            if (workLogs.Count == 0)
            {
                workLogs.Add(new WorkLog());
            }
            InitializeLogViewer();
        }
        catch (Exception ex)
        {
            Debug.LogError($"ToyMaker 작업공간 활성화 중 오류 발생: {ex.Message}");
            
            // 기본 데이터로 초기화
            if (currentTasks == null) currentTasks = new List<WorkTask>();
            if (workLogs == null) workLogs = new List<WorkLog> { new WorkLog() };
        }
    }
    
    void AddNewLogButton()
    {
        if (SirenixEditorGUI.ToolbarButton(EditorIcons.Plus))
        {
            workLogs.Insert(0, new WorkLog());
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        SaveData();
    }
      private void SaveData()
    {
        try
        {
            // null 체크
            if (currentTasks == null) currentTasks = new List<WorkTask>();
            if (workLogs == null) workLogs = new List<WorkLog>();
            
            // 작업 목록 저장
            string tasksJson = JsonUtility.ToJson(new SerializableList<WorkTask> { items = currentTasks });
            EditorPrefs.SetString(PREFS_KEY + "tasks", tasksJson);
            
            // 작업 로그 저장
            string logsJson = JsonUtility.ToJson(new SerializableList<WorkLog> { items = workLogs });
            EditorPrefs.SetString(PREFS_KEY + "logs", logsJson);
        }
        catch (Exception ex)
        {
            Debug.LogError($"작업공간 데이터 저장 중 오류 발생: {ex.Message}");
        }
    }
    
    // 검색 필터링 - 메소드명 변경하여 모호성 해결
    private bool ShouldShowTaskList()
    {
        if (string.IsNullOrEmpty(searchQuery)) return true;
        return currentTasks.Any(t => t.description.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   t.date.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);
    }
    
    private bool ShouldShowLogList()
    {
        if (string.IsNullOrEmpty(searchQuery)) return true;
        return workLogs.Any(l => l.content.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                              l.date.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);
    }
      private void LoadData()
    {
        try
        {
            // 작업 목록 불러오기
            if (EditorPrefs.HasKey(PREFS_KEY + "tasks"))
            {
                string tasksJson = EditorPrefs.GetString(PREFS_KEY + "tasks");
                var loadedTasks = JsonUtility.FromJson<SerializableList<WorkTask>>(tasksJson);
                currentTasks = loadedTasks?.items ?? new List<WorkTask>();
                
                // null 체크
                if (currentTasks == null)
                {
                    currentTasks = new List<WorkTask>();
                }
            }
            else
            {
                // 처음 실행 시 예시 데이터 추가
                currentTasks = new List<WorkTask>
                {
                    new WorkTask { description = "DataManagerEditor UI 개선하기", date = DateTime.Now.ToString("MM/dd") },
                    new WorkTask { description = "GameObjectPoolManager 사용 예제 추가", date = DateTime.Now.ToString("MM/dd") },
                    new WorkTask { description = "작업공간 활용법 숙지하기", completed = true, date = DateTime.Now.ToString("MM/dd") },
                };
            }
            
            // 작업 로그 불러오기
            if (EditorPrefs.HasKey(PREFS_KEY + "logs"))
            {
                string logsJson = EditorPrefs.GetString(PREFS_KEY + "logs");
                var loadedLogs = JsonUtility.FromJson<SerializableList<WorkLog>>(logsJson);
                workLogs = loadedLogs?.items ?? new List<WorkLog>();
                
                // null 체크
                if (workLogs == null)
                {
                    workLogs = new List<WorkLog>();
                }
            }
            else
            {
                // 처음 실행 시 예시 로그 추가
                workLogs = new List<WorkLog>
                {
                    new WorkLog
                    {
                        date = DateTime.Now.ToString("yyyy-MM-dd"),
                        content = "프로젝트 시작!\n- ProjectGuide.md를 읽고 앞으로의 개발 방향 파악\n- 기존 코드 구조 분석\n- DataManagerEditor 개선 아이디어 정리"
                    }
                };
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"데이터 로드 중 오류 발생: {ex.Message}");
            
            // 오류 발생 시 기본 데이터로 초기화
            currentTasks = new List<WorkTask>
            {
                new WorkTask { description = "작업공간 오류 수정하기", date = DateTime.Now.ToString("MM/dd") }
            };
            
            workLogs = new List<WorkLog>
            {
                new WorkLog
                {
                    date = DateTime.Now.ToString("yyyy-MM-dd"),
                    content = "데이터 로드 중 오류가 발생했습니다.\n이 메시지가 계속 나타나면 EditorPrefs를 초기화해보세요."
                }
            };
        }
    }
    
    [Serializable]
    private class SerializableList<T>
    {
        public List<T> items = new List<T>();
    }    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void OnRuntimeMethodLoad()
    {
        // 플레이 모드를 확인하여 필요한 경우에만 실행
        if (EditorApplication.isPlaying)
            return;
            
        // 약간의 지연 후 실행하여 Unity 에디터가 완전히 로드된 후 작동하도록 함
        // Unity의 Delayed Call을 이용해 안전하게 실행
        EditorApplication.delayCall += () => {
            try 
            {
                // 에디터가 유효한 상태에 있을 때만 실행
                if (!EditorApplication.isCompiling && !EditorApplication.isUpdating) 
                {
                    OpenWorkspaceOnStartup();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"작업공간 자동 실행 중 오류 발생: {ex.Message}");
            }
        };
    }
    
    static void OpenWorkspaceOnStartup()
    {
        // 최초 실행 시에만 창 열기
        if (!EditorPrefs.HasKey(PREFS_KEY + "shownOnStartup"))
        {
            try
            {
                // 0.5초 더 지연하여 안정적으로 실행
                EditorApplication.delayCall += () => {
                    try
                    {
                        OpenWindow();
                        EditorPrefs.SetBool(PREFS_KEY + "shownOnStartup", true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"작업공간 창 열기 실패: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"작업공간 자동 실행 중 오류 발생: {ex.Message}");
            }
        }
    }
}