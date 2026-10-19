namespace GymSys.Core.Entities;

public class Trainer
{
    public int TrainerId { get; set; }

    /// <summary>Akun login instruktur; null bila instruktur belum punya akun.</summary>
    public int? UserId { get; set; }

    public string Nama { get; set; } = "";
    public string? NoTelepon { get; set; }
    public string? Keahlian { get; set; }
    public bool IsActive { get; set; } = true;

    public User? User { get; set; }
    public ICollection<PTAssignment> Assignments { get; set; } = [];
}
