using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
#if UNITY_ANDROID
        AndroidNotificationCenter.OnNotificationReceived += OnNotificationReceived;
#endif

        // بررسی اینکه آیا برنامه با کلیک روی نوتیفیکیشن باز شده است
        CheckForNotificationLaunch();
    }

    private void OnDestroy()
    {
        // حذف رویداد
#if UNITY_ANDROID
        AndroidNotificationCenter.OnNotificationReceived -= OnNotificationReceived;
#endif
    }

    // بررسی اینکه آیا برنامه با کلیک روی نوتیفیکیشن باز شده است
    private void CheckForNotificationLaunch()
    {
#if UNITY_ANDROID
        AndroidNotificationIntentData intentData = AndroidNotificationCenter.GetLastNotificationIntent();

        if (intentData != null)
        {
            // استخراج شناسه دارو از نوتیفیکیشن
            string medicationId = intentData.Id.ToString();
            HandleNotification(medicationId);
        }
#endif
    }

    // رویداد دریافت نوتیفیکیشن
    private void OnNotificationReceived(AndroidNotificationIntentData intentData)
    {
        // این رویداد فقط زمانی که برنامه باز است فراخوانی می‌شود

        // استخراج شناسه دارو از نوتیفیکیشن
        string medicationId = intentData.Id.ToString();

        // نمایش هشدار تمام‌صفحه
        HandleNotification(medicationId);
    }

    // هندل کردن نوتیفیکیشن دریافتی
    private void HandleNotification(string notificationId)
    {
        // تبدیل شناسه نوتیفیکیشن به شناسه دارو
        // توجه: در اینجا فرض می‌کنیم که از الگوی خاصی برای تولید شناسه نوتیفیکیشن استفاده شده است
        // در پیاده‌سازی واقعی، باید الگوی مناسبی برای ذخیره و بازیابی شناسه دارو از نوتیفیکیشن طراحی کنید

        string medicationId = ExtractMedicationIdFromNotification(notificationId);

        // پیدا کردن دارو
        Medication medication = FindMedicationById(medicationId);

        if (medication != null)
        {
            // نمایش هشدار تمام‌صفحه
            ShowFullScreenAlarm(medication);
        }
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

        // روشن کردن صفحه
        WakeUpScreen();

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

        // تنظیم یادآور جدید برای 10 دقیقه بعد
        PostponeMedicationReminder(currentMedicationId, 10);

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
            // ایجاد نوتیفیکیشن جدید برای چند دقیقه بعد
            var notification = new AndroidNotification()
            {
                Title = $"یادآور مصرف {medication.Name} (به تعویق افتاده)",
                Text = $"زمان مصرف {medication.Name} رسیده است. {medication.Description}",
                SmallIcon = "icon_small",
                LargeIcon = "icon_large",
                FireTime = DateTime.Now.AddMinutes(minutes)
            };

            // ارسال نوتیفیکیشن
            string channelId = "medication_reminder_channel";
            int notificationId = medication.Id.GetHashCode() + DateTime.Now.GetHashCode();
            AndroidNotificationCenter.SendNotification(notification, channelId, notificationId);
        }
    }

    // پخش صدای هشدار
    private void PlayAlarmSound()
    {
        // پیاده‌سازی پخش صدای هشدار
        // می‌توانید از AudioSource استفاده کنید

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    // توقف صدای هشدار
    private void StopAlarmSound()
    {
        // توقف صدای هشدار
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    // روشن کردن صفحه
    private void WakeUpScreen()
    {
        // روشن کردن صفحه
#if UNITY_ANDROID
        // استفاده از کد زیر برای روشن کردن صفحه در اندروید
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
        {
            window.Call("addFlags", 0x00000080); // FLAG_KEEP_SCREEN_ON
            window.Call("addFlags", 0x00400000); // FLAG_SHOW_WHEN_LOCKED
            window.Call("addFlags", 0x04000000); // FLAG_TURN_SCREEN_ON
        }
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

    // ارسال پیام به مخاطب ا