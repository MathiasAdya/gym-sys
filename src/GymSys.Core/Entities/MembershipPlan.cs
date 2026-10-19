namespace GymSys.Core.Entities;

public class MembershipPlan
{
    public int PlanId { get; set; }
    public string NamaPaket { get; set; } = "";
    public int DurasiHari { get; set; }
    public decimal Harga { get; set; }
    public string? Deskripsi { get; set; }
    public bool IsActive { get; set; } = true;
}
