using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMOmeets.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendaRemarkPerfIndexes : Migration
    {
        // Both of these indexes are in the EF model (tb_meetingAgendas.meetingRID and
        // tb_remarksOnAgendas.agendaRID come from the FK conventions; agendaRID now also INCLUDEs
        // progressPercentage) but were never materialised in the real database: InitialBaseline was
        // *baselined* against the legacy import (recorded as applied, never executed) and
        // db/cleanup_schema.sql added the FK CONSTRAINTS without backing indexes. A plain
        // CreateIndex/DropIndex (as EF scaffolds) would therefore try to drop a non-existent index and
        // fail, so we reconcile idempotently with guarded raw SQL. Safe on any target state (dev
        // localdb or production), and re-runnable.
        //
        // Why it matters: the meetings list and the action-points overview read each point's latest
        // ATR progress via a per-point correlated subquery over tb_remarksOnAgendas ordered by RID;
        // unindexed, that scanned the whole ATR table once per action point (~1674 x 1522 reads),
        // which can push the query past the SQL command timeout on a cold or loaded box.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tb_meetingAgendas_meetingRID' AND object_id = OBJECT_ID(N'dbo.tb_meetingAgendas'))
    CREATE INDEX [IX_tb_meetingAgendas_meetingRID] ON [dbo].[tb_meetingAgendas] ([meetingRID]);");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tb_remarksOnAgendas_agendaRID' AND object_id = OBJECT_ID(N'dbo.tb_remarksOnAgendas'))
    DROP INDEX [IX_tb_remarksOnAgendas_agendaRID] ON [dbo].[tb_remarksOnAgendas];
CREATE INDEX [IX_tb_remarksOnAgendas_agendaRID] ON [dbo].[tb_remarksOnAgendas] ([agendaRID]) INCLUDE ([progressPercentage]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tb_remarksOnAgendas_agendaRID' AND object_id = OBJECT_ID(N'dbo.tb_remarksOnAgendas'))
    DROP INDEX [IX_tb_remarksOnAgendas_agendaRID] ON [dbo].[tb_remarksOnAgendas];
CREATE INDEX [IX_tb_remarksOnAgendas_agendaRID] ON [dbo].[tb_remarksOnAgendas] ([agendaRID]);");
        }
    }
}
