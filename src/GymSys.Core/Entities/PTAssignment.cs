namespace GymSys.Core.Entities;

public class PTAssignment
{
    public int AssignmentId { get; set; }
    public int MemberId { get; set; }
    public int TrainerId { get; set; }
    public DateOnly TglMulai { get; set; }
    public DateOnly? TglSelesai { get; set; }
    public bool IsActive { get; set; } = true;

    public Member? Member { get; set; }
    public Trainer? Trainer { get; set; }
}
