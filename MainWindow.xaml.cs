using Microsoft.Win32;
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
    private readonly PersianCalendar _persian = new();
    private readonly HijriCalendar _hijri = new();
    private int _year, _month;
    private bool _locked, _pinned;

    public MainWindow()
    {
        InitializeComponent();
        var iranNow = IranNow();
        _year = _persian.GetYear(iranNow);
        _month = _persian.GetMonth(iranNow);
        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - ActualWidth - 14;
            Top = area.Top + 14;
        };
        UpdateClock();
        BuildCalendar();
        _timer.Tick += (_, _) => { UpdateClock(); UpdateProgress(); };
        _timer.Start();
    }

    private static DateTime IranNow()
    {
        try { return TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time")); }
        catch { return DateTime.Now; }
    }

    private void UpdateClock()
    {
        var now = IranNow();
        ClockText.Text = Digits(now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        WeekdayText.Text = DayName(now.DayOfWeek);
        JalaliText.Text = $"{Digits(_persian.GetDayOfMonth(now).ToString())} {MonthName(_persian.GetMonth(now))} {Digits(_persian.GetYear(now).ToString())}";
        GregorianText.Text = now.ToString("dddd, dd MMMM yyyy", CultureInfo.GetCultureInfo("en-US"));
        var hijri = new DateTime(_hijri.GetYear(now), _hijri.GetMonth(now), _hijri.GetDayOfMonth(now));
        HijriText.Text = $"قمری: {Digits(hijri.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))}";
        if (_year == 0) { _year = _persian.GetYear(now); _month = _persian.GetMonth(now); }
    }

    private void BuildCalendar()
    {
        CalendarGrid.Children.Clear();
        MonthText.Text = $"{MonthName(_month)} {Digits(_year.ToString())}";
        string[] days = { "ش", "ی", "د", "س", "چ", "پ", "ج" };
        foreach (var day in days)
            CalendarGrid.Children.Add(new TextBlock { Text = day, FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 210, 230)), HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 1, 0, 3) });

        var first = _persian.ToDateTime(_year, _month, 1, 0, 0, 0, 0);
        int offset = ((int)first.DayOfWeek + 1) % 7;
        for (int i = 0; i < offset; i++) CalendarGrid.Children.Add(new Border());
        int count = _persian.GetDaysInMonth(_year, _month);
        var today = IranNow();
        for (int day = 1; day <= count; day++)
        {
            var date = _persian.ToDateTime(_year, _month, day, 0, 0, 0, 0);
            bool isToday = date.Date == today.Date;
            var label = new TextBlock { Text = Digits(day.ToString()), FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Foreground = isToday ? Brushes.White : new SolidColorBrush(Color.FromRgb(235, 241, 250)) };
            CalendarGrid.Children.Add(new Border { Child = label, Height = 22, Margin = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Background = isToday ? new SolidColorBrush(Color.FromArgb(220, 33, 137, 164)) : Brushes.Transparent,
                ToolTip = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) });
        }
    }

    private void UpdateProgress()
    {
        var now = IranNow();
        int days = _persian.GetDaysInMonth(_persian.GetYear(now), _persian.GetMonth(now));
        double value = 100.0 * _persian.GetDayOfMonth(now) / days;
        MonthProgress.Value = value;
        ProgressText.Text = $"{Digits(_persian.GetDayOfMonth(now).ToString())} از {Digits(days.ToString())} روز ماه";
    }

    private void PreviousMonth_Click(object sender, RoutedEventArgs e)
    {
        if (--_month < 1) { _month = 12; _year--; }
        BuildCalendar();
    }
    private void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        if (++_month > 12) { _month = 1; _year++; }
        BuildCalendar();
    }
    private void Today_Click(object sender, RoutedEventArgs e)
    {
        var now = IranNow(); _year = _persian.GetYear(now); _month = _persian.GetMonth(now); BuildCalendar();
    }

    private static string Digits(string value) => value.Replace("0","۰").Replace("1","۱").Replace("2","۲")
        .Replace("3","۳").Replace("4","۴").Replace("5","۵").Replace("6","۶").Replace("7","۷").Replace("8","۸").Replace("9","۹");
    private static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Saturday => "شنبه", DayOfWeek.Sunday => "یکشنبه", DayOfWeek.Monday => "دوشنبه",
        DayOfWeek.Tuesday => "سه‌شنبه", DayOfWeek.Wednesday => "چهارشنبه", DayOfWeek.Thursday => "پنجشنبه", _ => "جمعه"
    };
    private static string MonthName(int month) => month switch
    {
        1=>"فروردین",2=>"اردیبهشت",3=>"خرداد",4=>"تیر",5=>"مرداد",6=>"شهریور",
        7=>"مهر",8=>"آبان",9=>"آذر",10=>"دی",11=>"بهمن",_=>"اسفند"
    };

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    { if (!_locked && e.LeftButton == MouseButtonState.Pressed) DragMove(); }
    private void Lock_Click(object sender, RoutedEventArgs e)
    { _locked = !_locked; LockButton.Content = _locked ? "🔒" : "🔓"; }
    private void Pin_Click(object sender, RoutedEventArgs e)
    { _pinned = !_pinned; Topmost = _pinned; PinButton.Content = _pinned ? "📍" : "📌"; }
    private void Minimize_Click(object sender, RoutedEventArgs e) => Hide();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show("اجرای خودکار هنگام ورود ویندوز فعال شود؟", "تنظیمات PersianGlass",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Cancel) return;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (answer == MessageBoxResult.Yes)
                key?.SetValue("PersianGlassDesktop", $"\"{Environment.ProcessPath}\"");
            else key?.DeleteValue("PersianGlassDesktop", false);
            StatusText.Text = answer == MessageBoxResult.Yes ? "اجرای خودکار فعال شد" : "اجرای خودکار غیرفعال شد";
        }
        catch (Exception ex) { MessageBox.Show($"تغییر تنظیمات ممکن نشد: {ex.Message}"); }
    }
}
