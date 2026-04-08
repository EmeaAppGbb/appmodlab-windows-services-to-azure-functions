CREATE PROCEDURE [dbo].[usp_GetPortfolios]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        PortfolioId,
        ClientId,
        Name,
        Benchmark,
        InceptionDate,
        Currency
    FROM Portfolios
    ORDER BY Name;
END
