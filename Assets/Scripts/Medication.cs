using System;
using System.Collections.Generic;
using UnityEngine;

// کلاس مدیریت دارو - نگهداری اطلاعات هر دارو
public class Medication
{
    // مشخصات اصلی دارو
    public string Id { get; private set; }            // شناسه یکتا برای هر دارو
    public string Name { get; set; }                  // نام دارو
    public string Description { get; set; }           // توضیحات دارو
    public Sprite Image { get; set; }                 // تصویر دارو
    public MedicationType Type { get; set; }          // نوع دارو (قرص، کپسول، شربت و غیره)
    public int Quantity { get; set; }                 // تعداد باقیمانده (برای قرص و کپسول)
    public int InitialQuantity { get; set; }          // تعداد اولیه
    public int DosagePerTime { get; set; }            // تعداد مصرف در هر نوبت

    // اطلاعات زمان‌بندی مصرف
    public List<DateTime> ReminderTimes { get; set; } // زمان‌های یادآوری
    public bool IsDaily { get; set; }                 // آیا روزانه است؟
    public List<DayOfWeek> DaysOfWeek { get; set; }   // روزهای هفته (اگر روزانه نیست)

    // اطلاعات مخاطب برای اطلاع‌رسانی
    public ContactInfo EmergencyContact { get; set; } // اطلاعات تماس اضطراری

    // سازنده کلاس
    public Medication(string name, string description, MedicationType type, int quantity)
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
        Description = description;
        Type = type;

        // تنظیم تعداد فقط برای قرص و کپسول
        if (type == MedicationType.Pill || type == MedicationType.Capsule)
        {
            Quantity = quantity;
            InitialQuantity = quantity;
            DosagePerTime = 1; // مقدار پیش‌فرض
        }

        ReminderTimes = new List<DateTime>();
        DaysOfWeek = new List<DayOfWeek>();
        IsDaily = true; // به صورت پیش‌فرض روزانه است
    }

    // متد مصرف دارو - کم کردن از موجودی
    public void TakeDose()
    {
        if (Type == MedicationType.Pill || Type == MedicationType.Capsule)
        {
            if (Quantity >= DosagePerTime)
            {
                Quantity -= DosagePerTime;
                Debug.Log($"دارو {Name} مصرف شد. {Quantity} عدد باقی مانده است.");
            }
            else
            {
                Debug.LogWarning($"هشدار: تعداد داروی {Name} کم شده است. لطفاً داروی جدید تهیه کنید.");
            }
        }
    }

    // چک کردن آیا دارو در روز مشخص باید مصرف شود
    public bool ShouldTakeToday()
    {
        if (IsDaily) return true;

        DateTime today = DateTime.Now;
        return DaysOfWeek.Contains(today.DayOfWeek);
    }
}

// انواع دارو
public enum MedicationType
{
    Pill,       // قرص
    Capsule,    // کپسول
    Syrup,      // شربت
    Injection,  // تزریقی
    Inhaler,    // اسپری تنفسی
    Drops,      // قطره
    Cream,      // کرم
    Other       // سایر
}

// کلاس اطلاعات تماس اضطراری
public class ContactInfo
{
    public string Name { get; set; }           // نام مخاطب
    public string PhoneNumber { get; set; }    // شماره تلفن
    public string TelegramId { get; set; }     // آیدی تلگرام
    public string WhatsAppNumber { get; set; } // شماره واتس‌اپ
    public string Email { get; set; }          // آدرس ایمیل

    public ContactInfo(string name)
    {
        Name = name;
    }
}