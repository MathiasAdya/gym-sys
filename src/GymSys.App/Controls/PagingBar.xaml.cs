using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GymSys.App.Controls;

public partial class PagingBar : UserControl
{
    public static readonly DependencyProperty PageIndexProperty =
        DependencyProperty.Register(nameof(PageIndex), typeof(int), typeof(PagingBar),
            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnChanged));

    public static readonly DependencyProperty PageSizeProperty =
        DependencyProperty.Register(nameof(PageSize), typeof(int), typeof(PagingBar), new PropertyMetadata(50, OnChanged));

    public static readonly DependencyProperty TotalItemsProperty =
        DependencyProperty.Register(nameof(TotalItems), typeof(int), typeof(PagingBar), new PropertyMetadata(0, OnChanged));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(PagingBar), new PropertyMetadata(null));

    public PagingBar()
    {
        InitializeComponent();
        Refresh();
    }

    /// <summary>Nomor halaman saat ini, mulai dari 1.</summary>
    public int PageIndex { get => (int)GetValue(PageIndexProperty); set => SetValue(PageIndexProperty, value); }
    public int PageSize { get => (int)GetValue(PageSizeProperty); set => SetValue(PageSizeProperty, value); }
    public int TotalItems { get => (int)GetValue(TotalItemsProperty); set => SetValue(TotalItemsProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)Math.Max(1, PageSize)));

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PagingBar)d).Refresh();

    private void Refresh()
    {
        if (InfoText is null)
            return;

        var pages = TotalPages;
        var page = Math.Clamp(PageIndex, 1, pages);

        if (TotalItems == 0)
        {
            InfoText.Text = "Tidak ada data";
        }
        else
        {
            var size = Math.Max(1, PageSize);
            var from = (page - 1) * size + 1;
            var to = Math.Min(TotalItems, page * size);
            InfoText.Text = $"Menampilkan {from}-{to} dari {TotalItems} data  ·  Halaman {page}/{pages}";
        }

        FirstButton.IsEnabled = page > 1;
        PrevButton.IsEnabled = page > 1;
        NextButton.IsEnabled = page < pages;
        LastButton.IsEnabled = page < pages;
    }

    private void GoTo(int page)
    {
        page = Math.Clamp(page, 1, TotalPages);
        if (page == PageIndex)
            return;

        PageIndex = page;
        if (Command?.CanExecute(page) == true)
            Command.Execute(page);
    }

    private void OnFirst(object sender, RoutedEventArgs e) => GoTo(1);
    private void OnPrev(object sender, RoutedEventArgs e) => GoTo(PageIndex - 1);
    private void OnNext(object sender, RoutedEventArgs e) => GoTo(PageIndex + 1);
    private void OnLast(object sender, RoutedEventArgs e) => GoTo(TotalPages);
}
