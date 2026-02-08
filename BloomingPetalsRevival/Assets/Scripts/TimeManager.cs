using UnityEngine;
using TMPro;

public class TimeManager : MonoBehaviour
{
    public TextMeshProUGUI HourText;
    public TextMeshProUGUI DayPhaseText;

    public Phase CurrentPhase;
    public Phase PreviousPhase;

    public static TimeManager instance;

    [Range(0.1f, 100)] public float timeMultiplier = 1.0f;

    public float seconds;
    public int hours;
    public int minutes;
    public string AMPM;
    public bool timePaused;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        hours = 7;
        minutes = 0;
        AMPM = "AM";
        CurrentPhase = Phase.BeforeClass;
        PreviousPhase = Phase.BeforeClass;
        UpdateText();
        UpdateDayPhase();
    }

    private void UpdateText()
    {
        string hourString = hours.ToString("00");
        string minuteString = minutes.ToString("00");
        HourText.text = $"{hourString}:{minuteString} {AMPM}";
    }

    public void SetTime(int hours_, int minutes_, string ampm_)
    {
        hours = hours_;
        minutes = minutes_;
        AMPM = ampm_;
        seconds = 0;
        UpdateText();
        UpdateDayPhase();
    }

    private void Update()
    {
        if (!timePaused)
        {
            UpdateTime();
            UpdateDayPhase();

            if (Input.GetKeyDown(KeyCode.P)) minutes = 59; // WILL REMOVE LATER !!
        }
    }

    void UpdateTime()
    {
        seconds += Time.deltaTime * timeMultiplier;

        if (seconds >= 60f)
        {
            minutes++;
            seconds = 0f;
        }

        if (minutes >= 60)
        {
            hours++;
            minutes = 0;
        }

        if (hours == 12)
            AMPM = AMPM == "AM" ? "PM" : "AM";

        if (hours > 12)
            hours -= 12;

        UpdateText();
    }

    void UpdateDayPhase()
    {
        if (hours == 7 && minutes == 0 && AMPM == "AM") CurrentPhase = Phase.BeforeClass;
        else if (hours == 8 && minutes == 0 && AMPM == "AM") CurrentPhase = Phase.ClassPreparation;
        else if ((hours == 8 && minutes >= 30 && AMPM == "AM") || (hours == 1 && minutes <= 29 && AMPM == "PM")) CurrentPhase = Phase.Classtime;
        else if (hours == 1 && minutes >= 30 && AMPM == "PM") CurrentPhase = Phase.Lunchtime;
        else if (hours == 2 && minutes >= 0 && AMPM == "PM" && minutes < 15) CurrentPhase = Phase.ClassPreparation;
        else if (hours == 2 && minutes >= 15 && AMPM == "PM") CurrentPhase = Phase.Classtime;
        else if (hours == 3 && minutes >= 30 && AMPM == "PM") CurrentPhase = Phase.CleaningTime;
        else if (hours == 4 && minutes >= 30 && AMPM == "PM") CurrentPhase = Phase.EndOfDay;

        if (PreviousPhase != CurrentPhase && CurrentPhase != Phase.BeforeClass)
        {
            SetPreviousPhase();
        }

        DayPhaseText.text = GetCurrentPhase().ToString();
    }

    void SetPreviousPhase()
    {
        PreviousPhase = CurrentPhase;
    }

    public Phase GetCurrentDayPhase()
    {
        return CurrentPhase;
    }

    private string GetCurrentPhase()
    {
        switch (CurrentPhase)
        {
            case Phase.BeforeClass:
                return "Before Class";
            case Phase.ClassPreparation:
                return "Class Preparation";
            case Phase.Classtime:
                return "Class Time";
            case Phase.Lunchtime:
                return "Lunch Time";
            case Phase.CleaningTime:
                return "Cleaning Time";
            case Phase.EndOfDay:
                return "After School";
            default:
                return string.Empty;
        }
    }
}