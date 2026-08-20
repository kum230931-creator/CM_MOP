namespace CMOmeets.Api.Domain.Entities
{
    public class TbMeetingLog
    {
        public long Rid { get; set; }

        public int MeetingRid { get; set; }

        public int MemberRid { get; set; }

        public DateTime? AddedAt { get; set; }

        public int DesignationId { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
