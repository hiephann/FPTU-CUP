/* =====================================================================
   FPTU Cup – Hệ thống Quản lý Giải bóng đá Sinh viên
   Script SQL Server THAM CHIẾU, khớp ERD (docs/diagrams/erd.png).
   Dự án dùng EF Core Code First: CSDL thật nên tạo bằng Migrations.
   Script này dùng để đối chiếu cấu trúc bảng, hoặc dựng nhanh CSDL để test.
   ===================================================================== */

IF DB_ID(N'FptuCupDb') IS NULL
    CREATE DATABASE FptuCupDb;
GO
USE FptuCupDb;
GO

/* ---------- 01. Users ---------- */
CREATE TABLE dbo.Users (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username      NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
    PasswordHash  NVARCHAR(255) NOT NULL,
    FullName      NVARCHAR(100) NOT NULL,
    Email         NVARCHAR(100) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    Role          NVARCHAR(20)  NOT NULL
        CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Referee', N'TeamManager')),
    IsActive      BIT           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt     DATETIME2     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME())
);
GO

/* ---------- 02. Tournaments ---------- */
CREATE TABLE dbo.Tournaments (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tournaments PRIMARY KEY,
    Name          NVARCHAR(150) NOT NULL,
    Season        NVARCHAR(20)  NOT NULL,
    StartDate     DATE          NOT NULL,
    EndDate       DATE          NOT NULL,
    MaxTeams      INT           NOT NULL CONSTRAINT CK_Tournaments_MaxTeams CHECK (MaxTeams >= 2),
    NumberOfLegs  TINYINT       NOT NULL CONSTRAINT DF_Tournaments_Legs DEFAULT (2)
        CONSTRAINT CK_Tournaments_Legs CHECK (NumberOfLegs IN (1, 2)),
    Status        NVARCHAR(20)  NOT NULL CONSTRAINT DF_Tournaments_Status DEFAULT (N'Draft')
        CONSTRAINT CK_Tournaments_Status CHECK (Status IN (N'Draft', N'Registration', N'Ongoing', N'Finished')),
    CreatedBy     INT           NOT NULL CONSTRAINT FK_Tournaments_Users REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Tournaments_Dates CHECK (EndDate >= StartDate)
);
GO

/* ---------- 03. Teams ---------- */
CREATE TABLE dbo.Teams (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Teams PRIMARY KEY,
    TournamentId  INT           NOT NULL CONSTRAINT FK_Teams_Tournaments REFERENCES dbo.Tournaments(Id),
    ManagerId     INT           NOT NULL CONSTRAINT FK_Teams_Users REFERENCES dbo.Users(Id),
    Name          NVARCHAR(100) NOT NULL,
    Faculty       NVARCHAR(100) NULL,
    LogoUrl       NVARCHAR(255) NULL,
    KitColor      NVARCHAR(30)  NULL,
    Status        NVARCHAR(20)  NOT NULL CONSTRAINT DF_Teams_Status DEFAULT (N'Pending')
        CONSTRAINT CK_Teams_Status CHECK (Status IN (N'Pending', N'Approved', N'Rejected')),
    CONSTRAINT UQ_Teams_Tournament_Name UNIQUE (TournamentId, Name)
);
GO

/* ---------- 04. Players (7–14 cầu thủ/đội: kiểm tra ở Service – BR01) ---------- */
CREATE TABLE dbo.Players (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Players PRIMARY KEY,
    TeamId        INT           NOT NULL CONSTRAINT FK_Players_Teams REFERENCES dbo.Teams(Id) ON DELETE CASCADE,
    StudentCode   VARCHAR(20)   NOT NULL,
    FullName      NVARCHAR(100) NOT NULL,
    ShirtNumber   INT           NOT NULL CONSTRAINT CK_Players_Shirt CHECK (ShirtNumber BETWEEN 1 AND 99),
    Position      VARCHAR(2)    NOT NULL CONSTRAINT CK_Players_Position CHECK (Position IN ('GK', 'DF', 'MF', 'FW')),
    DateOfBirth   DATE          NULL,
    CONSTRAINT UQ_Players_Team_Shirt UNIQUE (TeamId, ShirtNumber)          -- BR03
);
GO
CREATE INDEX IX_Players_StudentCode ON dbo.Players(StudentCode);           -- BR02 kiểm tra ở Service
GO

/* ---------- 05. Venues ---------- */
CREATE TABLE dbo.Venues (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Venues PRIMARY KEY,
    Name          NVARCHAR(100) NOT NULL,
    Address       NVARCHAR(255) NULL,
    Capacity      INT           NULL
);
GO

/* ---------- 06. Matches ---------- */
CREATE TABLE dbo.Matches (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Matches PRIMARY KEY,
    TournamentId  INT           NOT NULL CONSTRAINT FK_Matches_Tournaments REFERENCES dbo.Tournaments(Id),
    Round         INT           NOT NULL CONSTRAINT CK_Matches_Round CHECK (Round >= 1),
    HomeTeamId    INT           NOT NULL CONSTRAINT FK_Matches_HomeTeam REFERENCES dbo.Teams(Id),
    AwayTeamId    INT           NOT NULL CONSTRAINT FK_Matches_AwayTeam REFERENCES dbo.Teams(Id),
    VenueId       INT           NULL     CONSTRAINT FK_Matches_Venues REFERENCES dbo.Venues(Id),
    RefereeId     INT           NULL     CONSTRAINT FK_Matches_Referee REFERENCES dbo.Users(Id),
    KickoffTime   DATETIME2     NOT NULL,
    HomeScore     INT           NOT NULL CONSTRAINT DF_Matches_HomeScore DEFAULT (0),
    AwayScore     INT           NOT NULL CONSTRAINT DF_Matches_AwayScore DEFAULT (0),
    Status        NVARCHAR(20)  NOT NULL CONSTRAINT DF_Matches_Status DEFAULT (N'Scheduled')
        CONSTRAINT CK_Matches_Status CHECK (Status IN (N'Scheduled', N'InProgress', N'Finished', N'Postponed', N'Cancelled')),
    UpdatedBy     INT           NULL     CONSTRAINT FK_Matches_UpdatedBy REFERENCES dbo.Users(Id),
    UpdatedAt     DATETIME2     NULL,
    CONSTRAINT CK_Matches_DiffTeams CHECK (HomeTeamId <> AwayTeamId)       -- BR06
);
GO
CREATE INDEX IX_Matches_Tournament_Round ON dbo.Matches(TournamentId, Round);
CREATE INDEX IX_Matches_KickoffTime      ON dbo.Matches(KickoffTime);
CREATE INDEX IX_Matches_HomeTeam         ON dbo.Matches(HomeTeamId);
CREATE INDEX IX_Matches_AwayTeam         ON dbo.Matches(AwayTeamId);
GO

/* ---------- 07. MatchLineups (khóa khi trận InProgress – BR19, BR20) ---------- */
CREATE TABLE dbo.MatchLineups (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MatchLineups PRIMARY KEY,
    MatchId       INT           NOT NULL CONSTRAINT FK_MatchLineups_Matches REFERENCES dbo.Matches(Id) ON DELETE CASCADE,
    TeamId        INT           NOT NULL CONSTRAINT FK_MatchLineups_Teams REFERENCES dbo.Teams(Id),
    PlayerId      INT           NOT NULL CONSTRAINT FK_MatchLineups_Players REFERENCES dbo.Players(Id),
    IsStarter     BIT           NOT NULL,
    CONSTRAINT UQ_MatchLineups_Match_Player UNIQUE (MatchId, PlayerId)
);
GO

/* ---------- 08. Substitutions (BR21) ---------- */
CREATE TABLE dbo.Substitutions (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Substitutions PRIMARY KEY,
    MatchId       INT           NOT NULL CONSTRAINT FK_Substitutions_Matches REFERENCES dbo.Matches(Id) ON DELETE CASCADE,
    TeamId        INT           NOT NULL CONSTRAINT FK_Substitutions_Teams REFERENCES dbo.Teams(Id),
    PlayerOutId   INT           NOT NULL CONSTRAINT FK_Substitutions_PlayerOut REFERENCES dbo.Players(Id),
    PlayerInId    INT           NOT NULL CONSTRAINT FK_Substitutions_PlayerIn REFERENCES dbo.Players(Id),
    Minute        INT           NOT NULL CONSTRAINT CK_Substitutions_Minute CHECK (Minute BETWEEN 1 AND 60),
    CONSTRAINT CK_Substitutions_DiffPlayers CHECK (PlayerOutId <> PlayerInId)
);
GO

/* ---------- 09. Goals (BR10, BR11, BR22) ---------- */
CREATE TABLE dbo.Goals (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Goals PRIMARY KEY,
    MatchId       INT           NOT NULL CONSTRAINT FK_Goals_Matches REFERENCES dbo.Matches(Id) ON DELETE CASCADE,
    PlayerId      INT           NOT NULL CONSTRAINT FK_Goals_Players REFERENCES dbo.Players(Id),
    TeamId        INT           NOT NULL CONSTRAINT FK_Goals_Teams REFERENCES dbo.Teams(Id),   -- đội được tính bàn
    Minute        INT           NOT NULL CONSTRAINT CK_Goals_Minute CHECK (Minute BETWEEN 1 AND 60),
    IsOwnGoal     BIT           NOT NULL CONSTRAINT DF_Goals_IsOwnGoal DEFAULT (0)
);
GO

/* ---------- 10. Cards (BR14) ---------- */
CREATE TABLE dbo.Cards (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cards PRIMARY KEY,
    MatchId       INT           NOT NULL CONSTRAINT FK_Cards_Matches REFERENCES dbo.Matches(Id) ON DELETE CASCADE,
    PlayerId      INT           NOT NULL CONSTRAINT FK_Cards_Players REFERENCES dbo.Players(Id),
    CardType      NVARCHAR(10)  NOT NULL CONSTRAINT CK_Cards_Type CHECK (CardType IN (N'Yellow', N'Red')),
    Minute        INT           NOT NULL CONSTRAINT CK_Cards_Minute CHECK (Minute BETWEEN 1 AND 60)
);
GO

/* ---------- 11. Suspensions (sinh khi kết thúc trận – BR14) ---------- */
CREATE TABLE dbo.Suspensions (
    Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suspensions PRIMARY KEY,
    PlayerId         INT           NOT NULL CONSTRAINT FK_Suspensions_Players REFERENCES dbo.Players(Id),
    TournamentId     INT           NOT NULL CONSTRAINT FK_Suspensions_Tournaments REFERENCES dbo.Tournaments(Id),
    Reason           NVARCHAR(100) NOT NULL,
    MatchesRemaining INT           NOT NULL CONSTRAINT CK_Suspensions_Remaining CHECK (MatchesRemaining >= 0)
);
GO

/* ---------- View bảng xếp hạng (BR12, BR13) ----------
   Chỉ tính các trận Finished. Thứ hạng: Điểm → Hiệu số → Bàn thắng.
   Đối đầu trực tiếp (tiêu chí thứ 4) xử lý ở Service khi các tiêu chí trên bằng nhau. */
CREATE VIEW dbo.vw_Standings AS
WITH Results AS (
    SELECT m.TournamentId, m.HomeTeamId AS TeamId, m.HomeScore AS GF, m.AwayScore AS GA
    FROM dbo.Matches m WHERE m.Status = N'Finished'
    UNION ALL
    SELECT m.TournamentId, m.AwayTeamId, m.AwayScore, m.HomeScore
    FROM dbo.Matches m WHERE m.Status = N'Finished'
)
SELECT
    t.TournamentId,
    t.Id                                                   AS TeamId,
    t.Name                                                 AS TeamName,
    COUNT(r.TeamId)                                        AS Played,
    COALESCE(SUM(CASE WHEN r.GF > r.GA THEN 1 ELSE 0 END), 0)  AS Won,
    COALESCE(SUM(CASE WHEN r.GF = r.GA THEN 1 ELSE 0 END), 0)  AS Drawn,
    COALESCE(SUM(CASE WHEN r.GF < r.GA THEN 1 ELSE 0 END), 0)  AS Lost,
    COALESCE(SUM(r.GF), 0)                                 AS GoalsFor,
    COALESCE(SUM(r.GA), 0)                                 AS GoalsAgainst,
    COALESCE(SUM(r.GF - r.GA), 0)                          AS GoalDifference,
    COALESCE(SUM(CASE WHEN r.GF > r.GA THEN 3 WHEN r.GF = r.GA THEN 1 ELSE 0 END), 0) AS Points
FROM dbo.Teams t
LEFT JOIN Results r ON r.TeamId = t.Id AND r.TournamentId = t.TournamentId
WHERE t.Status = N'Approved'
GROUP BY t.TournamentId, t.Id, t.Name;
GO

/* Ví dụ:
   SELECT * FROM dbo.vw_Standings WHERE TournamentId = 1
   ORDER BY Points DESC, GoalDifference DESC, GoalsFor DESC, TeamName; */
