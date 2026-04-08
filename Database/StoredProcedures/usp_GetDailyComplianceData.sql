CREATE PROCEDURE [dbo].[usp_GetDailyComplianceData]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        d.DocumentId,
        d.FileName,
        d.DocumentType,
        d.ReceivedDate,
        d.ProcessedDate,
        CASE 
            WHEN EXISTS (
                SELECT 1 FROM ComplianceChecks cc 
                WHERE cc.DocumentId = d.DocumentId AND cc.Result = 'FAIL'
            ) THEN 'Failed'
            ELSE 'Passed'
        END AS Status
    FROM Documents d
    WHERE CAST(d.ReceivedDate AS DATE) = CAST(GETDATE() AS DATE)
    ORDER BY d.ReceivedDate DESC;
END
