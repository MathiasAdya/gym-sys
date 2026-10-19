namespace GymSys.App.ViewModels;

/// <summary>Layar sementara untuk menu yang belum dikerjakan. Hapus setelah View aslinya ada.</summary>
public sealed class PlaceholderViewModel
{
    public PlaceholderViewModel(string title, string owner)
    {
        Title = title;
        Owner = owner;
    }

    public string Title { get; }
    public string Owner { get; }
}
