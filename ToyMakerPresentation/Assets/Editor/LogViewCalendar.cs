using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Linq;

/// <summary>
/// ToyMaker 작업 로그를 달력 형식으로 표시하는 유틸리티 클래스
/// </summary>
public class LogViewCalendar
{
    // 달력 관련 상수
    private static readonly string[] DayNames = { "일", "월", "화", "수", "목", "금", "토" };
    private static readonly int DaysInWeek = 7;
    private static readonly float CalendarWidth = 200f;
    
    // 로그 데이터 참조
    private List<DateTime> logDates = new List<DateTime>();
    private DateTime currentViewMonth;
    private Action<DateTime> onDateSelected;
    
    // 선택 상태
    private DateTime selectedDate = DateTime.Today;
    private CalendarViewMode viewMode = CalendarViewMode.Month;

    public enum CalendarViewMode
    {
        Month,
        Week
    }
    
    public LogViewCalendar(Action<DateTime> dateSelectedCallback)
    {
        currentViewMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        onDateSelected = dateSelectedCallback;
    }
    
    public void UpdateLogDates(List<DateTime> dates)
    {
        logDates = dates ?? new List<DateTime>();
    }
    
    public void SetSelectedDate(DateTime date)
    {
        selectedDate = date.Date;
    }
    
    public void DrawCalendarUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(CalendarWidth));

        // 달력 헤더 - 월 표시와 이전/다음 버튼
        DrawCalendarHeader();
        
        // 뷰 모드 토글 버튼
        DrawViewModeToggle();
        
        // 뷰 모드에 따라 달력 또는 주간 뷰 표시
        if (viewMode == CalendarViewMode.Month)
        {
            DrawMonthCalendar();
        }
        else
        {
            DrawWeekView();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawCalendarHeader()
    {
        EditorGUILayout.BeginHorizontal();
        
        // 이전 월/주 버튼
        if (GUILayout.Button("◀", EditorStyles.miniButtonLeft, GUILayout.Width(30)))
        {
            if (viewMode == CalendarViewMode.Month)
                currentViewMonth = currentViewMonth.AddMonths(-1);
            else
                currentViewMonth = currentViewMonth.AddDays(-7);
        }
        
        // 현재 표시 중인 월/주 텍스트
        string headerText;
        if (viewMode == CalendarViewMode.Month)
        {
            headerText = $"{currentViewMonth.Year}년 {currentViewMonth.Month}월";
        }
        else
        {
            DateTime weekStart = GetFirstDayOfWeek(currentViewMonth);
            DateTime weekEnd = weekStart.AddDays(6);
            headerText = $"{weekStart:MM/dd} - {weekEnd:MM/dd}";
        }
        
        GUILayout.Label(headerText, EditorStyles.boldLabel, GUILayout.ExpandWidth(true), GUILayout.Height(20));
        
        // 다음 월/주 버튼
        if (GUILayout.Button("▶", EditorStyles.miniButtonRight, GUILayout.Width(30)))
        {
            if (viewMode == CalendarViewMode.Month)
                currentViewMonth = currentViewMonth.AddMonths(1);
            else
                currentViewMonth = currentViewMonth.AddDays(7);
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawViewModeToggle()
    {
        EditorGUILayout.BeginHorizontal();
        
        GUIContent monthContent = new GUIContent("월간 뷰", "월간 달력 보기");
        GUIContent weekContent = new GUIContent("주간 뷰", "주간 일정 보기");
        
        GUIStyle toggleButtonStyle = new GUIStyle(EditorStyles.miniButton);
        toggleButtonStyle.margin = new RectOffset(0, 0, 0, 0);
        
        if (GUILayout.Toggle(viewMode == CalendarViewMode.Month, monthContent, toggleButtonStyle, GUILayout.Width(CalendarWidth / 2 - 2)))
        {
            if (viewMode != CalendarViewMode.Month)
            {
                viewMode = CalendarViewMode.Month;
                // 월간 뷰로 전환 시 현재 선택된 날짜가 포함된 달로 이동
                currentViewMonth = new DateTime(selectedDate.Year, selectedDate.Month, 1);
            }
        }
        
        if (GUILayout.Toggle(viewMode == CalendarViewMode.Week, weekContent, toggleButtonStyle, GUILayout.Width(CalendarWidth / 2 - 2)))
        {
            if (viewMode != CalendarViewMode.Week)
            {
                viewMode = CalendarViewMode.Week;
                // 주간 뷰로 전환 시 현재 선택된 날짜가 포함된 주로 이동
                currentViewMonth = GetFirstDayOfWeek(selectedDate);
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 요일 헤더 (월간 뷰에만 표시)
        if (viewMode == CalendarViewMode.Month)
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < DaysInWeek; i++)
            {
                Color originalColor = GUI.color;
                if (i == 0) GUI.color = new Color(0.9f, 0.4f, 0.4f); // 일요일 색상
                else if (i == 6) GUI.color = new Color(0.4f, 0.6f, 0.9f); // 토요일 색상
                
                GUILayout.Label(DayNames[i], EditorStyles.centeredGreyMiniLabel, GUILayout.Width(CalendarWidth / DaysInWeek - 4), GUILayout.Height(16));
                GUI.color = originalColor;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void DrawMonthCalendar()
    {
        DateTime firstDay = new DateTime(currentViewMonth.Year, currentViewMonth.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(currentViewMonth.Year, currentViewMonth.Month);
        
        // 첫째 날의 요일 (0 = 일요일)
        int firstDayOfWeek = (int)firstDay.DayOfWeek;
        
        // 주 단위로 표시
        int currentDay = 1;
        int weeksToShow = (daysInMonth + firstDayOfWeek + 6) / 7;
        
        for (int week = 0; week < weeksToShow; week++)
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int dayOfWeek = 0; dayOfWeek < DaysInWeek; dayOfWeek++)
            {
                // 이전/다음 달의 날짜 표시 영역
                if ((week == 0 && dayOfWeek < firstDayOfWeek) || currentDay > daysInMonth)
                {
                    GUILayout.Label("", GUILayout.Width(CalendarWidth / DaysInWeek - 4), GUILayout.Height(20));
                    continue;
                }
                
                DateTime currentDate = new DateTime(currentViewMonth.Year, currentViewMonth.Month, currentDay);
                DrawDayButton(currentDate, CalendarWidth / DaysInWeek - 4);
                
                currentDay++;
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void DrawWeekView()
    {
        DateTime weekStart = GetFirstDayOfWeek(currentViewMonth);
        
        for (int dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            DateTime currentDate = weekStart.AddDays(dayOffset);
            
            EditorGUILayout.BeginHorizontal();
            
            // 요일 표시
            Color originalColor = GUI.color;
            if (dayOffset == 0) GUI.color = new Color(0.9f, 0.4f, 0.4f); // 일요일 색상
            else if (dayOffset == 6) GUI.color = new Color(0.4f, 0.6f, 0.9f); // 토요일 색상
            
            GUILayout.Label(DayNames[dayOffset], EditorStyles.boldLabel, GUILayout.Width(30));
            GUI.color = originalColor;
            
            // 날짜 버튼
            DrawDayButton(currentDate, CalendarWidth - 40);
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void DrawDayButton(DateTime date, float width)
    {
        // 특별한 날짜 스타일 설정
        bool isToday = date.Date == DateTime.Today;
        bool isSelected = date.Date == selectedDate.Date;
        bool hasLog = logDates.Any(d => d.Date == date.Date);
        
        // 버튼 스타일 설정
        GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
        buttonStyle.normal.textColor = isToday ? Color.white : EditorStyles.label.normal.textColor;
        
        if (isToday)
        {
            buttonStyle.normal.background = EditorGUIUtility.whiteTexture;
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
        
        // 날짜 표시 및 클릭 처리
        if (GUILayout.Button(date.Day.ToString(), buttonStyle, GUILayout.Width(width), GUILayout.Height(20)))
        {
            selectedDate = date;
            if (onDateSelected != null)
                onDateSelected(date);
        }
        
        // 스타일 초기화
        GUI.backgroundColor = Color.white;
    }
    
    // 특정 날짜가 속한 주의 첫 번째 일요일을 반환
    private DateTime GetFirstDayOfWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
