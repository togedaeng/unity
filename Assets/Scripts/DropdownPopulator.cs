using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스 추가
using System.Collections.Generic;

public class DropdownPopulator : MonoBehaviour
{
    public TMP_Dropdown yearDropdown;
    public TMP_Dropdown monthDropdown;
    public TMP_Dropdown dayDropdown;

    void Start()
    {
        PopulateYearDropdown();
        PopulateMonthDropdown();
        PopulateDayDropdown();
        UpdateDayOptions();
        monthDropdown.onValueChanged.AddListener(delegate { UpdateDayOptions(); });
        yearDropdown.onValueChanged.AddListener(delegate { UpdateDayOptions(); });
    }

    void PopulateYearDropdown()
    {
        yearDropdown.ClearOptions();
        int startYear = 1900;
        int endYear = 2025;
        List<string> years = new List<string>();
        for (int i = startYear; i <= endYear; i++)
        {
            years.Add(i.ToString());
        }
        yearDropdown.AddOptions(years);
    }

    void PopulateMonthDropdown()
    {
        monthDropdown.ClearOptions();
        List<string> months = new List<string>();
        for (int i = 1; i <= 12; i++)
        {
            months.Add(i.ToString("D2"));
        }
        monthDropdown.AddOptions(months);
    }

    void PopulateDayDropdown()
    {
        dayDropdown.ClearOptions();
        List<string> days = new List<string>();
        for (int i = 1; i <= 31; i++)
        {
            days.Add(i.ToString("D2"));
        }
        dayDropdown.AddOptions(days);
    }

    void UpdateDayOptions()
    {
        // 안전한 파싱을 위한 예외 처리
        try
        {
            // 드롭다운이 비어있거나 유효하지 않은 경우 체크
            if (monthDropdown.options.Count == 0 || yearDropdown.options.Count == 0)
            {
                Debug.LogWarning("⚠️ 드롭다운 옵션이 비어있습니다. 초기화를 건너뜁니다.");
                return;
            }

            // 현재 선택된 값이 유효한 범위 내에 있는지 확인
            if (monthDropdown.value < 0 || monthDropdown.value >= monthDropdown.options.Count ||
                yearDropdown.value < 0 || yearDropdown.value >= yearDropdown.options.Count)
            {
                Debug.LogWarning("⚠️ 드롭다운 선택값이 유효하지 않습니다.");
                return;
            }

            string monthText = monthDropdown.options[monthDropdown.value].text;
            string yearText = yearDropdown.options[yearDropdown.value].text;

            // 문자열이 숫자인지 확인
            if (!int.TryParse(monthText, out int month) || !int.TryParse(yearText, out int year))
            {
                Debug.LogWarning($"⚠️ 파싱 실패: 월='{monthText}', 년='{yearText}'");
                return;
            }

            // 유효한 월/년 범위 확인
            if (month < 1 || month > 12 || year < 1900 || year > 2025)
            {
                Debug.LogWarning($"⚠️ 유효하지 않은 날짜: 월={month}, 년={year}");
                return;
            }

            int maxDays = System.DateTime.DaysInMonth(year, month);
            dayDropdown.ClearOptions();
            List<string> days = new List<string>();
            for (int i = 1; i <= maxDays; i++)
            {
                days.Add(i.ToString("D2"));
            }
            dayDropdown.AddOptions(days);
            dayDropdown.value = 0;

            Debug.Log($"✅ 일 드롭다운 업데이트 완료: {year}년 {month}월 -> {maxDays}일까지");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ UpdateDayOptions 에러: {e.Message}");
        }
    }
}