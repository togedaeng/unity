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
        int month = int.Parse(monthDropdown.options[monthDropdown.value].text);
        int year = int.Parse(yearDropdown.options[yearDropdown.value].text);
        int maxDays = System.DateTime.DaysInMonth(year, month);
        dayDropdown.ClearOptions();
        List<string> days = new List<string>();
        for (int i = 1; i <= maxDays; i++)
        {
            days.Add(i.ToString("D2"));
        }
        dayDropdown.AddOptions(days);
        dayDropdown.value = 0;
    }
}