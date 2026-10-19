using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.App.Services;
using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.App.ViewModels;

/// <summary>Satu batang pada grafik tren. BarHeight sudah dalam piksel (maks 150).</summary>
public sealed record BarItem(string Label, int Value, double BarHeight);

public partial class DashboardViewModel : ObservableObject, IAsyncInitializable
{
    private const double MaxBarHeight = 150;
    private static readonly CultureInfo IdCulture = CultureInfo.GetCultureInfo("id-ID");

    private readonly IDashboardQueries _queries;

    public DashboardViewModel(IDashboardQueries queries, ICurrentUserService currentUser)
    {
        _queries = queries;
        // PRD Bagian 5.1: kartu pendapatan hanya untuk Admin.
        ShowRevenue = currentUser.User?.Role == UserRole.Admin;
    }

    public bool ShowRevenue { get; }

    public ObservableCollection<BarItem> Trend { get; } = new();
    public ObservableCollection<ExpiringMemberDto> Expiring { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private int _totalMemberAktif;

    [ObservableProperty]
    private int _memberBaruBulanIni;

    [ObservableProperty]
    private int _akanHabis7Hari;

    [ObservableProperty]
    private string _pendapatanText = "-";

    [ObservableProperty]
    private bool _isExpiringEmpty;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var summaryTask = _queries.GetSummaryAsync();
            var trendTask = _queries.GetRegistrationTrendAsync(12);
            var expiringTask = _queries.GetExpiringMembersAsync(7);
            await Task.WhenAll(summaryTask, trendTask, expiringTask);

            var summary = await summaryTask;
            TotalMemberAktif = summary.TotalMemberAktif;
            MemberBaruBulanIni = summary.MemberBaruBulanIni;
            AkanHabis7Hari = summary.AkanHabis7Hari;
            PendapatanText = summary.PendapatanBulanIni.ToString("C0", IdCulture);

            var trend = await trendTask;
            var max = Math.Max(1, trend.Count == 0 ? 1 : trend.Max(t => t.Count));
            Trend.Clear();
            foreach (var point in trend)
            {
                var height = point.Count == 0 ? 0 : Math.Max(4, point.Count / (double)max * MaxBarHeight);
                Trend.Add(new BarItem(point.Label, point.Count, height));
            }

            var expiring = await expiringTask;
            Expiring.Clear();
            foreach (var member in expiring)
                Expiring.Add(member);
            IsExpiringEmpty = Expiring.Count == 0;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "DashboardViewModel");
            ErrorMessage = "Gagal memuat data dashboard. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task RetryAsync() => InitializeAsync();
}
