using fptu_cup.Common.Enums;
using fptu_cup.Entities;
using Microsoft.EntityFrameworkCore;

namespace fptu_cup.Data.Seed
{
    /// <summary>
    /// Dữ liệu mẫu để chạy thử ngay:
    /// admin / Admin@123, referee1..4 / Referee@123, manager_cntt, manager_kt, ... / Manager@123.
    /// Giải "FPTU Cup 2026" đang Ongoing: 8 đội, 56 trận (14 vòng), vòng 1–3 đã đá xong, vòng 4 sắp đá.
    /// Giải "FPTU Cup 2025" đã Finished: xem DbSeeder.Cup2025.cs.
    /// </summary>
    public static partial class DbSeeder
    {
        private static readonly (int No, PlayerPosition Pos)[] Generic =
        {
            (1, PlayerPosition.GK), (2, PlayerPosition.DF), (3, PlayerPosition.DF), (4, PlayerPosition.DF),
            (6, PlayerPosition.MF), (8, PlayerPosition.MF), (10, PlayerPosition.MF),
            (7, PlayerPosition.FW), (9, PlayerPosition.FW), (11, PlayerPosition.FW),
        };

        private static readonly string[] LastNames = { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Đặng", "Bùi", "Đỗ", "Ngô" };
        private static readonly string[] MiddleNames = { "Văn", "Minh", "Quốc", "Hữu", "Gia", "Thanh", "Đức", "Anh", "Công", "Tuấn" };
        private static readonly string[] FirstNames = { "An", "Bảo", "Cường", "Dũng", "Hải", "Hiếu", "Khoa", "Long", "Nam", "Phúc", "Quân", "Sơn", "Thắng", "Tín", "Việt" };

        private static readonly (int No, PlayerPosition Pos, string Name)[] CnttRoster =
        {
            (1, PlayerPosition.GK, "Lê Minh Tâm"), (3, PlayerPosition.DF, "Võ Thanh Tùng"), (4, PlayerPosition.DF, "Hoàng Gia Khánh"),
            (5, PlayerPosition.DF, "Bùi Anh Khoa"), (6, PlayerPosition.MF, "Nguyễn Văn Em"), (8, PlayerPosition.MF, "Lê Văn Cường"),
            (9, PlayerPosition.FW, "Trần Văn Bình"), (11, PlayerPosition.FW, "Nguyễn Đức Long"), (12, PlayerPosition.MF, "Phạm Văn Dũng"),
            (13, PlayerPosition.GK, "Trương Hữu Nghĩa"), (14, PlayerPosition.DF, "Đinh Công Sơn"), (17, PlayerPosition.FW, "Lý Quang Hải"),
        };

        private static readonly (int No, PlayerPosition Pos, string Name)[] KinhTeRoster =
        {
            (1, PlayerPosition.GK, "Phan Quốc Bảo"), (2, PlayerPosition.DF, "Lương Văn Hậu"), (4, PlayerPosition.DF, "Mạc Đình Trọng"),
            (6, PlayerPosition.MF, "Huỳnh Công Vinh"), (8, PlayerPosition.MF, "Đào Duy Khánh"), (9, PlayerPosition.FW, "Đỗ Minh Giang"),
            (10, PlayerPosition.FW, "Cao Hữu Tài"), (12, PlayerPosition.MF, "Ông Thanh Sơn"), (14, PlayerPosition.DF, "Bế Xuân Hòa"),
            (16, PlayerPosition.FW, "Lưu Minh Hiếu"),
        };

        private static readonly (int No, PlayerPosition Pos, string Name)[] QtkdRoster =
        {
            (1, PlayerPosition.GK, "Châu Gia Huy"), (2, PlayerPosition.DF, "Trịnh Văn Lộc"), (4, PlayerPosition.DF, "Hồ Gia Bảo"),
            (7, PlayerPosition.MF, "Ngô Minh Khôi"), (8, PlayerPosition.MF, "Lâm Hoàng Phúc"), (10, PlayerPosition.FW, "Vũ Thành Phát"),
            (11, PlayerPosition.FW, "Kiều Văn Sang"), (15, PlayerPosition.MF, "Đặng Quang Vinh"), (18, PlayerPosition.DF, "Tạ Hữu Phước"),
            (20, PlayerPosition.GK, "Lê Công Minh"),
        };

        // Lượt đi (7 vòng x 4 trận): (nhà, khách, bàn nhà, bàn khách); null = chưa đá.
        private static readonly (string Home, string Away, int? HomeScore, int? AwayScore)[][] FirstLeg =
        {
            new[] { ("IT", "AI", (int?)4, (int?)1), ("KT", "TT", 2, 1), ("NA", "TK", 0, 2), ("QT", "NN", 3, 1) },
            new[] { ("IT", "TK", (int?)4, (int?)1), ("KT", "NN", 4, 1), ("AI", "TT", 4, 1), ("NA", "QT", 3, 1) },
            new[] { ("IT", "QT", (int?)3, (int?)1), ("KT", "NA", 2, 2), ("AI", "NN", 2, 0), ("TK", "TT", 1, 1) },
            new[] { ("IT", "KT", (int?)null, (int?)null), ("AI", "QT", null, null), ("NA", "TT", null, null), ("NN", "TK", null, null) },
            new[] { ("IT", "NA", (int?)null, (int?)null), ("KT", "AI", null, null), ("TK", "QT", null, null), ("TT", "NN", null, null) },
            new[] { ("IT", "TT", (int?)null, (int?)null), ("KT", "QT", null, null), ("AI", "TK", null, null), ("NA", "NN", null, null) },
            new[] { ("IT", "NN", (int?)null, (int?)null), ("KT", "TK", null, null), ("AI", "NA", null, null), ("QT", "TT", null, null) },
        };

        public static async Task SeedAsync(FptuCupDbContext db, IConfiguration config)
        {
            var section = config.GetSection("Seed:Admin");
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
            if (admin == null)
            {
                admin = new User
                {
                    Username = section["Username"] ?? "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(section["Password"] ?? "Admin@123"),
                    FullName = section["FullName"] ?? "Tournament Admin",
                    Email = section["Email"] ?? "admin@fptu.edu.vn",
                    Role = UserRole.Admin,
                };
                db.Users.Add(admin);
                await db.SaveChangesAsync();
            }

            if (await db.Tournaments.AnyAsync()) return; // đã có dữ liệu mẫu

            var refereeHash = BCrypt.Net.BCrypt.HashPassword("Referee@123");
            var managerHash = BCrypt.Net.BCrypt.HashPassword("Manager@123");

            var referees = new[] { "Hoàng Văn Rạng", "Phan Thị Mai", "Lê Quốc Tuấn", "Đỗ Thanh Hà" }
                .Select((name, i) => new User
                {
                    Username = $"referee{i + 1}", PasswordHash = refereeHash, FullName = name,
                    Email = $"referee{i + 1}@fptu.edu.vn", Role = UserRole.Referee,
                }).ToList();
            db.Users.AddRange(referees);

            var venueA = new Venue { Name = "Sân A", Address = "Khu thể thao FPTU", Capacity = 300 };
            var venueB = new Venue { Name = "Sân B", Address = "Khu thể thao FPTU", Capacity = 200 };
            db.Venues.AddRange(venueA, venueB);

            var cup2026 = NewTournament("FPTU Cup 2026", "Mùa Thu 2026", new DateOnly(2026, 10, 24), new DateOnly(2027, 1, 23), 8, 2, TournamentStatus.Ongoing, admin);
            var cup2025 = NewTournament("FPTU Cup 2025", "Mùa Thu 2025", new DateOnly(2025, 10, 25), new DateOnly(2026, 1, 24), 8, 2, TournamentStatus.Finished, admin);
            var cupXuan = NewTournament("FPTU Cup Xuân 2027", "Xuân 2027", new DateOnly(2027, 3, 13), new DateOnly(2027, 5, 15), 8, 2, TournamentStatus.Registration, admin);
            var cupHe = NewTournament("FPTU Cup Hè 2027", "Hè 2027", new DateOnly(2027, 6, 5), new DateOnly(2027, 7, 31), 6, 1, TournamentStatus.Draft, admin);
            db.Tournaments.AddRange(cup2026, cup2025, cupXuan, cupHe);

            // ---------------------------------------------------------------- Giải đang diễn ra: 8 đội
            var teamDefs = new (string Code, string Name, string Faculty, string Kit, string Manager, (int, PlayerPosition, string)[]? Roster, Dictionary<int, string>? Overrides)[]
            {
                ("IT", "CNTT FC", "Information Technology", "Cam", "manager_cntt", CnttRoster, null),
                ("KT", "Kinh tế FC", "Kinh tế", "Cam đỏ", "manager_kt", KinhTeRoster, null),
                ("AI", "AI FC", "Trí tuệ nhân tạo", "Xám", "manager_ai", null, null),
                ("NA", "Ngôn ngữ Anh FC", "Ngôn ngữ Anh", "Xám", "manager_na", null, null),
                ("TK", "Thiết kế ĐH FC", "Thiết kế đồ họa", "Xám", "manager_tk", null, null),
                ("QT", "QTKD FC", "Quản trị kinh doanh", "Xanh dương", "manager_qtkd", QtkdRoster, null),
                ("TT", "Truyền thông FC", "Truyền thông đa phương tiện", "Xám", "manager_tt", null, new Dictionary<int, string> { [2] = "Mai Tuấn Kiệt" }),
                ("NN", "Ngôn ngữ Nhật FC", "Ngôn ngữ Nhật", "Xám", "manager_nn", null, null),
            };

            var teams = new Dictionary<string, Team>();
            var players = new Dictionary<string, List<Player>>();
            var managers = new Dictionary<string, User>();
            for (var t = 0; t < teamDefs.Length; t++)
            {
                var d = teamDefs[t];
                var manager = NewManager(d.Manager, d.Name + " – quản lý", managerHash);
                db.Users.Add(manager);
                managers[d.Code] = manager;
                var team = new Team { Tournament = cup2026, Manager = manager, Name = d.Name, Faculty = d.Faculty, KitColor = d.Kit, Status = TeamStatus.Approved };
                db.Teams.Add(team);
                teams[d.Code] = team;
                players[d.Code] = BuildRoster(team, t + 1, d.Roster, d.Overrides);
                db.Players.AddRange(players[d.Code]);
            }

            // ---------------------------------------------------------------- Giải đang mở đăng ký: 5 đội
            var registration = new (string Name, string Faculty, string Manager, int Players, TeamStatus Status)[]
            {
                ("Cơ khí FC", "Cơ khí", "manager_ck", 9, TeamStatus.Approved),
                ("Điện tử FC", "Điện – Điện tử", "manager_dt", 12, TeamStatus.Pending),
                ("Marketing FC", "Marketing", "manager_mk", 14, TeamStatus.Pending),
                ("Luật FC", "Luật", "manager_luat", 6, TeamStatus.Pending),
                ("Du lịch FC", "Du lịch – Khách sạn", "manager_dl", 8, TeamStatus.Rejected),
            };
            for (var i = 0; i < registration.Length; i++)
            {
                var r = registration[i];
                var manager = NewManager(r.Manager, r.Name + " – quản lý", managerHash);
                db.Users.Add(manager);
                var team = new Team { Tournament = cupXuan, Manager = manager, Name = r.Name, Faculty = r.Faculty, KitColor = "Xanh lá", Status = r.Status };
                db.Teams.Add(team);
                db.Players.AddRange(BuildRoster(team, 20 + i, null, null, r.Players));
            }

            // ---------------------------------------------------------------- Lịch 14 vòng, kết quả vòng 1–3
            var matches = new List<(Match Match, int Index)>();
            var index = 0;
            for (var leg = 0; leg < 2; leg++)
            {
                for (var r = 0; r < FirstLeg.Length; r++)
                {
                    var round = leg * FirstLeg.Length + r + 1;
                    for (var i = 0; i < FirstLeg[r].Length; i++)
                    {
                        index++;
                        var (home, away, homeScore, awayScore) = FirstLeg[r][i];
                        var played = leg == 0 && homeScore.HasValue;
                        var time = i < 2 ? new TimeSpan(8, 0, 0) : new TimeSpan(9, 10, 0);
                        var m = new Match
                        {
                            Tournament = cup2026,
                            Round = round,
                            HomeTeam = teams[leg == 0 ? home : away],
                            AwayTeam = teams[leg == 0 ? away : home],
                            Venue = i % 2 == 0 ? venueA : venueB,
                            KickoffTime = new DateTime(2026, 10, 24).AddDays(7 * (round - 1)) + time,
                            Status = played ? MatchStatus.Finished : MatchStatus.Scheduled,
                            HomeScore = played ? homeScore!.Value : 0,
                            AwayScore = played ? awayScore!.Value : 0,
                        };
                        if (round <= 3) m.Referee = referees[(index + 3) % referees.Count];
                        if (index == 13) m.Referee = referees[0];
                        if (index == 14) m.Referee = referees[1];
                        db.Matches.Add(m);
                        matches.Add((m, index));
                    }
                }
            }

            foreach (var (m, n) in matches.Where(x => x.Match.Status == MatchStatus.Finished))
            {
                var homeCode = teams.First(t => t.Value == m.HomeTeam).Key;
                var awayCode = teams.First(t => t.Value == m.AwayTeam).Key;
                var featured = n == 9; // vòng 3: CNTT FC 3–1 QTKD FC – có diễn biến chi tiết
                AddLineup(db, m, m.HomeTeam!, players[homeCode], featured ? new[] { 1, 3, 14, 6, 8, 9, 11 } : null);
                AddLineup(db, m, m.AwayTeam!, players[awayCode], featured ? new[] { 1, 2, 4, 7, 8, 10, 11 } : null);

                if (featured) AddFeaturedEvents(db, m, players[homeCode], players[awayCode]);
                else
                {
                    AddGoals(db, m, m.HomeTeam!, players[homeCode], m.HomeScore, n, 0);
                    AddGoals(db, m, m.AwayTeam!, players[awayCode], m.AwayScore, n, 1);
                }
            }

            // Mai Tuấn Kiệt (Truyền thông FC) nhận 3 thẻ vàng ở vòng 1, 2, 3.
            var kiet = players["TT"].First(p => p.ShirtNumber == 2);
            foreach (var n in new[] { 2, 7, 12 })
                db.Cards.Add(new Card { Match = matches[n - 1].Match, Player = kiet, CardType = CardType.Yellow, Minute = 30 });

            // Vòng 4: CNTT FC và Kinh tế FC đã nộp đội hình (trận 13); AI FC (14) và Thiết kế ĐH FC (16) mới nộp một đội.
            AddLineup(db, matches[12].Match, teams["IT"], players["IT"], new[] { 1, 3, 5, 6, 8, 9, 11 }, new[] { 14 }); // Sơn đang bị treo giò (BR19)
            AddLineup(db, matches[12].Match, teams["KT"], players["KT"], null);
            AddLineup(db, matches[13].Match, teams["AI"], players["AI"], null);
            AddLineup(db, matches[15].Match, teams["TK"], players["TK"], null);

            // Án treo giò còn hiệu lực ở vòng 4.
            db.Suspensions.AddRange(
                new Suspension { Player = players["IT"].First(p => p.ShirtNumber == 14), Tournament = cup2026, Reason = "Thẻ đỏ vòng 3", MatchesRemaining = 1 },
                new Suspension { Player = players["QT"].First(p => p.ShirtNumber == 4), Tournament = cup2026, Reason = "Thẻ đỏ vòng 3", MatchesRemaining = 1 },
                new Suspension { Player = kiet, Tournament = cup2026, Reason = "Tích lũy 3 thẻ vàng", MatchesRemaining = 1 });

            // Giải 2025 đã kết thúc: các khoa cũ tham dự lại, cùng người quản lý đội.
            SeedCup2025(db, cup2025, teamDefs.Select(d => (d.Code, d.Name, d.Faculty, d.Kit)).ToArray(), managers, referees, venueA, venueB);

            await db.SaveChangesAsync();
        }

        private static Tournament NewTournament(string name, string season, DateOnly start, DateOnly end, int max, int legs, TournamentStatus status, User creator) => new()
        {
            Name = name, Season = season, StartDate = start, EndDate = end, MaxTeams = max,
            NumberOfLegs = legs, Status = status, Creator = creator,
        };

        private static User NewManager(string username, string fullName, string hash) => new()
        {
            Username = username, PasswordHash = hash, FullName = fullName,
            Email = username + "@fpt.edu.vn", Role = UserRole.TeamManager,
        };

        /// <summary>Tạo danh sách cầu thủ: dùng roster có tên sẵn hoặc sinh theo mẫu 10 người (1 GK, 3 DF, 3 MF, 3 FW).</summary>
        private static List<Player> BuildRoster(Team team, int teamNo, (int No, PlayerPosition Pos, string Name)[]? named,
            Dictionary<int, string>? overrides, int? count = null)
        {
            var list = new List<Player>();
            if (named != null)
            {
                foreach (var (no, pos, name) in named)
                    list.Add(NewPlayer(team, teamNo, list.Count + 1, no, pos, name));
                return list;
            }

            var spec = Generic.ToList();
            for (var extra = 12; spec.Count < (count ?? 10); extra++)
                spec.Add((extra, extra % 4 == 0 ? PlayerPosition.DF : extra % 4 == 1 ? PlayerPosition.MF : extra % 4 == 2 ? PlayerPosition.FW : PlayerPosition.GK));

            foreach (var (no, pos) in spec.Take(count ?? 10))
            {
                var n = list.Count + 1;
                var name = overrides != null && overrides.TryGetValue(no, out var fixedName)
                    ? fixedName
                    : $"{LastNames[(teamNo + n) % LastNames.Length]} {MiddleNames[(teamNo * 3 + n) % MiddleNames.Length]} {FirstNames[(teamNo * 5 + n * 7) % FirstNames.Length]}";
                list.Add(NewPlayer(team, teamNo, n, no, pos, name));
            }
            return list;
        }

        private static Player NewPlayer(Team team, int teamNo, int seq, int no, PlayerPosition pos, string name) => new()
        {
            Team = team,
            StudentCode = $"HE19{teamNo:00}{seq:00}",
            FullName = name,
            ShirtNumber = no,
            Position = pos,
            DateOfBirth = new DateOnly(2003 + seq % 2, 1 + seq % 12, 1 + (seq * 3) % 28),
        };

        /// <summary>Đội hình mặc định: 1 GK, 2 hậu vệ, 2 tiền vệ, 2 tiền đạo (số áo nhỏ nhất); còn lại dự bị (tối đa 7).</summary>
        private static void AddLineup(FptuCupDbContext db, Match match, Team team, List<Player> roster, int[]? starterNumbers, int[]? excludeNumbers = null)
        {
            if (excludeNumbers != null) roster = roster.Where(p => !excludeNumbers.Contains(p.ShirtNumber)).ToList();
            var starters = starterNumbers != null
                ? roster.Where(p => starterNumbers.Contains(p.ShirtNumber)).ToList()
                : Pick(roster, PlayerPosition.GK, 1).Concat(Pick(roster, PlayerPosition.DF, 2))
                    .Concat(Pick(roster, PlayerPosition.MF, 2)).Concat(Pick(roster, PlayerPosition.FW, 2)).ToList();

            foreach (var p in starters)
                db.MatchLineups.Add(new MatchLineup { Match = match, Team = team, Player = p, IsStarter = true });
            foreach (var p in roster.Where(p => !starters.Contains(p)).OrderBy(p => p.ShirtNumber).Take(7))
                db.MatchLineups.Add(new MatchLineup { Match = match, Team = team, Player = p, IsStarter = false });
        }

        private static IEnumerable<Player> Pick(List<Player> roster, PlayerPosition pos, int count) =>
            roster.Where(p => p.Position == pos).OrderBy(p => p.ShirtNumber).Take(count);

        /// <summary>Bàn thắng cho các trận không có diễn biến chi tiết: tiền đạo xuất phát ghi bàn, phút rải đều.</summary>
        private static void AddGoals(FptuCupDbContext db, Match match, Team team, List<Player> roster, int goals, int matchNo, int side)
        {
            var forwards = Pick(roster, PlayerPosition.FW, 2).ToList();
            for (var i = 0; i < goals; i++)
            {
                db.Goals.Add(new Goal
                {
                    Match = match, Team = team, Player = forwards[(matchNo + i) % forwards.Count],
                    Minute = 8 + (matchNo * 7 + i * 13 + side * 5) % 50,
                });
            }
        }

        /// <summary>Vòng 3, CNTT FC 3–1 QTKD FC: bàn thắng, thẻ, thay người để trang chi tiết trận có nội dung.</summary>
        private static void AddFeaturedEvents(FptuCupDbContext db, Match match, List<Player> home, List<Player> away)
        {
            Player H(int no) => home.First(p => p.ShirtNumber == no);
            Player A(int no) => away.First(p => p.ShirtNumber == no);

            db.Goals.AddRange(
                new Goal { Match = match, Team = match.HomeTeam, Player = H(9), Minute = 12 },
                new Goal { Match = match, Team = match.AwayTeam, Player = A(10), Minute = 27 },
                new Goal { Match = match, Team = match.HomeTeam, Player = H(9), Minute = 38 },
                new Goal { Match = match, Team = match.HomeTeam, Player = H(11), Minute = 51 });

            db.Cards.AddRange(
                new Card { Match = match, Player = A(4), CardType = CardType.Yellow, Minute = 25 },
                new Card { Match = match, Player = H(8), CardType = CardType.Yellow, Minute = 44 },
                new Card { Match = match, Player = H(14), CardType = CardType.Red, Minute = 55 },
                new Card { Match = match, Player = A(4), CardType = CardType.Yellow, Minute = 58 },
                new Card { Match = match, Player = A(4), CardType = CardType.Red, Minute = 58 }); // thẻ vàng thứ hai = thẻ đỏ

            db.Substitutions.AddRange(
                new Substitution { Match = match, Team = match.AwayTeam, PlayerOut = A(7), PlayerIn = A(15), Minute = 40 },
                new Substitution { Match = match, Team = match.HomeTeam, PlayerOut = H(6), PlayerIn = H(12), Minute = 46 });
        }
    }
}
