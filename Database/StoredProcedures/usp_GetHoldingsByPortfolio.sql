CREATE PROCEDURE [dbo].[usp_GetHoldingsByPortfolio]
    @PortfolioId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        HoldingId,
        PortfolioId,
        SecurityId,
        Quantity,
        CostBasis,
        AsOfDate
    FROM Holdings
    WHERE PortfolioId = @PortfolioId
    ORDER BY SecurityId;
END
