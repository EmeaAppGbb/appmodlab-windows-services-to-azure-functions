CREATE PROCEDURE [dbo].[usp_GetLatestMarketData]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        SecurityId,
        Date,
        ClosePrice,
        Volume,
        Source
    FROM MarketData
    WHERE Date = (SELECT MAX(Date) FROM MarketData)
    ORDER BY SecurityId;
END
