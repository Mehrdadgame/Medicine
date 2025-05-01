using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MedicationListItem : MonoBehaviour
{
    [SerializeField] private Image medicationImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text nextReminderText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Button editButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button takeDoseButton;

    private Medication medication;
    private MedicationUIManager uiManager;

    public void Initialize(Medication med, MedicationUIManager manager)
    {
        medication = med;
        uiManager = manager;

        // تنظیم اطلاعات دارو
        nameText.text = medication.Name;

        // تنظیم نوع دارو
        typeText.text = GetMedicationTypeText(medication.Type);

        // تنظیم تصویر
        if (medication.Image != null)
        {
            medicationImage.sprite = medication.Image;
            medicationImage.enabled = true;
        }
        else
        {
            medicationImage.enabled = false;
        }

        // نمایش زمان یادآوری بعدی
        DateTime nextReminder = GetNextReminderTime();
        if (nextReminder != DateTime.MinValue)
        {
            nextReminderText.text = $"یادآوری بعدی: {nextReminder.ToString("HH:mm")}";
        }
        else
        {
            nextReminderText.text = "بدون یادآوری";
        }

        // نمایش تعداد باقیمانده (فقط برای قرص و کپسول)
        if (medication.Type == MedicationType.Pill || medication.Type == MedicationType.Capsule)
        {
            quantityText.text = $"تعداد: {medication.Quantity} از {medication.InitialQuantity}";
            quantityText.gameObject.SetActive(true);

            // فعال کردن دکمه مصرف دارو
            takeDoseButton.gameObject.SetActive(true);
        }
        else
        {
            quantityText.gameObject.SetActive(false);
            takeDoseButton.gameObject.SetActive(false);
        }

        // تنظیم رویدادهای دکمه‌ها
        editButton.onClick.AddListener(OnEditButtonClicked);
        deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        takeDoseButton.onClick.AddListener(OnTakeDoseButtonClicked);
    }

    // بهبود متد نمایش هشدار برای تمام شدن دارو در کلاس MedicationListItem
    private void ShowMedicationEmptyWarning()
    {
        // ابتدا لاگ برای دیباگ
        Debug.LogWarning($"هشدار: داروی {medication.Name} تمام شده است. لطفاً داروی جدید تهیه کنید.");

        // نمایش هشدار در UI با استفاده از MedicationUIManager
        if (uiManager != null)
        {
            uiManager.ShowMedicationEmptyWarning(medication.Name);
        }
    }
    // تبدیل نوع دارو به متن
    private string GetMedicationTypeText(MedicationType type)
    {
        switch (type)
        {
            case MedicationType.Pill:
                return "قرص";
            case MedicationType.Capsule:
                return "کپسول";
            case MedicationType.Syrup:
                return "شربت";
            case MedicationType.Injection:
                return "تزریقی";
            case MedicationType.Inhaler:
                return "اسپری تنفسی";
            case MedicationType.Drops:
                return "قطره";
            case MedicationType.Cream:
                return "کرم";
            case MedicationType.Other:
                return "سایر";
            default:
                return "نامشخص";
        }
    }

    // محاسبه زمان یادآوری بعدی
    private DateTime GetNextReminderTime()
    {
        if (medication.ReminderTimes == null || medication.ReminderTimes.Count == 0)
        {
            return DateTime.MinValue;
        }

        DateTime now = DateTime.Now;
        DateTime closestTime = DateTime.MaxValue;

        foreach (DateTime reminderTime in medication.ReminderTimes)
        {
            // ساخت زمان برای امروز
            DateTime todayReminder = new DateTime(
                now.Year, now.Month, now.Day,
                reminderTime.Hour, reminderTime.Minute, 0
            );

            // اگر زمان امروز گذشته است، به فردا منتقل می‌شود
            if (todayReminder < now)
            {
                todayReminder = todayReminder.AddDays(1);
            }

            // بررسی اگر این دارو روزانه نیست و امروز/فردا باید مصرف شود
            if (!medication.IsDaily)
            {
                // روز هفته امروز و فردا
                DayOfWeek todayDayOfWeek = todayReminder.DayOfWeek;
                DayOfWeek tomorrowDayOfWeek = todayReminder.AddDays(1).DayOfWeek;

                // اگر امروز و فردا روز مصرف نیست، به روز بعدی مصرف منتقل می‌شود
                if (!medication.DaysOfWeek.Contains(todayDayOfWeek) && !medication.DaysOfWeek.Contains(tomorrowDayOfWeek))
                {
                    // جستجو برای اولین روز مناسب
                    for (int i = 2; i <= 7; i++)
                    {
                        DayOfWeek futureDayOfWeek = todayReminder.AddDays(i).DayOfWeek;
                        if (medication.DaysOfWeek.Contains(futureDayOfWeek))
                        {
                            todayReminder = todayReminder.AddDays(i);
                            break;
                        }
                    }
                }
                else if (!medication.DaysOfWeek.Contains(todayDayOfWeek) && medication.DaysOfWeek.Contains(tomorrowDayOfWeek))
                {
                    // اگر امروز روز مصرف نیست ولی فردا هست
                    todayReminder = todayReminder.AddDays(1);
                }
            }

            // انتخاب نزدیک‌ترین زمان
            if (todayReminder < closestTime)
            {
                closestTime = todayReminder;
            }
        }

        return closestTime == DateTime.MaxValue ? DateTime.MinValue : closestTime;
    }

    // رویداد دکمه ویرایش
    private void OnEditButtonClicked()
    {
        uiManager.EditMedication(medication);
    }

    // رویداد دکمه حذف
    private void OnDeleteButtonClicked()
    {
        uiManager.DeleteMedication(medication.Id);
    }

    // رویداد دکمه مصرف دارو
    private void OnTakeDoseButtonClicked()
    {
        // مصرف دارو
        medication.TakeDose();

        // ثبت مصرف در سیستم یادآوری
        ReminderManager.Instance.AcknowledgeMedication(medication.Id, DateTime.Now);

        // بروزرسانی نمایش تعداد
        quantityText.text = $"تعداد: {medication.Quantity} از {medication.InitialQuantity}";

        // اگر دارو تمام شده است
        if (medication.Quantity <= 0 && (medication.Type == MedicationType.Pill || medication.Type == MedicationType.Capsule))
        {
            // نمایش پیام هشدار
            ShowMedicationEmptyWarning();
        }
    }

    // نمایش هشدار برای تمام شدن دارو
}