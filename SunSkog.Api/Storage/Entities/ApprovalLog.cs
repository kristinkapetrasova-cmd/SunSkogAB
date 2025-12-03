using System;

namespace SunSkog.Api.Storage.Entities
{
    public class ApprovalLog
    {
        public Guid Id { get; set; }

        public Guid TimesheetId { get; set; }

        // Enum ApprovalAction je definovaný v ApprovalAction.cs
        public ApprovalAction Action { get; set; }

        // Identity používá string ID uživatele
        public string ByUserId { get; set; } = default!;

        public DateTime At { get; set; }

        public string? Note { get; set; }
    }
}