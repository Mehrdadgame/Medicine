using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class MedicationUIManager : MonoBehaviour
{
    [Header("Add Medication Panel")]
    [SerializeField] private GameObject addMedicationPanel;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField descriptionInput;
    [SerializeField] private TMP_Dropdown medicationTypeDropdown;
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private TMP_InputField dosageInput;
    [SerializeField] private Button addImageButton;
    [SerializeField] private Image medicationImagePreview;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button cancelButton;

    [Header("Reminders Time Setting")]
    [SerializeField] private GameObject reminderTimePanel;
    [SerializeField] private TMP_Dropdown hourDropdown;
    [SerializeField] private TMP_Dropdown minuteDropdown;
    [SerializeField] private Toggle dailyToggle;
    [SerializeField] private ToggleGroup daysOfWeekToggleGroup;
    [SerializeField] private Button addReminderTimeButton;
    [SerializeField] private Transform reminderTimesContainer;
    [SerializeField] private GameObject reminderTimeItemPrefab;

    [Header("Emergency Contact")]
    [SerializeField] private TMP_InputField contactNameInput;
    [SerializeField] private TMP_InputField phoneNumberInput;
    [SerializeField] private TMP_InputField telegramIdInput;
    [SerializeField] private TMP_InputField whatsappInput;
    [SerializeField] private TMP_InputField emailInput;

    [Header("Medication List")]
    [SerializeField] private Transform medicationListContainer;
    [SerializeField] private GameObject medicationListItemPrefab;
    [SerializeField] private Button addNewMedicationButton;

    // داروی در حال ویرایش
    private Medication currentMedication;

    // تصویر انتخاب شده
    private Sprite selectedImage;

    // لیست زمان‌های یادآوری موقت
    private List<DateTime> tempReminderTimes = new List<DateTime>();

    // Start is called before the first frame update
    void Start()
    {
        InitializeUI();
        LoadMedicationsToUI();
    }

    // مقدار دهی اولیه رابط کاربری
    private void InitializeUI()
    {
        // مقداردهی اولیه لیست کشویی نوع دارو
        medicationTypeDropdown.ClearOptions();
        List<string> typeOptions = new List<string>
        {
            "قرص",
            "کپسول",
            "شربت",
            "تزریقی",
            "اسپری تنفسی",
            "قطره",
            "کرم",
            "سایر"
        };
        medicationTypeDropdown.AddOptions(typeOptions);

        // مقداردهی اولیه لیست کشویی ساعت و دقیقه
        hourDropdown.ClearOptions();
        List<string> hourOptions = new List<string>();
        for (int i = 0; i < 24; i++)
        {
            hourOptions.Add(i.ToString("00"));
        }
        hourDropdown.AddOptions(hourOptions);

        minuteDropdown.ClearOptions();
        List<string> minuteOptions = new List<string>();
        for (int i = 0; i < 60; i += 5)
        {
            minuteOptions.Add(i.ToString("00"));
        }
        minuteDropdown.AddOptions(minuteOptions);

        // تنظیم رویدادها
        addImageButton.onClick.AddListener(OpenImagePicker);
        saveButton.onClick.AddListener(SaveMedication);
        cancelButton.onClick.AddListener(CancelAddingMedication);
        addReminderTimeButton.onClick.AddListener(AddReminderTime);
        addNewMedicationButton.onClick.AddListener(ShowAddMedicationPanel);

        // پنهان کردن پنل اضافه کردن دارو در ابتدا
        addMedicationPanel.SetActive(false);
    }

    // نمایش پنل اضافه کردن دارو
    public void ShowAddMedicationPanel()
    {
        // پاک کردن فیلدها
        nameInput.text = "";
        descriptionInput.text = "";
        medicationTypeDropdown.value = 0;
        quantityInput.text = "30"; // مقدار پیش‌فرض
        dosageInput.text = "1"; // مقدار پیش‌فرض
        medicationImagePreview.sprite = null;
        selectedImage = null;

        // پاک کردن مقادیر مخاطب اضطراری
        contactNameInput.text = "";
        phoneNumberInput.text = "";
        telegramIdInput.text = "";
        whatsappInput.text = "";
        emailInput.text = "";

        // پاک کردن لیست زمان‌های یادآوری موقت
        tempReminderTimes.Clear();

        // پاک کردن لیست زمان‌های نمایش داده شده
        foreach (Transform child in reminderTimesContainer)
        {
            Destroy(child.gameObject);
        }

        // نمایش پنل
        addMedicationPanel.SetActive(true);

        // ایجاد داروی جدید موقت
        currentMedication = null;
    }

    // باز کردن انتخابگر تصویر
    private void OpenImagePicker()
    {
#if UNITY_ANDROID
        // برای اندروید باید از پلاگین مناسب استفاده کنید
        // مثال:
        // AndroidImagePicker.OpenGallery(OnImageSelected);
        Debug.Log("باز کردن گالری اندروید");

#elif UNITY_IOS
        // برای iOS باید از پلاگین مناسب استفاده کنید
        // مثال:
        // IOSImagePicker.OpenGallery(OnImageSelected);
        Debug.Log("باز کردن گالری iOS");
        
#else
        // برای ویندوز، مک و وب (تست)
        Debug.Log("انتخاب تصویر روی این پلتفرم پشتیبانی نمی‌شود");
        // برای تست می‌توانید از یک تصویر پیش‌فرض استفاده کنید
        StartCoroutine(LoadPlaceholderImage());
#endif
    }

    // برای تست: بارگذاری تصویر نمونه
    private IEnumerator LoadPlaceholderImage()
    {
        // این فقط برای تست است - در برنامه واقعی باید از گالری استفاده کنید
        string placeholderUrl = "https://via.placeholder.com/200";
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(placeholderUrl);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            selectedImage = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            medicationImagePreview.sprite = selectedImage;
        }
        else
        {
            Debug.LogError("خطا در بارگذاری تصویر نمونه: " + request.error);
        }
    }

    // افزودن زمان یادآوری
    private void AddReminderTime()
    {
        // ایجاد زمان یادآوری بر اساس مقادیر انتخاب شده
        int hour = int.Parse(hourDropdown.options[hourDropdown.value].text);
        int minute = int.Parse(minuteDropdown.options[minuteDropdown.value].text);

        DateTime now = DateTime.Now;
        DateTime reminderTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);

        // اگر زمان برای امروز گذشته است، به فردا منتقل می‌شود
        if (reminderTime < now)
        {
            reminderTime = reminderTime.AddDays(1);
        }

        // اضافه کردن به لیست موقت
        if (!tempReminderTimes.Contains(reminderTime))
        {
            tempReminderTimes.Add(reminderTime);

            // اضافه کردن به UI
            GameObject reminderTimeItem = Instantiate(reminderTimeItemPrefab, reminderTimesContainer);
            TMP_Text timeText = reminderTimeItem.GetComponentInChildren<TMP_Text>();
            timeText.text = reminderTime.ToString("HH:mm");

            // دکمه حذف
            Button deleteButton = reminderTimeItem.GetComponentInChildren<Button>();
            int index = tempReminderTimes.Count - 1;
            deleteButton.onClick.AddListener(() => RemoveReminderTime(index, reminderTimeItem));
        }
    }

    // حذف زمان یادآوری
    private void RemoveReminderTime(int index, GameObject reminderTimeItem)
    {
        if (index >= 0 && index < tempReminderTimes.Count)
        {
            tempReminderTimes.RemoveAt(index);
            Destroy(reminderTimeItem);
        }
    }

    // ذخیره دارو
    private void SaveMedication()
    {
        // بررسی اعتبار داده‌ها
        if (string.IsNullOrEmpty(nameInput.text))
        {
            Debug.LogError("نام دارو نمی‌تواند خالی باشد");
            ShowErrorMessage("لطفاً نام دارو را وارد کنید");
            return;
        }

        if (tempReminderTimes.Count == 0)
        {
            Debug.LogError("حداقل یک زمان یادآوری لازم است");
            ShowErrorMessage("لطفاً حداقل یک زمان یادآوری اضافه کنید");
            return;
        }

        // تبدیل نوع دارو
        MedicationType medicationType = (MedicationType)medicationTypeDropdown.value;

        // تبدیل مقدار
        int quantity = 0;
        if (medicationType == MedicationType.Pill || medicationType == MedicationType.Capsule)
        {
            if (!int.TryParse(quantityInput.text, out quantity) || quantity <= 0)
            {
                Debug.LogError("تعداد دارو باید یک عدد مثبت باشد");
                ShowErrorMessage("لطفاً تعداد معتبر وارد کنید");
                return;
            }
        }

        // تبدیل دوز مصرفی
        int dosage = 1;
        if (!string.IsNullOrEmpty(dosageInput.text))
        {
            if (!int.TryParse(dosageInput.text, out dosage) || dosage <= 0)
            {
                Debug.LogError("دوز مصرفی باید یک عدد مثبت باشد");
                ShowErrorMessage("لطفاً دوز مصرفی معتبر وارد کنید");
                return;
            }
        }

        // ایجاد داروی جدید یا ویرایش داروی موجود
        Medication medication;
        if (currentMedication == null)
        {
            // ایجاد داروی جدید
            medication = new Medication(nameInput.text, descriptionInput.text, medicationType, quantity);
        }
        else
        {
            // ویرایش داروی موجود
            medication = currentMedication;
            medication.Name = nameInput.text;
            medication.Description = descriptionInput.text;
            medication.Type = medicationType;

            if (medicationType == MedicationType.Pill || medicationType == MedicationType.Capsule)
            {
                medication.Quantity = quantity;
                medication.InitialQuantity = quantity;
            }
        }

        // تنظیم دوز مصرفی
        medication.DosagePerTime = dosage;

        // تنظیم تصویر
        if (selectedImage != null)
        {
            medication.Image = selectedImage;
        }

        // تنظیم زمان‌های یادآوری
        medication.ReminderTimes = new List<DateTime>(tempReminderTimes);

        // تنظیم روزانه بودن
        medication.IsDaily = dailyToggle.isOn;

        // تنظیم روزهای هفته
        if (!medication.IsDaily)
        {
            medication.DaysOfWeek = new List<DayOfWeek>();

            // بررسی روزهای انتخاب شده
            foreach (Toggle dayToggle in daysOfWeekToggleGroup.GetComponentsInChildren<Toggle>())
            {
                if (dayToggle.isOn)
                {
                    int dayIndex = dayToggle.transform.GetSiblingIndex();
                    medication.DaysOfWeek.Add((DayOfWeek)dayIndex);
                }
            }
        }

        // تنظیم اطلاعات مخاطب اضطراری
        if (!string.IsNullOrEmpty(contactNameInput.text))
        {
            ContactInfo contact = new ContactInfo(contactNameInput.text);
            contact.PhoneNumber = phoneNumberInput.text;
            contact.TelegramId = telegramIdInput.text;
            contact.WhatsAppNumber = whatsappInput.text;
            contact.Email = emailInput.text;

            medication.EmergencyContact = contact;
        }

        // ذخیره دارو در مدیریت یادآوری
        if (currentMedication == null)
        {
            ReminderManager.Instance.AddMedication(medication);
        }
        else
        {
            // در حالت ویرایش، باید یادآورها مجدداً تنظیم شوند
            ReminderManager.Instance.ScheduleAllReminders();
            ReminderManager.Instance.SaveMedications();
        }

        // بروزرسانی UI
        LoadMedicationsToUI();

        // بستن پنل
        addMedicationPanel.SetActive(false);
    }

    // لغو اضافه کردن دارو
    private void CancelAddingMedication()
    {
        addMedicationPanel.SetActive(false);
    }

    // نمایش پیام خطا
    private void ShowErrorMessage(string message)
    {
        // در پیاده‌سازی واقعی، یک پنل پیام خطا نمایش دهید
        Debug.LogError(message);
    }

    // بارگذاری داروها در UI
    private void LoadMedicationsToUI()
    {
        // پاک کردن لیست فعلی
        foreach (Transform child in medicationListContainer)
        {
            Destroy(child.gameObject);
        }

        // افزودن همه داروها به لیست
        foreach (Medication medication in GetMedicationList())
        {
            GameObject item = Instantiate(medicationListItemPrefab, medicationListContainer);
            MedicationListItem listItem = item.GetComponent<MedicationListItem>();
            if (listItem != null)
            {
                listItem.Initialize(medication, this);
            }
        }
    }

    // دریافت لیست داروها از مدیریت یادآوری
    private List<Medication> GetMedicationList()
    {
        // این متد باید از ReminderManager لیست داروها را بگیرد
        // برای رعایت اصول شی‌گرایی، بهتر است ReminderManager یک متد عمومی برای دسترسی به لیست داروها فراهم کند

        // در اینجا برای نمونه یک لیست موقت برمی‌گردانیم
        return new List<Medication>();
    }

    // ویرایش دارو
    public void EditMedication(Medication medication)
    {
        // تنظیم داروی فعلی
        currentMedication = medication;

        // پر کردن فیلدها با اطلاعات دارو
        nameInput.text = medication.Name;
        descriptionInput.text = medication.Description;
        medicationTypeDropdown.value = (int)medication.Type;

        if (medication.Type == MedicationType.Pill || medication.Type == MedicationType.Capsule)
        {
            quantityInput.text = medication.Quantity.ToString();
            dosageInput.text = medication.DosagePerTime.ToString();
        }

        // تنظیم تصویر
        if (medication.Image != null)
        {
            medicationImagePreview.sprite = medication.Image;
            selectedImage = medication.Image;
        }
        else
        {
            medicationImagePreview.sprite = null;
            selectedImage = null;
        }

        // تنظیم زمان‌های یادآوری
        tempReminderTimes = new List<DateTime>(medication.ReminderTimes);

        // پاک کردن لیست زمان‌های نمایش داده شده
        foreach (Transform child in reminderTimesContainer)
        {
            Destroy(child.gameObject);
        }

        // نمایش زمان‌های یادآوری
        for (int i = 0; i < tempReminderTimes.Count; i++)
        {
            DateTime reminderTime = tempReminderTimes[i];

            GameObject reminderTimeItem = Instantiate(reminderTimeItemPrefab, reminderTimesContainer);
            TMP_Text timeText = reminderTimeItem.GetComponentInChildren<TMP_Text>();
            timeText.text = reminderTime.ToString("HH:mm");

            // دکمه حذف
            Button deleteButton = reminderTimeItem.GetComponentInChildren<Button>();
            int index = i;
            deleteButton.onClick.AddListener(() => RemoveReminderTime(index, reminderTimeItem));
        }

        // تنظیم روزانه بودن
        dailyToggle.isOn = medication.IsDaily;

        // تنظیم روزهای هفته
        if (!medication.IsDaily && medication.DaysOfWeek != null)
        {
            Toggle[] dayToggles = daysOfWeekToggleGroup.GetComponentsInChildren<Toggle>();
            for (int i = 0; i < dayToggles.Length; i++)
            {
                dayToggles[i].isOn = medication.DaysOfWeek.Contains((DayOfWeek)i);
            }
        }

        // تنظیم اطلاعات مخاطب اضطراری
        if (medication.EmergencyContact != null)
        {
            contactNameInput.text = medication.EmergencyContact.Name;
            phoneNumberInput.text = medication.EmergencyContact.PhoneNumber;
            telegramIdInput.text = medication.EmergencyContact.TelegramId;
            whatsappInput.text = medication.EmergencyContact.WhatsAppNumber;
            emailInput.text = medication.EmergencyContact.Email;
        }
        else
        {
            contactNameInput.text = "";
            phoneNumberInput.text = "";
            telegramIdInput.text = "";
            whatsappInput.text = "";
            emailInput.text = "";
        }

        // نمایش پنل
        addMedicationPanel.SetActive(true);
    }

    // حذف دارو
    public void DeleteMedication(string medicationId)
    {
        // درخواست تأیید از کاربر
        // در پیاده‌سازی واقعی، یک پنجره تأیید نمایش دهید

        // حذف دارو
        ReminderManager.Instance.RemoveMedication(medicationId);

        // بروزرسانی UI
        LoadMedicationsToUI();
    }
}