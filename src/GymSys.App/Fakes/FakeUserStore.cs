using GymSys.Core.Entities;

namespace GymSys.App.Fakes;

/// <summary>
/// "Database" akun di memori yang dipakai bersama FakeAuthService dan FakeAccountService,
/// jadi ganti password / tambah staf langsung berlaku saat login berikutnya.
/// Password di sini teks biasa HANYA karena ini data palsu; versi asli wajib hash PBKDF2.
/// </summary>
public sealed class FakeUserStore
{
    public sealed class Entry
    {
        public int UserId { get; init; }
        public string Username { get; init; } = "";
        public string Password { get; set; } = "";
        public string NamaLengkap { get; init; } = "";
        public UserRole Role { get; init; }
        public bool IsActive { get; set; } = true;
    }

    private readonly List<Entry> _users = new()
    {
        new Entry { UserId = 1, Username = "admin", Password = "admin123", NamaLengkap = "Pemilik Gym", Role = UserRole.Admin },
        new Entry { UserId = 2, Username = "resepsionis", Password = "resep123", NamaLengkap = "Rina Wulandari", Role = UserRole.Resepsionis },
        new Entry { UserId = 3, Username = "pt", Password = "pt123", NamaLengkap = "Bima Saputra", Role = UserRole.Instruktur },
    };

    private int _nextId = 4;

    public IReadOnlyList<Entry> All => _users;

    public Entry? FindByUsername(string username)
        => _users.FirstOrDefault(u => string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));

    public Entry? FindById(int userId) => _users.FirstOrDefault(u => u.UserId == userId);

    public Entry Add(string username, string namaLengkap, UserRole role, string password)
    {
        var entry = new Entry
        {
            UserId = _nextId++,
            Username = username.Trim(),
            NamaLengkap = namaLengkap.Trim(),
            Role = role,
            Password = password
        };
        _users.Add(entry);
        return entry;
    }
}
