using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// استفاده از بسته رسمی Unity Mobile Notifications
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

    [SerializeField] private string smallIconName = "icon_small";
    [SerializeField] private string largeIconName = "icon_large";
    [SerializeField] private Color notificationColor = Color.red;

    private void Awake()
    {
        // پیاده‌سازی الگوی سینگلتون
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        // لغو همه نوتیفیکیشن‌های قبلی
        AndroidNotificationCenter.CancelAllNotifications();

        // ایجاد کانال نوتیفیکیشن (برای اندروید 8.0 و بالاتر)
        var channel = new AndroidNotificationChannel()
        {
            Id = channelId,
            Name = channelName,
            Description = channelDescription,
            Importance = Importance.High,
            CanBypassDnd = true,
            CanShowBadge = true,
            EnableLights = true,
            EnableVibration = true,
            LockScreenVisibility = LockScreenVisibility.Public
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
        // استفاده از هش کد ID دارو
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
            SmallIcon = smallIconName,
            LargeIcon = largeIconName,
            FireTime = reminderTime,
            Color = notificationColor,

            // تنظیمات اضافی برای نمایش به صورت آلارم
            Group = "medication_alarms",
            GroupSummary = true,
            GroupAlertBehaviour = GroupAlertBehaviours.GroupAlertAll,

            // داده های اضافی برای شناسایی دارو
            // ذخیره شناسه دارو در IntentData
            IntentData = $"medication_{medication.Id}",

            // تنظیمات بستن خودکار
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

    // دریافت دارو با شناسه
    public Medication GetMedicationById(string medicationId)
    {
        return medications.Find(m => m.Id == medicationId);
    }

    // دریافت همه داروها
    public List<Medication> GetAllMedications()
    {
        return new List<Medication>(medications);
    }

    // ذخیره‌سازی داده‌های داروها
    public void SaveMedications()
    {
        // ایجاد کلاس موقت برای ذخیره لیست داروها
        MedicationDataList dataList = new MedicationDataList();
        dataList.medicationsData = new List<MedicationData>();

        // تبدیل هر دارو به داده قابل ذخیره‌سازی
        foreach (Medication med in medications)
        {
            MedicationData data = new MedicationData();
            data.id = med.Id;
            data.name = med.Name;
            data.description = med.Description;
            data.type = (int)med.Type;
            data.quantity = med.Quantity;
            data.initialQuantity = med.InitialQuantity;
            data.dosagePerTime = med.DosagePerTime;
            data.isDaily = med.IsDaily;

            // ذخیره زمان‌های یادآوری
            data.reminderHours = new List<int>();
            data.reminderMinutes = new List<int>();
            foreach (DateTime time in med.ReminderTimes)
            {
                data.reminderHours.Add(time.Hour);
                data.reminderMinutes.Add(time.Minute);
            }

            // ذخیره روزهای هفته
            data.daysOfWeek = new List<int>();
            if (med.DaysOfWeek != null)
            {
                foreach (DayOfWeek day in med.DaysOfWeek)
                {
                    data.daysOfWeek.Add((int)day);
                }
            }

            // ذخیره اطلاعات مخاطب اضطراری
            if (med.EmergencyContact != null)
            {
                data.emergencyContactName = med.EmergencyContact.Name;
                data.emergencyContactPhone = med.EmergencyContact.PhoneNumber;
                data.emergencyContactEmail = med.EmergencyContact.Email;
                data.emergencyContactTelegram = med.EmergencyContact.TelegramId;
                data.emergencyContactWhatsApp = med.EmergencyContact.WhatsAppNumber;
            }

            dataList.medicationsData.Add(data);
        }

        // تبدیل به JSON
        string jsonData = JsonUtility.ToJson(dataList, true);

        // ذخیره در PlayerPrefs
        PlayerPrefs.SetString("MedicationsJson", jsonData);
        PlayerPrefs.Save();

        Debug.Log($"{medications.Count} دارو ذخیره شد.");
    }

    // بارگذاری داده‌های داروها
    private void LoadMedications()
    {
        // پاک کردن لیست فعلی
        medications.Clear();

        // خواندن JSON از PlayerPrefs
        string jsonData = PlayerPrefs.GetString("MedicationsJson", "");

        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.Log("داده ذخیره‌شده‌ای یافت نشد.");
            return;
        }

        // تبدیل JSON به داده
        MedicationDataList dataList = JsonUtility.FromJson<MedicationDataList>(jsonData);

        if (dataList == null || dataList.medicationsData == null)
        {
            Debug.LogError("خطا در خواندن داده‌های دارو!");
            return;
        }

        // بازسازی داروها از داده
        foreach (MedicationData data in dataList.medicationsData)
        {
            // ساخت شی دارو
            Medication med = new Medication(data.name, data.description, (MedicationType)data.type, data.quantity);
            med.Quantity = data.quantity;
            med.InitialQuantity = data.initialQuantity;
            med.DosagePerTime = data.dosagePerTime;
            med.IsDaily = data.isDaily;

            // بازسازی زمان‌های یادآوری
            med.ReminderTimes = new List<DateTime>();
            for (int i = 0; i < data.reminderHours.Count && i < data.reminderMinutes.Count; i++)
            {
                DateTime now = DateTime.Now;
                DateTime reminderTime = new DateTime(
                    now.Year, now.Month, now.Day,
                    data.reminderHours[i], data.reminderMinutes[i], 0
                );
                med.ReminderTimes.Add(reminderTime);
            }

            // بازسازی روزهای هفته
            med.DaysOfWeek = new List<DayOfWeek>();
            if (data.daysOfWeek != null)
            {
                foreach (int day in data.daysOfWeek)
                {
                    med.DaysOfWeek.Add((DayOfWeek)day);
                }
            }

            // بازسازی مخاطب اضطراری
            if (!string.IsNullOrEmpty(data.emergencyContactName))
            {
                ContactInfo contact = new ContactInfo(data.emergencyContactName);
                contact.PhoneNumber = data.emergencyContactPhone;
                contact.Email = data.emergencyContactEmail;
                contact.TelegramId = data.emergencyContactTelegram;
                contact.WhatsAppNumber = data.emergencyContactWhatsApp;
                med.EmergencyContact = contact;
            }

            medications.Add(med);
        }

        Debug.Log($"{medications.Count} دارو بارگذاری شد.");
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

            // بررسی اتمام دارو
            if (med.Quantity <= 0 && (med.Type == MedicationType.Pill || med.Type == MedicationType.Capsule))
            {
                // اعلام اتمام دارو
                MedicationUIManager uiManager = FindObjectOfType<MedicationUIManager>();
                if (uiManager != null)
                {
                    uiManager.ShowMedicationEmptyWarning(med.Name);
                }
            }
        }
    }
}

// کلاس‌های کمکی برای ذخیره‌سازی داده‌ها
[System.Serializable]
public class MedicationDataList
{
    public List<MedicationData> medicationsData;
}

[System.Serializable]
public class MedicationData
{
    public string id;
    public string name;
    public string description;
    public int type;
    public int quantity;
    public int initialQuantity;
    public int dosagePerTime;
    public bool isDaily;
    public List<int> reminderHours;
    public List<int> reminderMinutes;
    public List<int> daysOfWeek;
    public string emergencyContactName;
    public string emergencyContactPhone;
    public string emergencyContactEmail;
    public string emergencyContactTelegram;
    public string emergencyContactWhatsApp;
}