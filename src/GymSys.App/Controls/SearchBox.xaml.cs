using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GymSys.App.Controls;

public partial class SearchBox : UserControl
{
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchBox), new PropertyMetadata("Cari..."));

    /// <summary>Teks pencarian SETELAH debounce. Two-way by default.</summary>
    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(SearchBox),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSearchTextChanged));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SearchBox), new PropertyMetadata(null));

    public static readonly DependencyProperty DebounceMillisecondsProperty =
        DependencyProperty.Register(nameof(DebounceMilliseconds), typeof(int), typeof(SearchBox), new PropertyMetadata(300));

    public static readonly DependencyProperty MinLengthProperty =
        DependencyProperty.Register(nameof(MinLength), typeof(int), typeof(SearchBox), new PropertyMetadata(2));

    private readonly DispatcherTimer _timer = new();

    public SearchBox()
    {
        InitializeComponent();
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Commit();
        };
    }

    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public int DebounceMilliseconds { get => (int)GetValue(DebounceMillisecondsProperty); set => SetValue(DebounceMillisecondsProperty, value); }
    public int MinLength { get => (int)GetValue(MinLengthProperty); set => SetValue(MinLengthProperty, value); }

    /// <summary>Dipakai shortcut Ctrl+F di layar yang memuat kotak ini.</summary>
    public void FocusInput()
    {
        Input.Focus();
        Input.SelectAll();
    }

    private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (SearchBox)d;
        var value = (string?)e.NewValue ?? "";
        if (box.Input is not null && box.Input.Text != value)
            box.Input.Text = value;
    }

    private void OnInputChanged(object sender, TextChangedEventArgs e)
    {
        var hasText = Input.Text.Length > 0;
        ClearButton.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;
        PlaceholderText.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;

        _timer.Stop();
        _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(0, DebounceMilliseconds));
        _timer.Start();
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        Input.Clear();
        Commit();
        Input.Focus();
    }

    private void Commit()
    {
        var text = Input.Text.Trim();

        // 1 karakter belum dicari (hemat query); kosong = reset pencarian.
        if (text.Length > 0 && text.Length < MinLength)
            return;
        if (text == SearchText)
            return;

        SearchText = text;
        if (Command?.CanExecute(text) == true)
            Command.Execute(text);
    }
}
