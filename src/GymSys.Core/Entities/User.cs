namespace GymSys.Core.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];
    public UserRole Role { get; set; }
    public string NamaLengkap { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string Theme { get; set; } = "Dark";
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }

    /// <summary>Wajib ganti password saat login berikutnya (akun admin hasil seed).</summary>
    public bool MustChangePassword { get; set; }

    public Trainer? Trainer { get; set; }
}
