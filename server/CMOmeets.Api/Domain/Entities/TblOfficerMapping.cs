namespace CMOmeets.Api.Domain.Entities
{
    public class TblOfficerMapping
    {
        public long Rid { get; set; }
        public int OfficerID { get; set; }
        public int DeptID { get; set; }
        public int? DesigID { get; set; }
        public string Active { get; set; } = "1";
        public string IsPrimary { get; set; } = "1";
        public DateTime EffectiveFrom { get; set; } = DateTime.Now;
        public DateTime? EffectiveTo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = null!;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
