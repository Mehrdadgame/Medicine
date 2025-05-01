using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// استفاده از بسته رسمی Unity Mobile Notifications
using Unity.Notifications.Android;

public class AlarmNotificationHandler : MonoBehaviour
{
    [Header("Notification Response")]
    [SerializeField] private GameObject fullScreenAlarmPanel;
    [SerializeField] private TMP_Text medicationNameText;
    [SerializeField] private TMP_Text medicationDescriptionText;
    [SerializeField] private Image medicationImage;
    [SerializeField] private Button acknowledgeButton;
    [SerializeField] private Button postponeButton;

    [Header("Alarm Sound")]
    [SerializeField] private AudioClip alarmSound;
    [SerializeField] private float alarmVolume = 1.0f;
    [SerializeField] private int postponeMinutes = 10; // زمان به تعویق انداختن به دقیقه

    // اطلاعات دارو فعلی
    private string currentMedicationId;
    private DateTime currentReminderTime;

    // سینگلتون برای دسترسی آسان
    public static AlarmNotificationHandler Instance { get; private set; }

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

        // مخفی کردن پنل در ابتدا
        if (fullScreenAlarmPanel != null)
        {
            fullScreenAlarmPanel.SetActive(false);
        }

        // تنظیم رویدادها
        if (acknowledgeButton != null)
        {
            acknowledgeButton.onClick.AddListener(OnAcknowledgeButtonClicked);
        }

        if (postponeButton != null)
        {
            postponeButton.onClick.AddListener(OnPostponeButtonClicked);
        }
    }

    private void Start()
    {
        // ثبت رویداد دریافت نوتیفیکیشن
        AndroidNotificationCenter.OnNotificationReceived += OnNotificationReceived;

        // بررسی اینکه آیا برنامه با کلیک روی نوتیفیکیشن باز شده است
        CheckForNotificationLaunch();
    }

    private void OnDestroy()
    {
        // حذف رویداد
        AndroidNotificationCenter.OnNotificationReceived -= OnNotificationReceived;
    }

    // بررسی اینکه آیا برنامه با کلیک روی نوتیفیکیشن باز شده است
    private void CheckForNotificationLaunch()
    {
        AndroidNotificationIntentData intentData = AndroidNotificationCenter.GetLastNotificationIntent();

        if (intentData != null)
        {
            // استخراج شناسه دارو از نوتیفیکیشن
            // تغییر از IntentData به Notification.IntentData
            string medicationId = ExtractMedicationIdFromNotification(intentData.Notification.IntentData);
            HandleNotification(medicationId);

            // روشن کردن صفحه
            WakeUpScreen();
        }
    }

    // رویداد دریافت نوتیفیکیشن
    private void OnNotificationReceived(AndroidNotificationIntentData intentData)
    {
        // این رویداد فقط زمانی که برنامه باز است فراخوانی می‌شود

        // استخراج شناسه دارو از نوتیفیکیشن
        // تغییر از IntentData به Notification.IntentData
        string medicationId = ExtractMedicationIdFromNotification(intentData.Notification.IntentData);

        // نمایش هشدار تمام‌صفحه
        HandleNotification(medicationId);

        // روشن کردن صفحه
        WakeUpScreen();
    }

    // استخراج شناسه دارو از داده های نوتیفیکیشن
    private string ExtractMedicationIdFromNotification(string intentData)
    {
        // الگوی داده: "medication_ID"
        if (!string.IsNullOrEmpty(intentData) && intentData.StartsWith("medication_"))
        {
            return intentData.Substring("medication_".Length);
        }

        // اگر الگو مطابقت نداشت، از شناسه نوتیفیکیشن استفاده می‌کنیم
        return intentData;
    }

    // هندل کردن نوتیفیکیشن دریافتی
    private void HandleNotification(string medicationId)
    {
        // پیدا کردن دارو
        Medication medication = FindMedicationById(medicationId);

        if (medication != null)
        {
            // نمایش هشدار تمام‌صفحه
            ShowFullScreenAlarm(medication);
        }
    }

    // پیدا کردن دارو با شناسه
    private Medication FindMedicationById(string medicationId)
    {
        // برای دسترسی به لیست داروها از ReminderManager استفاده می‌کنیم
        if (ReminderManager.Instance != null)
        {
            return ReminderManager.Instance.GetMedicationById(medicationId);
        }

        Debug.LogError("ReminderManager یافت نشد!");
        return null;
    }

    // نمایش هشدار تمام‌صفحه
    public void ShowFullScreenAlarm(Medication medication)
    {
        if (medication == null || fullScreenAlarmPanel == null)
        {
            return;
        }

        // ذخیره اطلاعات دارو فعلی
        currentMedicationId = medication.Id;
        currentReminderTime = DateTime.Now;

        // تنظیم اطلاعات دارو در UI
        if (medicationNameText != null)
        {
            medicationNameText.text = medication.Name;
        }

        if (medicationDescriptionText != null)
        {
            medicationDescriptionText.text = medication.Description;
        }

        if (medicationImage != null)
        {
            if (medication.Image != null)
            {
                medicationImage.sprite = medication.Image;
                medicationImage.enabled = true;
            }
            else
            {
                medicationImage.enabled = false;
            }
        }

        // پخش صدای هشدار
        PlayAlarmSound();

        // نمایش پنل هشدار
        fullScreenAlarmPanel.SetActive(true);

        // شروع تایمر برای ارسال پیام به مخاطب اضطراری در صورت عدم پاسخ
        StartCoroutine(StartEmergencyContactTimer(medication));
    }

    // رویداد دکمه تأیید مصرف دارو
    private void OnAcknowledgeButtonClicked()
    {
        if (string.IsNullOrEmpty(currentMedicationId))
        {
            return;
        }

        // ثبت مصرف دارو
        ReminderManager.Instance.AcknowledgeMedication(currentMedicationId, currentReminderTime);

        // توقف صدای هشدار
        StopAlarmSound();

        // مخفی کردن پنل هشدار
        fullScreenAlarmPanel.SetActive(false);

        // پاک کردن اطلاعات دارو فعلی
        currentMedicationId = null;
    }

    // رویداد دکمه به تعویق انداختن مصرف دارو
    private void OnPostponeButtonClicked()
    {
        if (string.IsNullOrEmpty(currentMedicationId))
        {
            return;
        }

        // تنظیم یادآور جدید برای چند دقیقه بعد
        PostponeMedicationReminder(currentMedicationId, postponeMinutes);

        // توقف صدای هشدار
        StopAlarmSound();

        // مخفی کردن پنل هشدار
        fullScreenAlarmPanel.SetActive(false);

        // پاک کردن اطلاعات دارو فعلی
        currentMedicationId = null;
    }

    // به تعویق انداختن یادآور دارو
    private void PostponeMedicationReminder(string medicationId, int minutes)
    {
        Medication medication = FindMedicationById(medicationId);

        if (medication != null)
        {
            // ایجاد نوتیفیکیشن اندروید
            var notification = new AndroidNotification()
            {
                Title = $"یادآور مصرف {medication.Name} (به تعویق افتاده)",
                Text = $"زمان مصرف {medication.Name} رسیده است. {medication.Description}",
                SmallIcon = "icon_small",
                LargeIcon = "icon_large",
                FireTime = DateTime.Now.AddMinutes(minutes),

                // تنظیمات اضافی
                Color = new Color32(255, 0, 0, 255),
                Group = "medication_alarms",
                GroupSummary = true,
                GroupAlertBehaviour = GroupAlertBehaviours.GroupAlertAll,

                // داده ها برای شناسایی
                // تغییر کلید به IntentData
                IntentData = $"medication_{medication.Id}",

                // تنظیمات بستن خودکار
                ShouldAutoCancel = false
            };

            // ارسال نوتیفیکیشن
            string channelId = "medication_reminder_channel";
            int notificationId = medication.Id.GetHashCode() + DateTime.Now.GetHashCode();
            AndroidNotificationCenter.SendNotificationWithExplicitID(notification, channelId, notificationId);

            Debug.Log($"یادآور {medication.Name} به {minutes} دقیقه بعد موکول شد.");
        }
    }

    // پخش صدای هشدار
    private void PlayAlarmSound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (alarmSound != null)
        {
            audioSource.clip = alarmSound;
            audioSource.volume = alarmVolume;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("فایل صوتی آلارم تنظیم نشده است!");
        }
    }

    // توقف صدای هشدار
    private void StopAlarmSound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    // روشن کردن صفحه و نمایش روی صفحه قفل
    private void WakeUpScreen()
    {
#if UNITY_ANDROID
        // استفاده از کد زیر برای روشن کردن صفحه در اندروید
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
        {
            // FLAG_KEEP_SCREEN_ON: نگه داشتن صفحه روشن
            window.Call("addFlags", 0x00000080);

            // FLAG_DISMISS_KEYGUARD: رد کردن صفحه کلید (برای اندروید قدیمی)
            window.Call("addFlags", 0x00400000);

            // FLAG_SHOW_WHEN_LOCKED: نمایش روی صفحه قفل
            window.Call("addFlags", 0x00080000);

            // FLAG_TURN_SCREEN_ON: روشن کردن صفحه
            window.Call("addFlags", 0x00200000);

            // اعلان به سیستم که این اکتیویتی با اولویت بالا است
            if (AndroidSDKLevel() >= 27) // Android 8.1 Oreo و بالاتر
            {
                activity.Call("setShowWhenLocked", true);
                activity.Call("setTurnScreenOn", true);
            }
        }
#endif
    }

    // بررسی نسخه SDK اندروید
    private int AndroidSDKLevel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return versionClass.GetStatic<int>("SDK_INT");
        }
#else
        return 0;
#endif
    }

    // تایمر ارسال پیام به مخاطب اضطراری
    private IEnumerator StartEmergencyContactTimer(Medication medication)
    {
        // انتظار برای 2 دقیقه
        yield return new WaitForSeconds(120);

        // اگر هنوز پنل هشدار فعال است (یعنی کاربر پاسخ نداده)
        if (fullScreenAlarmPanel.activeInHierarchy && medication.EmergencyContact != null)
        {
            // ارسال پیام به مخاطب اضطراری
            SendEmergencyMessage(medication);
        }
    }

    // ارسال پیام به مخاطب اضطراری
    private void SendEmergencyMessage(Medication medication)
    {
        if (medication == null || medication.EmergencyContact == null)
        {
            Debug.LogError("دارو یا مخاطب اضطراری تعریف نشده است!");
            return;
        }

        string userName = PlayerPrefs.GetString("UserName", "کاربر");
        string message = $"هشدار: {userName} داروی {medication.Name} را در زمان مقرر مصرف نکرده است.";

        // ارسال ایمیل
        if (!string.IsNullOrEmpty(medication.EmergencyContact.Email))
        {
            SendEmail(medication.EmergencyContact.Email, "هشدار یادآور دارو", message);
        }

        // ارسال پیامک
        if (!string.IsNullOrEmpty(medication.EmergencyContact.PhoneNumber))
        {
            SendSMS(medication.EmergencyContact.PhoneNumber, message);
        }

        Debug.Log($"پیام اضطراری برای {medication.Name} به {medication.EmergencyContact.Name} ارسال شد.");
    }

    // ارسال ایمیل
    private void SendEmail(string email, string subject, string message)
    {
        // این متد نیاز به پیاده‌سازی با استفاده از پلاگین یا سرویس وب دارد
        // به عنوان مثال می‌توانید از Simple Mail یا Email Composer استفاده کنید

        // نمونه کد برای استفاده از یک سرویس ایمیل فرضی:
        /*
        EmailService.Instance.SendEmail(
            toAddress: email,
            subject: subject,
            body: message,
            onSuccess: () => Debug.Log("ایمیل با موفقیت ارسال شد"),
            onFailure: (error) => Debug.LogError($"خطا در ارسال ایمیل: {error}")
        );
        */

        Debug.Log($"ارسال ایمیل به {email}: {subject}");
    }

    // ارسال پیامک
    private void SendSMS(string phoneNumber, string message)
    {
        // این متد نیاز به پیاده‌سازی با استفاده از پلاگین یا سرویس وب دارد

        // نمونه کد برای استفاده از پلاگین SMS فرضی:
        /*
        #if UNITY_ANDROID
        using (AndroidJavaClass smsClass = new AndroidJavaClass("com.example.smsplugin.SMSManager"))
        {
            smsClass.CallStatic("sendSMS", phoneNumber, message);
        }
        #endif
        */

        Debug.Log($"ارسال پیامک به {phoneNumber}: {message}");
    }
}