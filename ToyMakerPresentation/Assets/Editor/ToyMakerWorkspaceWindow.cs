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
{
    private const string PREFS_KEY = "ToyMakerWorkspace_";
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

        [LabelText("태그"), DelayedProperty]
        [FolderPath]
        [PropertyTooltip("쉼표로 구분하여 여러 태그를 입력할 수 있습니다 (예: UI,버그수정,최적화)")]
        public string tags = "";

        [HorizontalGroup("TimeTracking")]
        [LabelText("시작 시간")]
        [PropertyTooltip("작업을 시작한 시간 (yyyy-MM-dd HH:mm)")]
        public string startTime = "";

        [HorizontalGroup("TimeTracking")]
        [LabelText("완료 시간")]
        [PropertyTooltip("작업을 완료한 시간 (yyyy-MM-dd HH:mm)")]
        public string endTime = "";

        [HideInInspector]
        public float estimatedHours = 0f;

        // 작업 소요 시간을 반환 (시간 단위)
        public float GetWorkDuration()
        {
            if (string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime))
                return estimatedHours; // 수동으로 입력한 추정 시간 반환

            if (DateTime.TryParse(startTime, out DateTime start) &&
                DateTime.TryParse(endTime, out DateTime end))
            {
                TimeSpan duration = end - start;
                return (float)duration.TotalHours;
            }

            return estimatedHours;
        }

        // 태그 목록을 배열로 반환
        public string[] GetTags()
        {
            if (string.IsNullOrEmpty(tags))
                return new string[0];

            return tags.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

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

    // 검색 기능 제거 - 사용자 요청에 따라 불필요한 기능 삭제
    // [TabGroup("작업관리", "작업관리"), PropertyOrder(-10)]
    // [HideLabel, LabelText("검색")]
    // [DelayedProperty]
    [HideInInspector]
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

    [TabGroup("작업관리", "작업관리")]
    [Button(ButtonSizes.Medium), PropertySpace(8), PropertyOrder(30)]
    [GUIColor(0.4f, 0.5f, 0.9f)]
    [LabelText("작업로그 저장")]
    private void SaveWorkLogs()
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
            DateTime now = DateTime.Now;
            string timestamp = now.ToString("yyyy-MM-dd");
            string timeSegment = now.ToString("HHmmss");
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

            // 파일 이름에 시간을 포함시켜 중복 방지 (WorkLog_YYYY-MM-DD_HHmmss.md)
            string filePath = Path.Combine(GetAbsoluteLogsPath(), $"WorkLog_{timestamp}_{timeSegment}.md");
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

    // 검색 기능 관련 필드
    [TabGroup("기록 뷰어", "기록 뷰어")]
    [PropertyOrder(99)]
    [HorizontalGroup("기록 뷰어/검색", Width = 1.0f)]
    [LabelText("검색어"), HideLabel]
    public string logSearchQuery = "";
    [TabGroup("기록 뷰어", "기록 뷰어")]
    [PropertyOrder(99)]
    [HorizontalGroup("기록 뷰어/검색")]
    [Button, GUIColor(0.3f, 0.6f, 0.9f)]
    [LabelText("검색")]
    private void SearchInLogs()
    {
        if (string.IsNullOrEmpty(logSearchQuery))
        {
            ShowNotification(new GUIContent("검색어를 입력해주세요"));
            return;
        }

        // 검색 결과 생성
        List<SearchResult> results = new List<SearchResult>();
        foreach (var log in savedLogs)
        {
            // 제목에서 검색
            if (log.title.IndexOf(logSearchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                results.Add(new SearchResult
                {
                    logItem = log,
                    matchType = SearchMatchType.Title,
                    matchText = log.title,
                    matchPosition = log.title.IndexOf(logSearchQuery, StringComparison.OrdinalIgnoreCase)
                });
            }

            // 내용에서 검색
            if (log.content != null)
            {
                int pos = log.content.IndexOf(logSearchQuery, StringComparison.OrdinalIgnoreCase);
                if (pos >= 0)
                {
                    // 검색어 주변 내용 추출 (앞뒤 50자)
                    int startPos = Math.Max(0, pos - 50);
                    int endPos = Math.Min(log.content.Length, pos + logSearchQuery.Length + 50);
                    string surroundingText = log.content.Substring(startPos, endPos - startPos);

                    // 실제 검색어 위치 계산
                    int matchPosInSurrounding = pos - startPos;

                    // 결과 추가
                    results.Add(new SearchResult
                    {
                        logItem = log,
                        matchType = SearchMatchType.Content,
                        matchText = "..." + surroundingText + "...",
                        matchPosition = pos
                    });
                }
            }
        }

        // 검색 결과 창 열기
        SearchResultsWindow.OpenWindow(results, logSearchQuery, this);
    }

    // 사용자 요청에 따라 불필요한 숫자 선택기 제거
    [HideInInspector]
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
            // 시작: 달력 뷰 및 로그 선택 UI 구성
            EditorGUILayout.BeginHorizontal();

            // 좌측: 달력 영역
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(220), GUILayout.ExpandHeight(true));

            // 달력 헤더
            DrawCalendarHeader();

            // 월간/주간 뷰 토글
            DrawViewModeToggle();

            // 달력 본체 - 날짜 그리드 표시
            DrawCalendarGrid();

            EditorGUILayout.EndVertical();

            // 우측: 선택된 날짜에 대한 로그 목록
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // 저장된 로그가 없으면 메시지 표시
            if (savedLogs == null || savedLogs.Count == 0)
            {
                EditorGUILayout.LabelField("저장된 작업 로그 없음");
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                return;
            }

            // 선택된 날짜에 대한 로그 표시
            DrawSelectedDateLogs();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }
        catch (Exception ex)
        {
            Debug.LogError($"로그 선택 UI 표시 중 오류: {ex.Message}");

            try
            {
                EditorGUILayout.EndVertical(); // 수직 레이아웃 종료 보장
                EditorGUILayout.EndHorizontal(); // 수평 레이아웃 종료 보장
            }
            catch { }
        }
    }

    // 달력 헤더 표시 - 년월 및 이전/다음 버튼
    private void DrawCalendarHeader()
    {
        EditorGUILayout.BeginHorizontal();

        // 이전 월 버튼
        if (GUILayout.Button("◀", EditorStyles.miniButtonLeft, GUILayout.Width(30)))
        {
            selectedViewDate = selectedViewDate.AddMonths(-1);
            UpdateLogsByDate();
        }

        // 현재 표시 중인 월 텍스트
        string headerText = $"{selectedViewDate.Year}년 {selectedViewDate.Month}월";
        GUILayout.Label(headerText, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

        // 다음 월 버튼
        if (GUILayout.Button("▶", EditorStyles.miniButtonRight, GUILayout.Width(30)))
        {
            selectedViewDate = selectedViewDate.AddMonths(1);
            UpdateLogsByDate();
        }

        EditorGUILayout.EndHorizontal();
    }

    // 월간/주간 뷰 토글 버튼
    private void DrawViewModeToggle()
    {
        EditorGUILayout.BeginHorizontal();

        string[] viewModeLabels = { "월간 뷰", "주간 뷰" };
        CalendarViewMode selectedMode = (CalendarViewMode)GUILayout.Toolbar(
            (int)GetCalendarViewMode(),
            viewModeLabels,
            EditorStyles.miniButton
        );

        // 모드 변경 감지 및 처리
        if (selectedMode != GetCalendarViewMode())
        {
            SetCalendarViewMode(selectedMode);
            UpdateLogsByDate();
        }

        EditorGUILayout.EndHorizontal();

        // 요일 헤더
        string[] dayNames = { "일", "월", "화", "수", "목", "금", "토" };
        EditorGUILayout.BeginHorizontal();

        for (int i = 0; i < 7; i++)
        {
            Color originalColor = GUI.color;

            // 주말 색상 설정
            if (i == 0) GUI.color = new Color(0.9f, 0.4f, 0.4f); // 일요일
            else if (i == 6) GUI.color = new Color(0.4f, 0.6f, 0.9f); // 토요일

            GUILayout.Label(dayNames[i], EditorStyles.centeredGreyMiniLabel, GUILayout.Width(25));
            GUI.color = originalColor;
        }

        EditorGUILayout.EndHorizontal();
    }

    // 달력 그리드 표시
    private void DrawCalendarGrid()
    {
        CalendarViewMode viewMode = GetCalendarViewMode();

        if (viewMode == CalendarViewMode.Month)
        {
            DrawMonthCalendar();
        }
        else
        {
            DrawWeekCalendar();
        }
    }

    // 월간 달력 표시
    private void DrawMonthCalendar()
    {
        DateTime firstDay = new DateTime(selectedViewDate.Year, selectedViewDate.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(selectedViewDate.Year, selectedViewDate.Month);

        // 첫째 날의 요일 (0 = 일요일)
        int firstDayOfWeek = (int)firstDay.DayOfWeek;

        // 주 단위로 표시
        int currentDay = 1;
        int weeksToShow = (daysInMonth + firstDayOfWeek + 6) / 7;

        for (int week = 0; week < weeksToShow; week++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
            {
                // 이전/다음 달의 날짜 표시 영역
                if ((week == 0 && dayOfWeek < firstDayOfWeek) || currentDay > daysInMonth)
                {
                    GUILayout.Label("", GUILayout.Width(25), GUILayout.Height(25));
                    continue;
                }

                DateTime currentDate = new DateTime(firstDay.Year, firstDay.Month, currentDay);
                DrawDayButton(currentDate);

                currentDay++;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    // 주간 달력 표시
    private void DrawWeekCalendar()
    {
        // 현재 선택된 날짜가 속한 주의 일요일 찾기
        DateTime startOfWeek = selectedViewDate.AddDays(-(int)selectedViewDate.DayOfWeek);

        EditorGUILayout.BeginVertical();

        // 주간 뷰의 각 날짜 표시
        for (int day = 0; day < 7; day++)
        {
            DateTime currentDate = startOfWeek.AddDays(day);

            EditorGUILayout.BeginHorizontal();

            // 요일 레이블
            string[] dayNames = { "일", "월", "화", "수", "목", "금", "토" };

            Color originalColor = GUI.color;
            if (day == 0) GUI.color = new Color(0.9f, 0.4f, 0.4f); // 일요일
            else if (day == 6) GUI.color = new Color(0.4f, 0.6f, 0.9f); // 토요일

            GUILayout.Label(dayNames[day], GUILayout.Width(25));

            GUI.color = originalColor;

            // 날짜 버튼
            DrawDayButton(currentDate, true);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    // 날짜 버튼 표시
    private void DrawDayButton(DateTime date, bool isWeekView = false)
    {
        // 특별한 날짜 스타일 설정
        bool isToday = date.Date == DateTime.Today;
        bool isSelected = date.Date == selectedViewDate.Date;
        bool hasLog = HasLogsForDate(date);
        // 버튼 스타일 설정
        GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
        buttonStyle.normal.textColor = isToday ? Color.white : EditorStyles.label.normal.textColor;

        float buttonWidth = isWeekView ? 165 : 25;

        if (isToday)
        {
            GUI.backgroundColor = new Color(0.0f, 0.5f, 0.8f);
        }
        if (isSelected)
        {
            buttonStyle.fontStyle = FontStyle.Bold;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        }
        if (hasLog)
        {
            buttonStyle.fontStyle = FontStyle.Bold;
            // 로그가 있는 날짜는 작은 점이나 배경 색상으로 표시
            if (!isSelected && !isToday)
                GUI.backgroundColor = new Color(0.9f, 0.9f, 0.4f);
        }

        // 날짜 표시 - 주간 뷰에서는 날짜와 기록 개수 표시
        string buttonText = isWeekView ?
            $"{date.Day}일 ({GetLogCountForDate(date)}개의 기록)" :
            date.Day.ToString();

        // 날짜 클릭 처리
        if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(25)))
        {
            selectedViewDate = date;
            OnDateSelected(date);
        }

        // 스타일 초기화
        GUI.backgroundColor = Color.white;
    }

    // 선택된 날짜의 로그 목록 표시
    private void DrawSelectedDateLogs()
    {
        EditorGUILayout.LabelField($"선택된 날짜: {selectedViewDate.ToString("yyyy-MM-dd")}", EditorStyles.boldLabel);

        // 해당 날짜의 로그 목록 가져오기
        List<LogViewItem> dateLogItems = GetLogsForDate(selectedViewDate);

        if (dateLogItems.Count == 0)
        {
            EditorGUILayout.HelpBox("이 날짜에 저장된 로그가 없습니다.", MessageType.Info);
            return;
        }

        // 선택된 날짜의 로그 표시
        for (int i = 0; i < dateLogItems.Count; i++)
        {
            LogViewItem logItem = dateLogItems[i];

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(logItem.title, EditorStyles.boldLabel);

            // 로그 선택 버튼
            if (GUILayout.Button("이 로그 보기", GUILayout.Height(30)))
            {
                try
                {
                    selectedLogPath = logItem.path;
                    OnSelectedLogChanged();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"로그 선택 중 오류 발생: {ex.Message}");
                }
            }

            EditorGUILayout.EndVertical();
        }
    }

    // 날짜 선택 이벤트 처리
    private void OnDateSelected(DateTime date)
    {
        selectedViewDate = date;

        // 해당 날짜의 첫 번째 로그 선택
        List<LogViewItem> dateLogItems = GetLogsForDate(date);

        if (dateLogItems.Count > 0)
        {
            try
            {
                selectedLogPath = dateLogItems[0].path;
                OnSelectedLogChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"날짜 로그 선택 중 오류 발생: {ex.Message}");
            }
        }
        else
        {
            // 선택된 날짜에 로그가 없으면 내용 지우기
            selectedLogContent = $"{date.ToString("yyyy-MM-dd")} 날짜에 작성된 로그가 없습니다.";
        }

        // UI 갱신
        Repaint();
    }

    // 날짜별 로그 정보 업데이트
    private void UpdateLogsByDate()
    {
        try
        {
            // 날짜별 로그 정보 초기화
            logsByDate.Clear();

            // 각 로그를 날짜별로 분류
            foreach (var log in savedLogs)
            {
                DateTime logDate = log.date.Date;

                if (!logsByDate.ContainsKey(logDate))
                {
                    logsByDate[logDate] = new List<LogViewItem>();
                }

                logsByDate[logDate].Add(log);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"날짜별 로그 분류 중 오류 발생: {ex.Message}");
        }
    }

    // 특정 날짜의 로그 목록 반환
    private List<LogViewItem> GetLogsForDate(DateTime date)
    {
        if (logsByDate.TryGetValue(date.Date, out var logs))
        {
            return logs;
        }
        return new List<LogViewItem>();
    }

    // 특정 날짜에 로그가 있는지 확인
    private bool HasLogsForDate(DateTime date)
    {
        return logsByDate.ContainsKey(date.Date) && logsByDate[date.Date].Count > 0;
    }

    // 특정 날짜의 로그 개수 반환
    private int GetLogCountForDate(DateTime date)
    {
        if (logsByDate.TryGetValue(date.Date, out var logs))
        {
            return logs.Count;
        }
        return 0;
    }

    // 캘린더 뷰 모드 가져오기
    private CalendarViewMode GetCalendarViewMode()
    {
        // 세션 상태에서 모드 가져오기, 없으면 월간 뷰 기본값 사용
        if (EditorPrefs.HasKey(PREFS_KEY + "calendarViewMode"))
        {
            return (CalendarViewMode)EditorPrefs.GetInt(PREFS_KEY + "calendarViewMode");
        }
        return CalendarViewMode.Month;
    }

    // 캘린더 뷰 모드 설정
    private void SetCalendarViewMode(CalendarViewMode mode)
    {
        EditorPrefs.SetInt(PREFS_KEY + "calendarViewMode", (int)mode);
    }

    [TabGroup("기록 뷰어", "기록 뷰어")]
    [PropertyOrder(102)]
    [ShowInInspector]
    [HideLabel]
    [OnInspectorGUI]
    private void DrawLogContentWithHighlight()
    {
        Color defaultColor = GUI.backgroundColor;

        EditorGUILayout.BeginVertical();

        // 하이라이트가 있는 경우 스크롤 위치 계산
        if (currentHighlightPos > 0 && currentHighlightLength > 0)
        {
            // 텍스트 영역의 가로 길이 계산 (평균적인 글자 너비 * 글자 수)
            int charsPerLine = 80; // 평균적인 한 줄에 들어가는 글자 수
            int lineNumber = 0;

            // 하이라이팅된 부분의 라인 찾기
            for (int i = 0; i < currentHighlightPos; i++)
            {
                if (i < selectedLogContent.Length && selectedLogContent[i] == '\n')
                {
                    lineNumber++;
                }
            }

            // 하이라이트 부분이 보이도록 스크롤 위치 설정
            if (lineNumber > 5)
            {
                EditorGUILayout.LabelField($"검색 결과: {lineNumber}번째 줄", EditorStyles.boldLabel);
            }
        }

        // 텍스트 영역 표시
        string newContent = EditorGUILayout.TextArea(selectedLogContent, GUILayout.Height(300));

        // 내용이 변경되었으면 업데이트
        if (newContent != selectedLogContent)
        {
            selectedLogContent = newContent;
            // 하이라이트 정보 초기화 (편집 시)
            currentHighlightPos = -1;
            currentHighlightLength = 0;
        }

        EditorGUILayout.EndVertical();

        GUI.backgroundColor = defaultColor;
    }

    [TabGroup("기록 뷰어", "기록 뷰어")]
    [PropertyOrder(103)]
    [HorizontalGroup("기록 뷰어/로그 편집 버튼", Width = 1.0f)]
    [Button(ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 0.5f)]
    [LabelText("변경사항 저장")]
    private void SaveLogEdits()
    {
        try
        {
            if (string.IsNullOrEmpty(selectedLogPath))
            {
                ShowNotification(new GUIContent("편집할 로그를 먼저 선택해주세요"));
                return;
            }

            // 파일 존재 확인
            if (!File.Exists(selectedLogPath))
            {
                ShowNotification(new GUIContent("선택한 로그 파일이 더 이상 존재하지 않습니다"));
                return;
            }

            // 변경된 내용 파일에 저장
            File.WriteAllText(selectedLogPath, selectedLogContent);

            // 로그 아이템 업데이트
            for (int i = 0; i < savedLogs.Count; i++)
            {
                if (savedLogs[i].path == selectedLogPath)
                {
                    savedLogs[i].content = selectedLogContent;
                    break;
                }
            }

            ShowNotification(new GUIContent("로그가 저장되었습니다"));

            // 에디터 리프레시
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(this);
            Repaint();
        }
        catch (Exception ex)
        {
            Debug.LogError($"로그 편집 저장 중 오류 발생: {ex.Message}");
            EditorUtility.DisplayDialog("오류", $"로그 편집 내용을 저장하는 중 오류가 발생했습니다.\n{ex.Message}", "확인");
        }
    }
    [Serializable]
    public class LogViewItem
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

                // 파일명에서 날짜 추출 (시간 정보가 있는 새 형식과 이전 형식 모두 지원)
                var match = Regex.Match(fileName, @"WorkLog_(\d{4}-\d{2}-\d{2})(?:_(\d{6}))?");
                if (match.Success)
                {
                    // 안전한 날짜 파싱
                    if (DateTime.TryParse(match.Groups[1].Value, out var fileDate))
                    {
                        date = fileDate;

                        // 시간 정보가 있는 경우 추출 (HHmmss)
                        string timeInfo = "";
                        if (match.Groups.Count > 2 && !string.IsNullOrEmpty(match.Groups[2].Value))
                        {
                            string timeStr = match.Groups[2].Value;
                            try
                            {
                                // 시간 형식 파싱 (HHmmss)
                                if (timeStr.Length == 6)
                                {
                                    int hours = int.Parse(timeStr.Substring(0, 2));
                                    int minutes = int.Parse(timeStr.Substring(2, 2));
                                    int seconds = int.Parse(timeStr.Substring(4, 2));
                                    date = new DateTime(date.Year, date.Month, date.Day, hours, minutes, seconds);
                                    timeInfo = $"({hours:00}:{minutes:00})";
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"시간 정보 파싱 실패: {timeStr}, {ex.Message}");
                            }
                        }

                        title = $"[{fileDate.ToString("yyyy-MM-dd")}] {timeInfo} 작업 기록";
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
    }
    private void OnSelectedLogChanged()
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
    }
    private void InitializeLogViewer()
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

            // 날짜별 로그 정보 초기화
            if (logsByDate == null) logsByDate = new Dictionary<DateTime, List<LogViewItem>>();
            else logsByDate.Clear();

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

                    // 로그를 날짜별로 분류 - 캘린더 뷰를 위한 데이터 구조화
                    UpdateLogsByDate();
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
    // 달력 뷰 관련 필드
    [HideInInspector]
    private Dictionary<DateTime, List<LogViewItem>> logsByDate = new Dictionary<DateTime, List<LogViewItem>>();

    // 캘린더 뷰 모드 (월간/주간)
    private enum CalendarViewMode
    {
        Month,
        Week
    }

    // 현재 선택된 날짜
    [HideInInspector]
    private DateTime selectedViewDate = DateTime.Today;

    [Button(ButtonSizes.Small), PropertyOrder(35)]
    [HorizontalGroup("QuickLinks", Width = 0.33f)]
    [GUIColor(0.3f, 0.6f, 0.4f)]
    private void OpenReadme()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string readmePath = Path.Combine(projectRoot, "Readme.md");
        EditorUtility.OpenWithDefaultApp(readmePath);
    }

    [Button(ButtonSizes.Small), PropertyOrder(35)]
    [HorizontalGroup("QuickLinks", Width = 0.33f)]
    [GUIColor(0.3f, 0.6f, 0.4f)]
    private void OpenProjectGuide()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string guidePath = Path.Combine(projectRoot, "Docs/ProjectGuide.md");
        EditorUtility.OpenWithDefaultApp(guidePath);
    }

    [Button(ButtonSizes.Small), PropertyOrder(35)]
    [HorizontalGroup("QuickLinks", Width = 0.33f)]
    [GUIColor(0.3f, 0.6f, 0.4f)]
    private void RefreshLogViewer()
    {
        InitializeLogViewer();
        ShowNotification(new GUIContent("기록 뷰어가 새로고침되었습니다"));
    }
    [MenuItem("Tools/ToyMaker/작업공간", priority = 1)]
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
            EditorApplication.delayCall += () =>
            {
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
                window.position = new Rect(screenCenter - size / 2, size);

                // 제목 설정 및 표시 - 유틸리티 윈도우로 표시하여 오류 가능성 최소화
                window.titleContent = new GUIContent("ToyMaker 작업공간");
                window.Show(true); // true = 유틸리티 윈도우로 표시 (독립 창)

                // 최소/최대 크기 제한
                window.minSize = new Vector2(500, 600);
            }

            // 안전한 초기화 - 창이 표시된 후 약간의 지연을 두고 초기화
            EditorApplication.delayCall += () =>
            {
                try
                {
                    if (window != null)
                    {
                        window.Repaint();
                    }
                }
                catch (Exception ex)
                {
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
    // 검색 필터링 기능 단순화 - 항상 모든 항목 표시
    private bool ShouldShowTaskList()
    {
        return true; // 항상 모든 작업 표시
    }

    private bool ShouldShowLogList()
    {
        return true; // 항상 모든 로그 표시
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
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void OnRuntimeMethodLoad()
    {
        // 플레이 모드를 확인하여 필요한 경우에만 실행
        if (EditorApplication.isPlaying)
            return;

        // 약간의 지연 후 실행하여 Unity 에디터가 완전히 로드된 후 작동하도록 함
        // Unity의 Delayed Call을 이용해 안전하게 실행
        EditorApplication.delayCall += () =>
        {
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
                EditorApplication.delayCall += () =>
                {
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

    // 검색 관련 필드    [HideInInspector]
    private int currentHighlightPos = -1;
    [HideInInspector]
    private int currentHighlightLength = 0;
    [HideInInspector]
    private string selectedLogContent = "";

    // 검색 결과 표시
    public void ShowSearchResults(List<SearchResult> results)
    {
        // 검색 결과 창 표시
        SearchResultsWindow.OpenWindow(results, logSearchQuery, this);
    }

    // 검색 결과 항목 클래스
    public class SearchResult
    {
        public LogViewItem logItem;
        public SearchMatchType matchType;
        public int matchPosition;
        public string matchText;
    }

    // 검색 결과 유형
    public enum SearchMatchType
    {
        Title,
        Content
    }

    // 검색된 문자열 하이라이트
    public void HighlightSearchMatch(int position, int length)
    {
        currentHighlightPos = position;
        currentHighlightLength = length;
        // UI 갱신
        EditorUtility.SetDirty(this);
        Repaint();
    }

    // 로그 파일 경로로 열기
    public void OpenLogFromPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            ShowNotification(new GUIContent("로그 파일을 찾을 수 없습니다"));
            return;
        }

        // 해당 로그 선택
        selectedLogPath = path;
        OnSelectedLogChanged();

        // 해당 탭 선택
        Repaint();
    }

    [TabGroup("통계/태그", "통계")]
    [PropertyOrder(200)]
    [TitleGroup("통계/태그/작업 시간 통계", Alignment = TitleAlignments.Centered)]
    [OnInspectorGUI]
    private void DrawWorkTimeStatistics()
    {
        if (currentTasks == null || currentTasks.Count == 0)
        {
            EditorGUILayout.HelpBox("작업 항목이 없습니다.", MessageType.Info);
            return;
        }

        // 작업 통계 데이터 계산
        int totalTasks = currentTasks.Count;
        int completedTasks = 0;
        float totalWorkHours = 0f;
        Dictionary<string, int> tagCounts = new Dictionary<string, int>();

        foreach (var task in currentTasks)
        {
            if (task.completed)
                completedTasks++;

            // 작업 시간 계산
            float taskHours = task.GetWorkDuration();
            totalWorkHours += taskHours;

            // 태그 카운트
            foreach (var tag in task.GetTags())
            {
                string normalizedTag = tag.Trim().ToLower();
                if (!string.IsNullOrEmpty(normalizedTag))
                {
                    if (tagCounts.ContainsKey(normalizedTag))
                        tagCounts[normalizedTag]++;
                    else
                        tagCounts[normalizedTag] = 1;
                }
            }
        }

        // 통계 정보 표시
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 작업 완료율
        float completionRate = totalTasks > 0 ? (float)completedTasks / totalTasks * 100f : 0f;
        EditorGUILayout.LabelField($"작업 완료율: {completionRate:F1}% ({completedTasks}/{totalTasks})", EditorStyles.boldLabel);

        // 진행률 바 표시
        Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
        EditorGUI.DrawRect(progressRect, new Color(0.1f, 0.1f, 0.1f));

        Rect filledRect = progressRect;
        filledRect.width = progressRect.width * (completionRate / 100f);
        EditorGUI.DrawRect(filledRect, new Color(0.2f, 0.7f, 0.3f));

        // 텍스트 중앙 정렬
        GUIStyle centeredStyle = new GUIStyle(EditorStyles.boldLabel);
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.normal.textColor = Color.white;
        EditorGUI.LabelField(progressRect, $"{completionRate:F1}%", centeredStyle);

        EditorGUILayout.Space();

        // 총 작업 시간
        EditorGUILayout.LabelField($"총 작업 시간: {totalWorkHours:F1} 시간", EditorStyles.boldLabel);

        // 주간 작업 시간 그래프
        if (totalWorkHours > 0)
        {
            EditorGUILayout.LabelField("주간 작업 시간 분포", EditorStyles.boldLabel);
            DrawWeeklyWorkTimeGraph();
        }

        EditorGUILayout.EndVertical();

        // 태그 클라우드 표시
        if (tagCounts.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("태그 클라우드", EditorStyles.boldLabel);
            DrawTagCloud(tagCounts);
        }
    }

    // 주간 작업 시간 그래프 표시
    private void DrawWeeklyWorkTimeGraph()
    {
        // 현재 날짜 기준 최근 7일의 작업 시간 계산
        Dictionary<DateTime, float> dailyWorkHours = new Dictionary<DateTime, float>();

        // 7일간의 날짜 초기화
        DateTime today = DateTime.Today;
        for (int i = 6; i >= 0; i--)
        {
            DateTime date = today.AddDays(-i);
            dailyWorkHours[date] = 0f;
        }

        // 각 작업의 시작/종료 시간을 날짜별로 집계
        foreach (var task in currentTasks)
        {
            if (!string.IsNullOrEmpty(task.startTime) && !string.IsNullOrEmpty(task.endTime))
            {
                if (DateTime.TryParse(task.startTime, out DateTime start) &&
                    DateTime.TryParse(task.endTime, out DateTime end))
                {
                    // 시작일과 종료일이 다른 경우 각 날짜별로 나누어 계산
                    if (start.Date == end.Date)
                    {
                        // 같은 날짜인 경우
                        if (dailyWorkHours.ContainsKey(start.Date))
                        {
                            dailyWorkHours[start.Date] += (float)(end - start).TotalHours;
                        }
                    }
                    else
                    {
                        // 여러 날짜에 걸친 작업
                        DateTime current = start.Date;
                        while (current <= end.Date)
                        {
                            if (dailyWorkHours.ContainsKey(current))
                            {
                                // 해당 날짜의 작업 시간 계산
                                if (current == start.Date)
                                {
                                    // 시작일: 시작 시간부터 자정까지
                                    DateTime endOfDay = current.AddDays(1).AddSeconds(-1);
                                    dailyWorkHours[current] += (float)(endOfDay - start).TotalHours;
                                }
                                else if (current == end.Date)
                                {
                                    // 종료일: 자정부터 종료 시간까지
                                    DateTime startOfDay = current.Date;
                                    dailyWorkHours[current] += (float)(end - startOfDay).TotalHours;
                                }
                                else
                                {
                                    // 중간 날짜: 하루 전체
                                    dailyWorkHours[current] += 24f;
                                }
                            }
                            current = current.AddDays(1);
                        }
                    }
                }
            }
        }

        // 그래프 영역 설정
        Rect graphRect = EditorGUILayout.GetControlRect(false, 100);
        Rect legendRect = EditorGUILayout.GetControlRect(false, 20);

        // 그래프 배경
        EditorGUI.DrawRect(graphRect, new Color(0.15f, 0.15f, 0.15f));

        // 최대 작업 시간 찾기
        float maxHours = dailyWorkHours.Values.Max();
        if (maxHours <= 0) maxHours = 8f; // 기본값 설정

        // 막대 너비 계산
        float barWidth = graphRect.width / 7 * 0.8f;
        float spacing = graphRect.width / 7 * 0.2f;

        // 각 날짜별 막대 그래프 그리기
        int index = 0;
        foreach (var entry in dailyWorkHours.OrderBy(d => d.Key))
        {
            // 막대 높이 및 위치 계산
            float barHeight = entry.Value / maxHours * graphRect.height;
            float xPos = graphRect.x + index * (barWidth + spacing);

            // 막대 그리기
            Rect barRect = new Rect(
                xPos + spacing / 2,
                graphRect.y + graphRect.height - barHeight,
                barWidth,
                barHeight
            );

            // 요일에 따라 색상 변경
            Color barColor = Color.cyan;
            if (entry.Key.DayOfWeek == DayOfWeek.Saturday) barColor = new Color(0.4f, 0.6f, 0.9f);
            if (entry.Key.DayOfWeek == DayOfWeek.Sunday) barColor = new Color(0.9f, 0.4f, 0.4f);

            EditorGUI.DrawRect(barRect, barColor);

            // 시간 값 표시
            if (entry.Value > 0)
            {
                GUIStyle timeStyle = new GUIStyle(EditorStyles.miniLabel);
                timeStyle.alignment = TextAnchor.MiddleCenter;
                timeStyle.normal.textColor = Color.white;
                EditorGUI.LabelField(new Rect(barRect.x, barRect.y - 15, barRect.width, 20),
                    $"{entry.Value:F1}h", timeStyle);
            }

            // 날짜 레이블 표시
            GUIStyle dayStyle = new GUIStyle(EditorStyles.miniLabel);
            dayStyle.alignment = TextAnchor.MiddleCenter;

            // 주말은 색상 변경
            if (entry.Key.DayOfWeek == DayOfWeek.Saturday) dayStyle.normal.textColor = new Color(0.4f, 0.6f, 0.9f);
            else if (entry.Key.DayOfWeek == DayOfWeek.Sunday) dayStyle.normal.textColor = new Color(0.9f, 0.4f, 0.4f);

            EditorGUI.LabelField(
                new Rect(xPos, legendRect.y, barWidth + spacing, legendRect.height),
                $"{entry.Key.ToString("MM/dd")}\n{GetDayOfWeekKorean(entry.Key.DayOfWeek)}",
                dayStyle
            );

            index++;
        }
    }

    // 요일을 한글로 변환
    private string GetDayOfWeekKorean(DayOfWeek dayOfWeek)
    {
        switch (dayOfWeek)
        {
            case DayOfWeek.Monday: return "월";
            case DayOfWeek.Tuesday: return "화";
            case DayOfWeek.Wednesday: return "수";
            case DayOfWeek.Thursday: return "목";
            case DayOfWeek.Friday: return "금";
            case DayOfWeek.Saturday: return "토";
            case DayOfWeek.Sunday: return "일";
            default: return "";
        }
    }

    // 태그 클라우드 표시
    private void DrawTagCloud(Dictionary<string, int> tagCounts)
    {
        if (tagCounts.Count == 0) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        int maxCount = tagCounts.Values.Max();
        int currentColumn = 0;
        int maxColumns = 3;

        foreach (var tag in tagCounts.OrderByDescending(t => t.Value))
        {
            if (currentColumn >= maxColumns)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                currentColumn = 0;
            }

            // 태그의 빈도에 따라 스타일 크기 조정
            float fontScale = (float)tag.Value / maxCount;
            fontScale = 0.8f + fontScale * 0.7f; // 1.0 ~ 1.5 범위로 조정

            GUIStyle tagStyle = new GUIStyle(EditorStyles.miniButton);
            tagStyle.fontSize = Mathf.RoundToInt(tagStyle.fontSize * fontScale);

            // 빈도에 따라 색상 변경
            Color tagColor = Color.Lerp(new Color(0.5f, 0.5f, 0.5f), new Color(0.2f, 0.7f, 0.9f), fontScale - 0.8f);
            tagStyle.normal.textColor = tagColor;

            if (GUILayout.Button($"{tag.Key} ({tag.Value})", tagStyle, GUILayout.MinWidth(80)))
            {
                // 해당 태그로 필터링
                filterTag = tag.Key;
                ApplyTagFilter();
            }

            currentColumn++;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    [TabGroup("통계/태그", "태그 필터링")]
    [PropertyOrder(201)]
    [TitleGroup("통계/태그/태그 기반 필터링", Alignment = TitleAlignments.Centered)]
    [LabelText("필터 태그"), PropertySpace(8)]
    [DelayedProperty]
    public string filterTag = "";

    [TabGroup("통계/태그", "태그 필터링")]
    [PropertyOrder(202)]
    [Button(ButtonSizes.Medium), GUIColor(0.4f, 0.6f, 0.9f)]
    [LabelText("태그로 필터링")]
    private void ApplyTagFilter()
    {
        if (string.IsNullOrEmpty(filterTag))
        {
            ShowNotification(new GUIContent("필터링할 태그를 입력하세요"));
            return;
        }

        // 태그로 작업 필터링
        List<WorkTask> filteredTasks = new List<WorkTask>();
        string normalizedFilter = filterTag.Trim().ToLower();

        foreach (var task in currentTasks)
        {
            foreach (var tag in task.GetTags())
            {
                if (tag.ToLower().Contains(normalizedFilter))
                {
                    filteredTasks.Add(task);
                    break;
                }
            }
        }

        // 결과 표시
        if (filteredTasks.Count > 0)
        {
            // 필터링된 작업 표시하는 창 열기
            ShowFilteredTasksWindow(filteredTasks, filterTag);
        }
        else
        {
            ShowNotification(new GUIContent($"'{filterTag}' 태그가 포함된 작업이 없습니다."));
        }
    }

    // 필터링된 작업 결과 창 표시
    private void ShowFilteredTasksWindow(List<WorkTask> filteredTasks, string tag)
    {
        // 필터링된 작업 목록을 보여주는 팝업 창
        var window = EditorWindow.GetWindow<FilteredTasksWindow>($"'{tag}' 태그 작업 목록");
        window.minSize = new Vector2(400, 300);
        window.ShowFilteredTasks(filteredTasks, tag);
    }



    // 필터링된 작업을 표시하는 창
    public class FilteredTasksWindow : EditorWindow
    {
        private List<WorkTask> filteredTasks;
        private string filterTag;
        private Vector2 scrollPosition;

        public void ShowFilteredTasks(List<WorkTask> tasks, string tag)
        {
            filteredTasks = tasks;
            filterTag = tag;
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField($"'{filterTag}' 태그 작업 목록 ({filteredTasks.Count}개)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var task in filteredTasks)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 작업 상태 및 제목
                EditorGUILayout.BeginHorizontal();

                GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
                statusStyle.normal.textColor = task.completed ?
                    new Color(0.0f, 0.7f, 0.0f) : new Color(0.8f, 0.3f, 0.0f);

                EditorGUILayout.LabelField(task.completed ? "☑ 완료" : "☐ 진행 중", statusStyle, GUILayout.Width(80));
                EditorGUILayout.LabelField(task.description, EditorStyles.boldLabel);

                EditorGUILayout.EndHorizontal();

                // 작업 날짜
                EditorGUILayout.LabelField($"날짜: {task.date}", EditorStyles.miniLabel);

                // 작업 시간 정보
                if (!string.IsNullOrEmpty(task.startTime) || !string.IsNullOrEmpty(task.endTime))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (!string.IsNullOrEmpty(task.startTime))
                        EditorGUILayout.LabelField($"시작: {task.startTime}", EditorStyles.miniLabel);
                    if (!string.IsNullOrEmpty(task.endTime))
                        EditorGUILayout.LabelField($"종료: {task.endTime}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();

                    // 소요 시간 계산
                    float duration = task.GetWorkDuration();
                    if (duration > 0)
                    {
                        EditorGUILayout.LabelField($"소요 시간: {duration:F1} 시간", EditorStyles.miniLabel);
                    }
                }

                // 태그 정보
                string[] tags = task.GetTags();
                if (tags.Length > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("태그:", GUILayout.Width(40));

                    foreach (var tag in tags)
                    {
                        GUIStyle tagStyle = new GUIStyle(EditorStyles.miniLabel);
                        // 검색 태그와 일치하는 경우 하이라이트
                        if (tag.ToLower().Contains(filterTag.ToLower()))
                        {
                            tagStyle.normal.textColor = new Color(0.1f, 0.6f, 0.9f);
                            tagStyle.fontStyle = FontStyle.Bold;
                        }
                        EditorGUILayout.LabelField(tag, tagStyle);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    // 완료된 작업을 표시하는 창
    public class CompletedTasksWindow : EditorWindow
    {
        private List<WorkTask> completedTasks;
        private Vector2 scrollPosition;
        private string filterText = "";

        public void ShowCompletedTasks(List<WorkTask> tasks)
        {
            completedTasks = tasks;
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField($"완료된 작업 목록 ({completedTasks.Count}개)", EditorStyles.boldLabel);

            // 필터 입력 필드
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("필터:", GUILayout.Width(40));
            string newFilter = EditorGUILayout.TextField(filterText);
            if (newFilter != filterText)
            {
                filterText = newFilter;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 작업을 날짜순으로 정렬하고 필터링
            List<WorkTask> filteredTasks = completedTasks
                .Where(t => string.IsNullOrEmpty(filterText) ||
                       t.description.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       (t.tags != null && t.tags.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderByDescending(t => t.date)
                .ToList();

            foreach (var task in filteredTasks)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 작업 제목
                EditorGUILayout.LabelField(task.description, EditorStyles.boldLabel);

                // 작업 날짜
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"완료일: {task.date}", EditorStyles.miniLabel);

                // 시간 정보가 있는 경우 표시
                if (!string.IsNullOrEmpty(task.startTime) && !string.IsNullOrEmpty(task.endTime))
                {
                    EditorGUILayout.LabelField($"소요 시간: {task.GetWorkDuration():F1} 시간", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                // 태그 정보
                string[] tags = task.GetTags();
                if (tags.Length > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("태그:", GUILayout.Width(40));

                    foreach (var tag in tags)
                    {
                        GUIStyle tagStyle = new GUIStyle(EditorStyles.miniLabel);
                        // 검색 필터와 일치하는 경우 하이라이트
                        if (!string.IsNullOrEmpty(filterText) &&
                            tag.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            tagStyle.normal.textColor = new Color(0.1f, 0.6f, 0.9f);
                            tagStyle.fontStyle = FontStyle.Bold;
                        }
                        EditorGUILayout.LabelField(tag, tagStyle);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();

            // 요약 정보
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            float totalHours = filteredTasks.Sum(t => t.GetWorkDuration());
            EditorGUILayout.LabelField($"총 완료 작업 시간: {totalHours:F1} 시간", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
        }
    }

    // 작업 시간 기록을 위한 창
    public class WorkTimeTrackingWindow : EditorWindow
    {
        private List<WorkTask> tasks;
        private ToyMakerWorkspaceWindow parentWindow;
        private int selectedTaskIndex = 0;
        private Vector2 scrollPosition;
        private bool isRecording = false;
        private DateTime recordStartTime;
        private string formattedStartTime = "";
        private string formattedEndTime = "";

        public void ShowWorkTimeTracking(List<WorkTask> taskList, ToyMakerWorkspaceWindow parent)
        {
            tasks = taskList;
            parentWindow = parent;

            // 미완료 작업이 있으면 첫 번째 미완료 작업 선택
            for (int i = 0; i < tasks.Count; i++)
            {
                if (!tasks[i].completed)
                {
                    selectedTaskIndex = i;
                    break;
                }
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("작업 시간 기록", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 작업 선택 드롭다운
            string[] taskNames = new string[tasks.Count];
            for (int i = 0; i < tasks.Count; i++)
            {
                taskNames[i] = tasks[i].description + (tasks[i].completed ? " (완료)" : "");
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("작업 선택:", GUILayout.Width(70));
            int newSelectedIndex = EditorGUILayout.Popup(selectedTaskIndex, taskNames);
            if (newSelectedIndex != selectedTaskIndex)
            {
                selectedTaskIndex = newSelectedIndex;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 현재 선택된 작업 정보
            var selectedTask = tasks[selectedTaskIndex];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 작업 설명
            EditorGUILayout.LabelField("작업:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selectedTask.description);

            // 작업 상태
            EditorGUILayout.BeginHorizontal();
            Color defaultColor = GUI.color;
            GUI.color = selectedTask.completed ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.4f, 0.3f);
            EditorGUILayout.LabelField(selectedTask.completed ? "상태: 완료됨" : "상태: 진행 중", EditorStyles.boldLabel);
            GUI.color = defaultColor;
            EditorGUILayout.EndHorizontal();

            // 기존 작업 시간 정보
            if (!string.IsNullOrEmpty(selectedTask.startTime) || !string.IsNullOrEmpty(selectedTask.endTime))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("기존 기록된 시간:", EditorStyles.boldLabel);

                if (!string.IsNullOrEmpty(selectedTask.startTime))
                    EditorGUILayout.LabelField($"시작: {selectedTask.startTime}");

                if (!string.IsNullOrEmpty(selectedTask.endTime))
                    EditorGUILayout.LabelField($"종료: {selectedTask.endTime}");

                if (!string.IsNullOrEmpty(selectedTask.startTime) && !string.IsNullOrEmpty(selectedTask.endTime))
                {
                    float duration = selectedTask.GetWorkDuration();
                    EditorGUILayout.LabelField($"소요 시간: {duration:F1} 시간");
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 시간 입력 필드
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("시간 기록", EditorStyles.boldLabel);

            if (isRecording)
            {
                // 녹화 중인 경우 현재 경과 시간 표시
                TimeSpan elapsed = DateTime.Now - recordStartTime;
                EditorGUILayout.LabelField($"기록 중... ({elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2} 경과)");

                if (GUILayout.Button("기록 종료", GUILayout.Height(30)))
                {
                    StopRecording();
                }
            }
            else
            {
                // 수동 입력
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("시작 시간:", GUILayout.Width(70));
                formattedStartTime = EditorGUILayout.TextField(formattedStartTime);

                if (GUILayout.Button("현재", GUILayout.Width(60)))
                {
                    formattedStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("종료 시간:", GUILayout.Width(70));
                formattedEndTime = EditorGUILayout.TextField(formattedEndTime);

                if (GUILayout.Button("현재", GUILayout.Width(60)))
                {
                    formattedEndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // 버튼 영역
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("기록 시작", GUILayout.Height(30)))
                {
                    StartRecording();
                }

                if (GUILayout.Button("시간 저장", GUILayout.Height(30)))
                {
                    SaveTimeTracking();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            // 도움말
            EditorGUILayout.HelpBox(
                "작업 시간 기록 방법:\n" +
                "1. 자동 기록: '기록 시작' 버튼을 눌러 작업 시작 시간을 기록하고, '기록 종료' 버튼을 눌러 종료 시간을 기록\n" +
                "2. 수동 기록: 시작 및 종료 시간을 직접 입력 후 '시간 저장' 버튼 클릭\n\n" +
                "형식: YYYY-MM-DD HH:MM (예: 2025-05-18 14:30)",
                MessageType.Info
            );
        }

        private void StartRecording()
        {
            isRecording = true;
            recordStartTime = DateTime.Now;
            formattedStartTime = recordStartTime.ToString("yyyy-MM-dd HH:mm");
            formattedEndTime = "";
        }

        private void StopRecording()
        {
            if (!isRecording) return;

            isRecording = false;
            DateTime endTime = DateTime.Now;
            formattedEndTime = endTime.ToString("yyyy-MM-dd HH:mm");

            // 자동으로 저장
            SaveTimeTracking();
        }

        private void SaveTimeTracking()
        {
            bool validStart = !string.IsNullOrEmpty(formattedStartTime);
            bool validEnd = !string.IsNullOrEmpty(formattedEndTime);

            if (!validStart && !validEnd)
            {
                EditorUtility.DisplayDialog("오류", "시작 시간과 종료 시간을 입력하세요.", "확인");
                return;
            }
            // 시간 형식 검증
            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now;

            bool startTimeValid = validStart && DateTime.TryParse(formattedStartTime, out startTime);
            bool endTimeValid = validEnd && DateTime.TryParse(formattedEndTime, out endTime);

            if (validStart && !startTimeValid)
            {
                EditorUtility.DisplayDialog("오류", "시작 시간이 올바른 형식이 아닙니다. (YYYY-MM-DD HH:MM)", "확인");
                return;
            }

            if (validEnd && !endTimeValid)
            {
                EditorUtility.DisplayDialog("오류", "종료 시간이 올바른 형식이 아닙니다. (YYYY-MM-DD HH:MM)", "확인");
                return;
            }

            if (startTimeValid && endTimeValid && startTime >= endTime)
            {
                EditorUtility.DisplayDialog("오류", "종료 시간은 시작 시간보다 나중이어야 합니다.", "확인");
                return;
            }

            // 작업에 시간 정보 저장
            tasks[selectedTaskIndex].startTime = formattedStartTime;
            tasks[selectedTaskIndex].endTime = formattedEndTime;

            // 완료 여부 확인
            if (!tasks[selectedTaskIndex].completed)
            {
                bool shouldComplete = EditorUtility.DisplayDialog(
                    "작업 완료",
                    "이 작업을 완료됨으로 표시하시겠습니까?",
                    "예", "아니오");

                if (shouldComplete)
                {
                    tasks[selectedTaskIndex].completed = true;
                }
            }

            // 변경 내용 저장
            EditorUtility.SetDirty(parentWindow);

            // 성공 메시지
            EditorUtility.DisplayDialog("성공", "작업 시간이 저장되었습니다.", "확인");

            // 창 닫기
            Close();
        }

        void OnDestroy()
        {
            // 녹화 중이었다면 중지
            if (isRecording)
            {
                isRecording = false;
            }
        }
    }
}