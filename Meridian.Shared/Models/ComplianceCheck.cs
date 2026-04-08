using System;

namespace Meridian.Shared.Models
{
    public class ComplianceCheck
    {
        public int CheckId { get; set; }
        public int DocumentId { get; set; }
        public int RuleId { get; set; }
        public string Result { get; set; }
        public string Details { get; set; }
        public DateTime CheckedDate { get; set; }
    }

    public class ComplianceRule
    {
        public int RuleId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Expression { get; set; }
        public string Severity { get; set; }
        public bool IsActive { get; set; }
    }
}
