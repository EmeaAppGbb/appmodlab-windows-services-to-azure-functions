CREATE PROCEDURE [dbo].[usp_InsertDocument]
    @FileName NVARCHAR(255),
    @DocumentType NVARCHAR(50),
    @ReceivedDate DATETIME,
    @Status NVARCHAR(50),
    @ClientId INT,
    @ExtractedData NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO Documents (FileName, DocumentType, ReceivedDate, Status, ClientId, ExtractedData)
    VALUES (@FileName, @DocumentType, @ReceivedDate, @Status, @ClientId, @ExtractedData);
    
    SELECT SCOPE_IDENTITY() AS DocumentId;
END
