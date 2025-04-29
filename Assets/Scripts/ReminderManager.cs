using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Notifications.Android;

public class ReminderManager : MonoBehaviour
{
    // سینگلتون برای دسترسی آسان
    public static ReminderManager Instance { get; private set; }

    // لیست داروها
    private List<Medication> medications = new List<Medication>();

    // تنظیمات کانال نوتیفیکیشن
    private string channelId = "medication_reminder_channel";
    private string channelName = "یادآور دارو";
    private string channelDescription = "کانال مربوط به یادآوری مصرف داروها";

    private void Awake()
    {
        // پیاده‌سازی الگوی سینگلتون
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // حفظ این آبجکت بین صحنه‌ها
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // راه‌اندازی سیستم نوتیفیکیشن
        InitializeNotificationChannel();

        // بارگذاری داده‌های ذخیره شده
        LoadMedications();

        // بررسی و تنظیم مجدد یادآورها
        ScheduleAllReminders();
    }

    // راه‌اندازی کانال نوتیفیکیشن برای اندروید
    private void InitializeNotificationChannel()
    {
        var channel = new AndroidNotificationChannel()
        {
            Id = channelId,
            Name = channelName,
            Description = channelDescription,
            Importance = Importance.High,
            CanShowBadge = true,
            EnableLights = true,
            EnableVibration = true,
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        Debug.Log("کانال نوتیفیکیشن ایجاد شد");
    }

    // اضافه کردن دارو جدید
    public void AddMedication(Medication medication)
    {
        medications.Add(medication);
        SaveMedications(); // ذخیره‌سازی تغییرات
        ScheduleRemindersForMedication(medication); // تنظیم یادآورها
    }

    // حذف دارو
    public void RemoveMedication(string medicationId)
    {
        Medication med = medications.Find(m => m.Id == medicationId);
        if (med != null)
        {
            CancelRemindersForMedication(med);
            medications.Remove(med);
            SaveMedications();
        }
    }

    // تنظیم یادآور برای یک دارو
    private void ScheduleRemindersForMedication(Medication medication)
    {
        // لغو یادآورهای قبلی این دارو
        CancelRemindersForMedication(medication);

        // بررسی هر زمان یادآوری
        foreach (DateTime reminderTime in medication.ReminderTimes)
        {
            // اگر یادآور روزانه است یا روز مناسب است
            if (medication.IsDaily || medication.DaysOfWeek.Contains(DateTime.Now.DayOfWeek))
            {
                // ایجاد یادآور جدید
                ScheduleReminderNotification(medication, reminderTime);
            }
        }
    }

    // تنظیم همه یادآورها
    public void ScheduleAllReminders()
    {
        // لغو همه یادآورهای قبلی
        AndroidNotificationCenter.CancelAllNotifications();

        // تنظیم مجدد یادآورها برای همه داروها
        foreach (Medication medication in medications)
        {
            ScheduleRemindersForMedication(medication);
        }
    }

    // لغو یادآورهای یک دارو
    private void CancelRemindersForMedication(Medication medication)
    {
        // باید از الگوی مشخصی برای شناسه نوتیفیکیشن‌ها استفاده کنیم
        // و بر اساس آن نوتیفیکیشن‌های مربوط به این دارو را لغو کنیم

        // پیاده‌سازی ساده: استفاده از هش کد Id دارو
        int notificationId = medication.Id.GetHashCode();
        AndroidNotificationCenter.CancelNotification(notificationId);
    }

    // ایجاد نوتیفیکیشن برای یادآوری
    private void ScheduleReminderNotification(Medication medication, DateTime reminderTime)
    {
        // ایجاد نوتیفیکیشن اندروید
        var notification = new AndroidNotification()
        {
            Title = $"یادآور مصرف {medication.Name}",
            Text = $"زمان مصرف {medication.Name} رسیده است. {medication.Description}",
            SmallIcon = "icon_small",
            LargeIcon = "icon_large",
            FireTime = reminderTime,
            ShouldAutoCancel = false
        };

        // تنظیم نوتیفیکیشن به صورت تکرارشونده روزانه
        if (medication.IsDaily)
        {
            // تنظیم تکرار 24 ساعته
            TimeSpan repeatInterval = TimeSpan.FromDays(1);
            notification.RepeatInterval = repeatInterval;
        }

        // ارسال نوتیفیکیشن
        int notificationId = medication.Id.GetHashCode() + reminderTime.GetHashCode();
        AndroidNotificationCenter.SendNotificationWithExplicitID(notification, channelId, notificationId);

        Debug.Log($"یادآور برای {medication.Name} در {reminderTime} تنظیم شد.");
    }

    // چک کردن و پردازش یادآورها در صورت عدم پاسخ کاربر
    private IEnumerator CheckMissedReminders()
    {
        while (true)
        {
            // بررسی همه داروها
            foreach (Medication medication in medications)
            {
                foreach (DateTime reminderTime in medication.ReminderTimes)
                {
                    // اگر 15 دقیقه از زمان یادآوری گذشته باشد و هنوز پاسخی داده نشده
                    if (DateTime.Now > reminderTime.AddMinutes(15) &&
                        !PlayerPrefs.HasKey($"Reminder_Acknowledged_{medication.Id}_{reminderTime.ToShortTimeString()}"))
                    {
                        // ارسال پیام به مخاطب اضطراری
                        if (medication.EmergencyContact != null)
                        {
                            SendEmergencyMessage(medication);
                        }
                    }
                }
            }

            // بررسی مجدد بعد از 5 دقیقه
            yield return new WaitForSeconds(300);
        }
    }

    // ارسال پیام به مخاطب اضطراری
    private void SendEmergencyMessage(Medication medication)
    {
        string message = $"هشدار: {PlayerPrefs.GetString("UserName", "کاربر")} داروی {medication.Name} را در زمان مقرر مصرف نکرده است.";

        // تلاش برای ارسال ایمیل
        if (!string.IsNullOrEmpty(medication.EmergencyContact.Email))
        {
            SendEmail(medication.EmergencyContact.Email, "هشدار یادآور دارو", message);
        }

        // اینجا می‌توانید کدهای ارسال پیام به تلگرام یا واتس‌اپ را اضافه کنید
        // به SDK یا API های مربوطه نیاز خواهید داشت

        Debug.Log($"پیام اضطراری برای {medication.Name} به {medication.EmergencyContact.Name} ارسال شد.");
    }

    // ارسال ایمیل (نیاز به پیاده‌سازی با پلاگین یا سرویس وب)
    private void SendEmail(string email, string subject, string message)
    {
        // این متد نیاز به پیاده‌سازی با استفاده از پلاگین یا سرویس وب دارد
        // به عنوان مثال می‌توانید از Simple Mail یا Email Composer استفاده کنید
        Debug.Log($"ارسال ایمیل به {email}: {subject}");
    }

    // ذخیره‌سازی داده‌های داروها
    public void SaveMedications()
    {
        // تبدیل لیست داروها به فرمت JSON و ذخیره در PlayerPrefs
        // برای پیاده‌سازی واقعی، باید از JSONUtility یا Newtonsoft.Json استفاده کرد
        // و داده‌ها را به صورت JSON ذخیره کرد

        // اینجا فقط تعداد داروها را ذخیره می‌کنیم (نمونه اولیه)
        PlayerPrefs.SetInt("MedicationCount", medications.Count);
        PlayerPrefs.Save();

        Debug.Log($"{medications.Count} دارو ذخیره شد.");
    }

    // بارگذاری داده‌های داروها
    private void LoadMedications()
    {
        // در یک پیاده‌سازی واقعی، باید داده‌های داروها را از فایل JSON بخوانید
        // و آنها را به لیست داروها اضافه کنید

        int count = PlayerPrefs.GetInt("MedicationCount", 0);
        Debug.Log($"{count} دارو بارگذاری شد.");
    }

    // اعلام مصرف دارو توسط کاربر
    public void AcknowledgeMedication(string medicationId, DateTime reminderTime)
    {
        Medication med = medications.Find(m => m.Id == medicationId);
        if (med != null)
        {
            // کم کردن تعداد دارو در صورت نیاز
            med.TakeDose();

            // ذخیره تأیید مصرف
            PlayerPrefs.SetInt($"Reminder_Acknowledged_{med.Id}_{reminderTime.ToShortTimeString()}", 1);
            PlayerPrefs.Save();

            // ذخیره تغییرات مقدار دارو
            SaveMedications();
        }
    }
}