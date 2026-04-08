CREATE PROCEDURE [dbo].[usp_GetWeeklyRiskData]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        p.PortfolioId,
        p.Name AS PortfolioName,
        v.AsOfDate,
        v.NAV,
        v.DailyReturn,
        v.MTDReturn,
        v.YTDReturn
    FROM Portfolios p
    INNER JOIN Valuations v ON p.PortfolioId = v.PortfolioId
    WHERE v.AsOfDate >= DATEADD(DAY, -7, GETDATE())
    ORDER BY v.AsOfDate DESC, p.Name;
END
