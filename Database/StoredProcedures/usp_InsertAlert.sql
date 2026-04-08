CREATE PROCEDURE [dbo].[usp_InsertAlert]
    @PortfolioId INT,
    @AlertType NVARCHAR(50),
    @Message NVARCHAR(MAX),
    @CreatedDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO Alerts (PortfolioId, AlertType, Message, CreatedDate)
    VALUES (@PortfolioId, @AlertType, @Message, @CreatedDate);
    
    SELECT SCOPE_IDENTITY() AS AlertId;
END
