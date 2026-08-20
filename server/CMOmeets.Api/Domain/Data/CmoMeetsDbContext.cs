using CMOmeets.Api.Domain.Entities;
using CMOmeets.Domain.Entities;
using CMOmeets.Domain.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Data;

public partial class CmoMeetsDbContext : IdentityDbContext<AppUser>
{
    public CmoMeetsDbContext(DbContextOptions<CmoMeetsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DepartmentMa> DepartmentMas { get; set; }

    public virtual DbSet<DesignationMa> DesignationMas { get; set; }

    public virtual DbSet<LoginHistoryPwd> LoginHistoryPwds { get; set; }

    public virtual DbSet<MasDeptDesignation> MasDeptDesignations { get; set; }

    public virtual DbSet<MasterDistrict> MasterDistricts { get; set; }

    public virtual DbSet<MinistryMa> MinistryMas { get; set; }

    public virtual DbSet<TbMeetingAgenda> TbMeetingAgendas { get; set; }

    public virtual DbSet<TbMeetingGroup> TbMeetingGroups { get; set; }

    public virtual DbSet<TbMeetingMappedGroup> TbMeetingMappedGroups { get; set; }

    public virtual DbSet<TbMeetingMember> TbMeetingMembers { get; set; }
    public virtual DbSet<TbMeetingLog> TbMeetingLogs { get; set; }

    public virtual DbSet<TbMeetingSchedule> TbMeetingSchedules { get; set; }

    public virtual DbSet<TbRemarksOnAgenda> TbRemarksOnAgendas { get; set; }

    public virtual DbSet<TbActionPointView> TbActionPointViews { get; set; }

    public virtual DbSet<TblOfficer> TblOfficers { get; set; }

    public virtual DbSet<TblOfficerDepartment> TblOfficerDepartments { get; set; }

    public virtual DbSet<TblOfficerDesignation> TblOfficerDesignations { get; set; }

    public virtual DbSet<UserAuthenticationDetail> UserAuthenticationDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DepartmentMa>(entity =>
        {
            entity.HasKey(e => e.Rid);

            entity.ToTable("departmentMas");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .IsFixedLength()
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("createdBy");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("departmentName");
            entity.Property(e => e.DepartmentNameHin)
                .HasMaxLength(250)
                .HasColumnName("departmentNameHin");
            entity.Property(e => e.MinistryId).HasColumnName("ministryID");

            entity.HasOne(d => d.Ministry).WithMany(p => p.DepartmentMas)
                .HasForeignKey(d => d.MinistryId)
                .HasConstraintName("FK_departmentMas_ministry");
        });

        modelBuilder.Entity<DesignationMa>(entity =>
        {
            entity.HasKey(e => e.Rid);

            entity.ToTable("designationMas");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .IsFixedLength()
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("createdBy");
            entity.Property(e => e.DesignationName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("designationName");
            entity.Property(e => e.MobileNo)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("mobileNo");
            entity.Property(e => e.OfficerName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("officerName");
            entity.Property(e => e.SeqNo)
                .HasDefaultValue(9999)
                .HasColumnName("seqNo");
        });

        modelBuilder.Entity<LoginHistoryPwd>(entity =>
        {
            entity.HasKey(e => e.Rid);

            entity.ToTable("login_history_pwd");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.AuthId)
                .HasMaxLength(50)
                .HasColumnName("AuthID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IpAdd)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ip_add");
            entity.Property(e => e.LastLogin)
                .HasColumnType("datetime")
                .HasColumnName("last_login");
            entity.Property(e => e.LogoutTime)
                .HasColumnType("datetime")
                .HasColumnName("logout_time");
            entity.Property(e => e.SessionId)
                .HasMaxLength(350)
                .HasColumnName("session_id");
            entity.Property(e => e.UserType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("userType");
        });

        modelBuilder.Entity<MasDeptDesignation>(entity =>
        {
            entity.HasKey(e => e.Rid).HasName("PK_MAS_DeptDesignation_RID");

            entity.ToTable("MAS_DeptDesignation");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .IsFixedLength()
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("createdBy");
            entity.Property(e => e.DeptId).HasColumnName("deptID");
            entity.Property(e => e.DesigName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("desigName");
            entity.Property(e => e.SeqNo)
                .HasDefaultValue(9)
                .HasColumnName("seqNo");

            entity.HasOne(d => d.Dept).WithMany(p => p.MasDeptDesignations)
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_deptDesig_department");
        });

        modelBuilder.Entity<MasterDistrict>(entity =>
        {
            entity.HasKey(e => e.DCode);

            entity.ToTable("Master_District");

            entity.Property(e => e.DCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("dCode");
            entity.Property(e => e.DName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("dName");
            entity.Property(e => e.IsActive)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("isActive");
        });

        modelBuilder.Entity<MinistryMa>(entity =>
        {
            entity.HasKey(e => e.Rid);

            entity.ToTable("ministryMas");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.MinistryName)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("ministryName");
        });

        modelBuilder.Entity<TbMeetingAgenda>(entity =>
        {
            entity.HasKey(e => e.Rid).HasName("PK__tb_meeti__CAFF413277474227");

            entity.ToTable("tb_meetingAgendas");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .IsFixedLength()
                .HasColumnName("active");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("addedAt");
            entity.Property(e => e.AddedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("addedBy");
            entity.Property(e => e.AgendaDueDt).HasColumnName("agendaDueDt");
            entity.Property(e => e.AgendaMembers)
                .IsUnicode(false)
                .HasColumnName("agendaMembers");
            entity.Property(e => e.AgendaStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("InProgress")
                .HasColumnName("agendaStatus");
            entity.Property(e => e.DistrictName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("State")
                .HasColumnName("districtName");
            entity.Property(e => e.MeetingAgenda).HasColumnName("meetingAgenda");
            entity.Property(e => e.MeetingRid).HasColumnName("meetingRID");
            entity.Property(e => e.MemberRids)
                .IsUnicode(false)
                .HasColumnName("memberRIDs");
            // Admin follow-up on the concerned officer. Existing points default to "not called".
            entity.Property(e => e.IsOfficerCalled)
                .HasDefaultValue(false)
                .HasColumnName("isOfficerCalled");
            entity.Property(e => e.OfficerRemark)
                .IsUnicode(false)
                .HasColumnName("officerRemark");

            // The meetings list, dashboard and reports all fetch a meeting's action points by meetingRID;
            // without this the FK column is unindexed and every such fetch scans all agendas.
            entity.HasIndex(e => e.MeetingRid);

            entity.HasOne(d => d.MeetingR).WithMany(p => p.TbMeetingAgenda)
                .HasForeignKey(d => d.MeetingRid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_agenda_schedule");
        });

        modelBuilder.Entity<TbMeetingGroup>(entity =>
        {
            entity.HasKey(e => e.Rid);

            entity.ToTable("tb_MeetingGroup");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.Active)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .HasColumnName("active");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("addedAt");
            entity.Property(e => e.AddedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("addedBy");
            entity.Property(e => e.GroupName)
                .HasMaxLength(350)
                .HasColumnName("groupName");
        });

        modelBuilder.Entity<TbMeetingMappedGroup>(entity =>
        {
            entity.HasKey(e => e.Rid).HasName("PK_tb_meetingMappedGroup_RID");

            entity.ToTable("tb_meetingMappedGroup");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.Active)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .HasColumnName("active");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("addedAt");
            entity.Property(e => e.AddedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("addedBy");
            entity.Property(e => e.GroupRid).HasColumnName("groupRID");
            entity.Property(e => e.MeetingRid).HasColumnName("meetingRID");

            entity.HasOne(d => d.GroupR).WithMany(p => p.TbMeetingMappedGroups)
                .HasForeignKey(d => d.GroupRid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mappedGroup_group");

            entity.HasOne(d => d.MeetingR).WithMany(p => p.TbMeetingMappedGroups)
                .HasForeignKey(d => d.MeetingRid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mappedGroup_schedule");
        });
        modelBuilder.Entity<TbMeetingLog>(entity =>
        {
            entity.HasKey(e => e.Rid);

            entity.ToTable("tb_meetingLogs");

            entity.Property(e => e.Rid)
                .HasColumnName("RID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.MeetingRid)
                .HasColumnName("meetingRID");

            entity.Property(e => e.MemberRid)
                .HasColumnName("memberRID");

            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("addedAt");

            entity.Property(e => e.DesignationId)
                .HasColumnName("DesignationId");

            entity.Property(e => e.DeletedAt)
                .HasColumnType("datetime")
                .HasColumnName("deletedAt");
        });

        modelBuilder.Entity<TbMeetingMember>(entity =>
        {
            // entity.HasKey(e => new { e.MeetingRid, e.MemberRid });
            entity.HasKey(e => e.Rid);
            entity.Property(e => e.Rid).HasColumnName("RID").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.MeetingRid);
            entity.ToTable("tb_meetingMembers");

            entity.Property(e => e.MeetingRid).HasColumnName("meetingRID");
            entity.Property(e => e.MemberRid).HasColumnName("memberRID");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("addedAt");

            entity.HasOne(d => d.MeetingR).WithMany(p => p.TbMeetingMembers)
                .HasForeignKey(d => d.MeetingRid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_members_schedule");

            entity.HasOne(d => d.MemberR).WithMany(p => p.TbMeetingMembers)
                .HasForeignKey(d => d.MemberRid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_members_officer");
        });

        modelBuilder.Entity<TbMeetingSchedule>(entity =>
        {
            entity.HasKey(e => e.Rid).HasName("PK__tb_meeti__CAFF4132428AB66A");

            entity.ToTable("tb_meetingSchedules");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.Active)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .HasColumnName("active");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("addedAt");
            entity.Property(e => e.AddedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("addedBy");
            entity.Property(e => e.MeetingDate)
                .HasColumnType("datetime")
                .HasColumnName("meetingDate");
            entity.Property(e => e.MeetingDocument)
                .IsUnicode(false)
                .HasColumnName("meetingDocument");
            entity.Property(e => e.MeetingPlace)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("meetingPlace");
            entity.Property(e => e.MeetingSubject)
                .HasMaxLength(500)
                .HasColumnName("meetingSubject");
        });

        modelBuilder.Entity<TbRemarksOnAgenda>(entity =>
        {
            entity.HasKey(e => e.Rid);

            entity.ToTable("tb_remarksOnAgendas");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("addedAt");
            entity.Property(e => e.AddedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("addedBy");
            entity.Property(e => e.AgendaDueDate).HasColumnName("agendaDueDate");
            entity.Property(e => e.AgendaRemarks).HasColumnName("agendaRemarks");
            entity.Property(e => e.AgendaRid).HasColumnName("agendaRID");
            entity.Property(e => e.AtrDoc)
                .HasMaxLength(350)
                .IsUnicode(false)
                .HasColumnName("atrDoc");
            entity.Property(e => e.MeetingRid).HasColumnName("meetingRID");
            entity.Property(e => e.MemberName)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("memberName");
            entity.Property(e => e.MemberRid).HasColumnName("memberRID");
            entity.Property(e => e.RemarkStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("remarkStatus");
            entity.Property(e => e.ProgressPercentage).HasColumnName("progressPercentage");
            entity.Property(e => e.RemarksDate).HasColumnName("remarksDate");
            entity.Property(e => e.ExplanationRequest).HasMaxLength(1000).HasColumnName("explanationRequest");
            entity.Property(e => e.ExplanationRequestedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("explanationRequestedBy");
            entity.Property(e => e.ExplanationRequestedAt).HasColumnType("datetime").HasColumnName("explanationRequestedAt");
            entity.Property(e => e.ExplanationText).HasMaxLength(2000).HasColumnName("explanationText");
            entity.Property(e => e.ExplanationDoc).HasMaxLength(350).IsUnicode(false).HasColumnName("explanationDoc");
            entity.Property(e => e.ExplainedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("explainedBy");
            entity.Property(e => e.ExplainedAt).HasColumnType("datetime").HasColumnName("explainedAt");

            // Hot path: the meetings list / dashboard / reports read each action point's LATEST ATR
            // progress via a per-agenda correlated subquery (WHERE agendaRID = @x ORDER BY RID DESC).
            // agendaRID was unindexed, forcing a full scan of tb_remarksOnAgendas per action point.
            // Index it (progressPercentage INCLUDEd so the lookup is covering).
            entity.HasIndex(e => e.AgendaRid).IncludeProperties(e => e.ProgressPercentage);

            entity.HasOne(d => d.AgendaR).WithMany(p => p.TbRemarksOnAgenda)
                .HasForeignKey(d => d.AgendaRid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_remarks_agenda");

            entity.HasOne(d => d.MeetingR).WithMany(p => p.TbRemarksOnAgenda)
                .HasForeignKey(d => d.MeetingRid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_remarks_schedule");
        });

        modelBuilder.Entity<TbActionPointView>(entity =>
        {
            entity.HasKey(e => e.Rid);

            entity.ToTable("tb_actionPointViews");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.AgendaRid).HasColumnName("agendaRID");
            entity.Property(e => e.DeptId).HasColumnName("deptID");
            entity.Property(e => e.FirstViewedAt).HasColumnType("datetime").HasColumnName("firstViewedAt");
            entity.Property(e => e.LastViewedAt).HasColumnType("datetime").HasColumnName("lastViewedAt");
            entity.Property(e => e.ViewedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("viewedBy");

            entity.HasIndex(e => new { e.AgendaRid, e.DeptId }).IsUnique();
        });

        modelBuilder.Entity<TblOfficer>(entity =>
        {
            entity.HasKey(e => e.Rid);

            entity.ToTable("tbl_Officers");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .IsFixedLength()
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("createdBy");
            entity.Property(e => e.DeptId).HasColumnName("deptID");
            entity.Property(e => e.DesigId).HasColumnName("desigID");
            entity.Property(e => e.OfficerMobile)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("officerMobile");
            entity.Property(e => e.OfficerEmail)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("officerEmail");
            entity.Property(e => e.OfficerName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("officerName");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updatedAt");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("updatedBy");

            entity.HasOne(d => d.Dept).WithMany(p => p.TblOfficers)
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_officers_department");

            entity.HasOne(d => d.Desig).WithMany(p => p.TblOfficers)
                .HasForeignKey(d => d.DesigId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_officers_deptDesig");
        });

        modelBuilder.Entity<TblOfficerDepartment>(entity =>
        {
            entity.HasKey(e => e.Rid);

            entity.ToTable("tb_officerDepartments");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.OfficerId).HasColumnName("officerID");
            entity.Property(e => e.DeptId).HasColumnName("deptID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .IsFixedLength()
                .HasColumnName("active");

            entity.HasIndex(e => new { e.OfficerId, e.DeptId }).IsUnique();

            entity.HasOne(d => d.Officer).WithMany(p => p.OfficerDepartments)
                .HasForeignKey(d => d.OfficerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_officerDept_officer");

            entity.HasOne(d => d.Dept).WithMany()
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_officerDept_department");
        });

        modelBuilder.Entity<TblOfficerDesignation>(entity =>
        {
            entity.HasKey(e => e.Rid);

            entity.ToTable("tb_officerDesignations");

            entity.Property(e => e.Rid).HasColumnName("RID");
            entity.Property(e => e.OfficerId).HasColumnName("officerID");
            entity.Property(e => e.DesigId).HasColumnName("desigID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .IsFixedLength()
                .HasColumnName("active");

            entity.HasIndex(e => new { e.OfficerId, e.DesigId }).IsUnique();

            entity.HasOne(d => d.Officer).WithMany(p => p.OfficerDesignations)
                .HasForeignKey(d => d.OfficerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_officerDesig_officer");

            entity.HasOne(d => d.Desig).WithMany()
                .HasForeignKey(d => d.DesigId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_officerDesig_designation");
        });

        modelBuilder.Entity<UserAuthenticationDetail>(entity =>
        {
            entity.HasKey(e => e.AuthId).HasName("PK__User_Aut__12C15D338E8E35CE");

            entity.ToTable("User_Authentication_Detail");

            entity.Property(e => e.AuthId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("AuthID");
            entity.Property(e => e.Active)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .IsFixedLength();
            entity.Property(e => e.AuthPwd)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Created_At");
            entity.Property(e => e.Designation)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LocationCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocationName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MobileNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.OfficialName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Rid)
                .ValueGeneratedOnAdd()
                .HasColumnName("RID");
            entity.Property(e => e.UserType)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
