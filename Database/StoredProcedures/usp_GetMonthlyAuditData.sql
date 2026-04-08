CREATE PROCEDURE [dbo].[usp_GetMonthlyAuditData]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        d.DocumentId,
        d.FileName,
        d.DocumentType,
        d.ReceivedDate,
        d.ProcessedDate,
        cc.RuleId,
        cr.Name AS RuleName,
        cc.Result,
        cc.Details,
        cc.CheckedDate
    FROM Documents d
    INNER JOIN ComplianceChecks cc ON d.DocumentId = cc.DocumentId
    INNER JOIN ComplianceRules cr ON cc.RuleId = cr.RuleId
    WHERE MONTH(d.ReceivedDate) = MONTH(GETDATE())
      AND YEAR(d.ReceivedDate) = YEAR(GETDATE())
    ORDER BY d.ReceivedDate DESC, cc.CheckedDate DESC;
END
