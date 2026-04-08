-- Meridian Capital Database Schema

CREATE TABLE Documents (
    DocumentId INT PRIMARY KEY IDENTITY(1,1),
    FileName NVARCHAR(255) NOT NULL,
    DocumentType NVARCHAR(50) NOT NULL,
    ReceivedDate DATETIME NOT NULL,
    ProcessedDate DATETIME NULL,
    Status NVARCHAR(50) NOT NULL,
    ClientId INT NOT NULL,
    ExtractedData NVARCHAR(MAX) NULL
);

CREATE TABLE ComplianceRules (
    RuleId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    Expression NVARCHAR(500) NOT NULL,
    Severity NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE ComplianceChecks (
    CheckId INT PRIMARY KEY IDENTITY(1,1),
    DocumentId INT NOT NULL,
    RuleId INT NOT NULL,
    Result NVARCHAR(50) NOT NULL,
    Details NVARCHAR(MAX) NULL,
    CheckedDate DATETIME NOT NULL,
    FOREIGN KEY (DocumentId) REFERENCES Documents(DocumentId),
    FOREIGN KEY (RuleId) REFERENCES ComplianceRules(RuleId)
);

CREATE TABLE Portfolios (
    PortfolioId INT PRIMARY KEY IDENTITY(1,1),
    ClientId INT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Benchmark NVARCHAR(100) NOT NULL,
    InceptionDate DATE NOT NULL,
    Currency NVARCHAR(10) NOT NULL
);

CREATE TABLE Holdings (
    HoldingId INT PRIMARY KEY IDENTITY(1,1),
    PortfolioId INT NOT NULL,
    SecurityId INT NOT NULL,
    Quantity DECIMAL(18, 4) NOT NULL,
    CostBasis DECIMAL(18, 2) NOT NULL,
    AsOfDate DATE NOT NULL,
    FOREIGN KEY (PortfolioId) REFERENCES Portfolios(PortfolioId)
);

CREATE TABLE Valuations (
    ValuationId INT PRIMARY KEY IDENTITY(1,1),
    PortfolioId INT NOT NULL,
    AsOfDate DATETIME NOT NULL,
    NAV DECIMAL(18, 2) NOT NULL,
    DailyReturn DECIMAL(5, 2) NOT NULL,
    MTDReturn DECIMAL(5, 2) NOT NULL,
    YTDReturn DECIMAL(5, 2) NOT NULL,
    FOREIGN KEY (PortfolioId) REFERENCES Portfolios(PortfolioId)
);

CREATE TABLE MarketData (
    SecurityId INT NOT NULL,
    Date DATE NOT NULL,
    ClosePrice DECIMAL(18, 4) NOT NULL,
    Volume BIGINT NOT NULL,
    Source NVARCHAR(50) NOT NULL,
    PRIMARY KEY (SecurityId, Date)
);

CREATE TABLE Alerts (
    AlertId INT PRIMARY KEY IDENTITY(1,1),
    PortfolioId INT NOT NULL,
    AlertType NVARCHAR(50) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    CreatedDate DATETIME NOT NULL,
    AcknowledgedDate DATETIME NULL,
    FOREIGN KEY (PortfolioId) REFERENCES Portfolios(PortfolioId)
);

-- Sample data
INSERT INTO ComplianceRules (Name, Category, Expression, Severity, IsActive)
VALUES 
    ('Document Length Check', 'Format', 'LENGTH>100', 'Low', 1),
    ('Account Number Present', 'Content', 'CONTAINS:Account', 'High', 1),
    ('Date Validation', 'Content', 'CONTAINS:2024', 'Medium', 1);

INSERT INTO Portfolios (ClientId, Name, Benchmark, InceptionDate, Currency)
VALUES 
    (1, 'Growth Portfolio Alpha', 'S&P 500', '2020-01-01', 'USD'),
    (2, 'Conservative Income Fund', 'Barclays Agg', '2019-06-15', 'USD'),
    (3, 'International Equity', 'MSCI World', '2021-03-01', 'USD');

INSERT INTO Holdings (PortfolioId, SecurityId, Quantity, CostBasis, AsOfDate)
VALUES 
    (1, 1, 1000, 95000.00, '2024-01-01'),
    (1, 2, 500, 52000.00, '2024-01-01'),
    (2, 3, 2000, 210000.00, '2024-01-01'),
    (3, 4, 750, 78000.00, '2024-01-01');

INSERT INTO MarketData (SecurityId, Date, ClosePrice, Volume, Source)
VALUES 
    (1, CAST(GETDATE() AS DATE), 105.50, 1500000, 'BLOOMBERG'),
    (2, CAST(GETDATE() AS DATE), 112.75, 850000, 'BLOOMBERG'),
    (3, CAST(GETDATE() AS DATE), 108.25, 2100000, 'BLOOMBERG'),
    (4, CAST(GETDATE() AS DATE), 98.90, 650000, 'BLOOMBERG');
