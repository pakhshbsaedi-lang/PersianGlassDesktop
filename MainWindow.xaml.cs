using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PersianGlassDesktop;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _locked;
    private bool _pinned;
    private readonly PersianCalendar _persian = new();
    private readonly HijriCalendar _hijri = new();

    public MainWindow()
    {
        InitializeComponent();
        BuildCalendar();
        UpdateClock();
        _timer.Tick += (_, _) => UpdateClock();
        _timer.Start();
        ApplyAdaptiveTheme();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var tehran = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
        var iranNow = TimeZoneInfo.ConvertTime(now, tehran);

        ClockText.Text = iranNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        WeekdayText.Text = new PersianDateTimeFormat().DayName(iranNow.DayOfWeek);

        var py = _persian.GetYear(iranNow);
        var pm = _persian.GetMonth(iranNow);
        var pd = _persian.GetDayOfMonth(iranNow);
        JalaliText.Text = $"{ToPersianDigits(pd)} {PersianDateTimeFormat.MonthName(pm)} {ToPersianDigits(py)}";

        GregorianText.Text = iranNow.ToString("dddd, dd MMMM yyyy", CultureInfo.GetCultureInfo("en-US"));
        HijriText.Text = iranNow.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("ar-SA"));
    }

    private void BuildCalendar()
    {
        var localNow = DateTime.Now;
        var tehran = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
        var now = TimeZoneInfo.ConvertTime(localNow, tehran);
        var year = _persian.GetYear(now);
        var month = _persian.GetMonth(now);
        MonthText.Text = $"{PersianDateTimeFormat.MonthName(month)} {ToPersianDigits(year)}";

        string[] days = { "ش", "ی", "د", "س", "چ", "پ", "ج" };
        foreach (var day in days)
        {
            CalendarGrid.Children.Add(new TextBlock
            {
                Text = day, FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(151, 190, 238)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 7)
            });
        }

        var first = _persian.ToDateTime(year, month, 1, 0, 0, 0, 0);
        int offset = ((int)first.DayOfWeek + 1) % 7; // Saturday-first
        int daysInMonth = _persian.GetDaysInMonth(year, month);
        for (int i = 0; i < offset; i++) CalendarGrid.Children.Add(new Border());

        for (int day = 1; day <= daysInMonth; day++)
        {
            var cellDate = _persian.ToDateTime(year, month, day, 0, 0, 0, 0);
            var isToday = cellDate.Date == now.Date;
            var label = new TextBlock
            {
                Text = ToPersianDigits(day),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Foreground = Brushes.White
            };
            var border = new Border
            {
                Child = label,
                Height = 29,
                Margin = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Background = isToday ? new SolidColorBrush(Color.FromArgb(190, 46, 116, 190)) : Brushes.Transparent,
                BorderBrush = isToday ? Brushes.LightSkyBlue : Brushes.Transparent,
                BorderThickness = new Thickness(1),
                ToolTip = cellDate.ToString("yyyy-MM-dd")
            };
            CalendarGrid.Children.Add(border);
        }
    }

    private static string ToPersianDigits(int number) =>
        number.ToString(CultureInfo.InvariantCulture).Replace("0", "۰").Replace("1", "۱")
            .Replace("2", "۲").Replace("3", "۳").Replace("4", "۴").Replace("5", "۵")
            .Replace("6", "۶").Replace("7", "۷").Replace("8", "۸").Replace("9", "۹");

    private void ApplyAdaptiveTheme()
    {
        // Starter fallback: dark glass. Adaptive light/dark system-theme detection is planned for V1.1.
        RootBorder.Background = new SolidColorBrush(Color.FromArgb(224, 24, 35, 53));
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_locked && e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Lock_Click(object sender, RoutedEventArgs e)
    {
        _locked = !_locked;
        LockButton.Content = _locked ? "🔒" : "🔓";
        LockButton.ToolTip = _locked ? "قفل موقعیت فعال است" : "قفل موقعیت غیرفعال است";
    }

    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        _pinned = !_pinned;
        Topmost = _pinned;
        PinButton.Content = _pinned ? "📍" : "📌";
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "این نسخه، اسکلت اولیه رابط کاربری است. تنظیمات کامل، سرویس‌های آنلاین، ذخیره‌سازی آفلاین و نصب‌کننده در مراحل بعد تکمیل می‌شوند.",
            "PersianGlass Desktop", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

internal sealed class PersianDateTimeFormat
{
    public string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Saturday => "شنبه",
        DayOfWeek.Sunday => "یکشنبه",
        DayOfWeek.Monday => "دوشنبه",
        DayOfWeek.Tuesday => "سه‌شنبه",
        DayOfWeek.Wednesday => "چهارشنبه",
        DayOfWeek.Thursday => "پنجشنبه",
        _ => "جمعه"
    };

    public static string MonthName(int month) => month switch
    {
        1 => "فروردین", 2 => "اردیبهشت", 3 => "خرداد", 4 => "تیر",
        5 => "مرداد", 6 => "شهریور", 7 => "مهر", 8 => "آبان",
        9 => "آذر", 10 => "دی", 11 => "بهمن", _ => "اسفند"
    };
}
