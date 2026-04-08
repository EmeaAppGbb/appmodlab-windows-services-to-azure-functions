namespace Meridian.Shared.Data
{
    public static class StoredProcedures
    {
        public const string InsertDocument = "usp_InsertDocument";
        public const string UpdateDocumentStatus = "usp_UpdateDocumentStatus";
        public const string GetDocumentById = "usp_GetDocumentById";
        public const string GetPendingDocuments = "usp_GetPendingDocuments";
        
        public const string InsertComplianceCheck = "usp_InsertComplianceCheck";
        public const string GetActiveComplianceRules = "usp_GetActiveComplianceRules";
        public const string GetComplianceChecksByDocument = "usp_GetComplianceChecksByDocument";
        
        public const string GetDailyComplianceData = "usp_GetDailyComplianceData";
        public const string GetWeeklyRiskData = "usp_GetWeeklyRiskData";
        public const string GetMonthlyAuditData = "usp_GetMonthlyAuditData";
        
        public const string GetPortfolios = "usp_GetPortfolios";
        public const string GetHoldingsByPortfolio = "usp_GetHoldingsByPortfolio";
        public const string GetLatestMarketData = "usp_GetLatestMarketData";
        public const string InsertValuation = "usp_InsertValuation";
        public const string InsertMarketData = "usp_InsertMarketData";
        public const string InsertAlert = "usp_InsertAlert";
    }
}
