using fptu_cup.Common.Enums;
using fptu_cup.Entities;

namespace fptu_cup.Data.Seed
{
    /// <summary>
    /// Giải "FPTU Cup 2025" đã kết thúc: 8 đội, 56 trận (14 vòng, 25/10/2025 – 24/01/2026), tất cả đã đá xong
    /// kèm đội hình, bàn thắng, thẻ phạt, thay người. Kết quả sinh ngẫu nhiên nhưng cố định (seed 2025),
    /// có xét thẻ đỏ / 3 thẻ vàng nên cầu thủ bị treo giò không ra sân ở trận kế tiếp của đội.
    /// </summary>
    public static partial class DbSeeder
    {
        // Sức mạnh tương đối của từng đội ở mùa 2025 (ảnh hưởng số bàn kỳ vọng mỗi trận).
        private static readonly Dictionary<string, double> Strength2025 = new()
        {
            ["KT"] = 1.6, ["IT"] = 1.3, ["TK"] = 0.9, ["AI"] = 0.7, ["NA"] = 0.4, ["QT"] = 0.1, ["TT"] = -0.1, ["NN"] = -0.4,
        };

        private static void SeedCup2025(FptuCupDbContext db, Tournament cup, (string Code, string Name, string Faculty, string Kit)[] defs,
            Dictionary<string, User> managers, List<User> referees, Venue venueA, Venue venueB)
        {
            var rnd = new Random(2025);
            var teams = new Dictionary<string, Team>();
            var players = new Dictionary<string, List<Player>>();
            for (var t = 0; t < defs.Length; t++)
            {
                var d = defs[t];
                var team = new Team { Tournament = cup, Manager = managers[d.Code], Name = d.Name, Faculty = d.Faculty, KitColor = d.Kit, Status = TeamStatus.Approved };
                db.Teams.Add(team);
                teams[d.Code] = team;
                players[d.Code] = BuildRoster(team, 40 + t, null, null, 12);
                db.Players.AddRange(players[d.Code]);
            }

            var banned = defs.ToDictionary(d => d.Code, _ => new HashSet<Player>());
            var yellows = new Dictionary<Player, int>();
            var index = 0;
            for (var leg = 0; leg < 2; leg++)
            {
                for (var r = 0; r < FirstLeg.Length; r++)
                {
                    var round = leg * FirstLeg.Length + r + 1;
                    for (var i = 0; i < FirstLeg[r].Length; i++)
                    {
                        index++;
                        var (a, b, _, _) = FirstLeg[r][i];
                        var homeCode = leg == 0 ? a : b;
                        var awayCode = leg == 0 ? b : a;
                        var diff = Strength2025[homeCode] - Strength2025[awayCode];
                        var m = new Match
                        {
                            Tournament = cup,
                            Round = round,
                            HomeTeam = teams[homeCode],
                            AwayTeam = teams[awayCode],
                            Venue = i % 2 == 0 ? venueA : venueB,
                            Referee = referees[index % referees.Count],
                            KickoffTime = new DateTime(2025, 10, 25).AddDays(7 * (round - 1)) + (i < 2 ? new TimeSpan(8, 0, 0) : new TimeSpan(9, 10, 0)),
                            Status = MatchStatus.Finished,
                            HomeScore = Poisson(rnd, Math.Clamp(1.5 + 0.2 + 0.5 * diff, 0.3, 4.0)),
                            AwayScore = Poisson(rnd, Math.Clamp(1.5 - 0.5 * diff, 0.3, 4.0)),
                        };
                        db.Matches.Add(m);

                        PlayMatch2025(db, rnd, m, m.HomeTeam!, players[homeCode], banned[homeCode], yellows, m.HomeScore);
                        PlayMatch2025(db, rnd, m, m.AwayTeam!, players[awayCode], banned[awayCode], yellows, m.AwayScore);
                    }
                }
            }
        }

        /// <summary>Đội hình, bàn thắng, thẻ và thay người của một đội trong một trận.</summary>
        private static void PlayMatch2025(FptuCupDbContext db, Random rnd, Match match, Team team, List<Player> roster,
            HashSet<Player> banned, Dictionary<Player, int> yellows, int goals)
        {
            var available = roster.Where(p => !banned.Contains(p)).ToList();
            banned.Clear(); // án treo giò chỉ áp dụng cho đúng một trận kế tiếp

            var starters = Pick(available, PlayerPosition.GK, 1).Concat(Pick(available, PlayerPosition.DF, 2))
                .Concat(Pick(available, PlayerPosition.MF, 2)).Concat(Pick(available, PlayerPosition.FW, 2)).ToList();
            AddLineup(db, match, team, available, starters.Select(p => p.ShirtNumber).ToArray());
            var bench = available.Where(p => !starters.Contains(p)).OrderBy(p => p.ShirtNumber).Take(7).ToList();

            // Bàn thắng: tiền đạo ghi nhiều nhất, rồi tiền vệ, hậu vệ.
            var scorers = starters.Where(p => p.Position != PlayerPosition.GK).ToList();
            for (var g = 0; g < goals; g++)
            {
                var scorer = WeightedPick(rnd, scorers, p => p.Position == PlayerPosition.FW ? 5.0 : p.Position == PlayerPosition.MF ? 2.5 : 1.0);
                db.Goals.Add(new Goal { Match = match, Team = team, Player = scorer, Minute = 1 + rnd.Next(60) });
            }

            // Thẻ phạt: 0–3 thẻ vàng, hiếm khi có thẻ đỏ trực tiếp; cầu thủ bị phạt không phải thủ môn.
            var field = starters.Where(p => p.Position != PlayerPosition.GK).ToList();
            var yellowCount = rnd.Next(100) switch { < 40 => 0, < 75 => 1, < 95 => 2, _ => 3 };
            var carded = new HashSet<Player>();
            for (var c = 0; c < yellowCount && field.Count > carded.Count; c++)
            {
                var p = field.Where(x => !carded.Contains(x)).OrderBy(_ => rnd.Next()).First();
                carded.Add(p);
                db.Cards.Add(new Card { Match = match, Player = p, CardType = CardType.Yellow, Minute = 5 + rnd.Next(55) });
                yellows[p] = yellows.GetValueOrDefault(p) + 1;
                if (yellows[p] >= 3) { banned.Add(p); yellows[p] = 0; } // BR14: 3 thẻ vàng tích lũy = nghỉ một trận
            }
            if (rnd.Next(100) < 4)
            {
                var p = field.Where(x => !carded.Contains(x)).OrderBy(_ => rnd.Next()).First();
                db.Cards.Add(new Card { Match = match, Player = p, CardType = CardType.Red, Minute = 20 + rnd.Next(40) });
                banned.Add(p);
            }

            // Thay người: 0–2 lượt, tiền vệ/tiền đạo/hậu vệ rời sân nhường chỗ cho dự bị không phải thủ môn.
            var subCount = rnd.Next(100) switch { < 30 => 0, < 75 => 1, _ => 2 };
            var outs = starters.Where(p => p.Position != PlayerPosition.GK && !banned.Contains(p)).OrderBy(_ => rnd.Next()).ToList();
            var ins = bench.Where(p => p.Position != PlayerPosition.GK).OrderBy(_ => rnd.Next()).ToList();
            for (var s = 0; s < subCount && s < outs.Count && s < ins.Count; s++)
                db.Substitutions.Add(new Substitution { Match = match, Team = team, PlayerOut = outs[s], PlayerIn = ins[s], Minute = 30 + rnd.Next(26) });
        }

        private static int Poisson(Random rnd, double lambda)
        {
            var limit = Math.Exp(-lambda);
            var k = 0;
            var p = 1.0;
            do { k++; p *= rnd.NextDouble(); } while (p > limit);
            return Math.Min(k - 1, 6);
        }

        private static T WeightedPick<T>(Random rnd, IList<T> items, Func<T, double> weight)
        {
            var roll = rnd.NextDouble() * items.Sum(weight);
            foreach (var item in items)
            {
                roll -= weight(item);
                if (roll <= 0) return item;
            }
            return items[^1];
        }
    }
}
