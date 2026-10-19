namespace GymSys.Core.Entities;

public class Member
{
    public int MemberId { get; set; }
    public string KodeMember { get; set; } = "";
    public string NamaLengkap { get; set; } = "";
    public string NoTelepon { get; set; } = "";
    public string? Email { get; set; }
    public string? Alamat { get; set; }
    public DateOnly? TglLahir { get; set; }

    /// <summary>'L' atau 'P'.</summary>
    public string? JenisKelamin { get; set; }

    public string? KontakDarurat { get; set; }
    public string? CatatanKesehatan { get; set; }
    public DateOnly TglBergabung { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<PTAssignment> PTAssignments { get; set; } = [];
}
