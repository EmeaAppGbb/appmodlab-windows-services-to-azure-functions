CREATE PROCEDURE [dbo].[usp_InsertValuation]
    @PortfolioId INT,
    @AsOfDate DATETIME,
    @NAV DECIMAL(18, 2),
    @DailyReturn DECIMAL(5, 2),
    @MTDReturn DECIMAL(5, 2),
    @YTDReturn DECIMAL(5, 2)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO Valuations (PortfolioId, AsOfDate, NAV, DailyReturn, MTDReturn, YTDReturn)
    VALUES (@PortfolioId, @AsOfDate, @NAV, @DailyReturn, @MTDReturn, @YTDReturn);
    
    SELECT SCOPE_IDENTITY() AS ValuationId;
END
