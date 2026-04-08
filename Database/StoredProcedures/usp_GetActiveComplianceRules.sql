CREATE PROCEDURE [dbo].[usp_GetActiveComplianceRules]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT RuleId, Name, Category, Expression, Severity, IsActive
    FROM ComplianceRules
    WHERE IsActive = 1
    ORDER BY Severity DESC, Name;
END
