/* Trang chủ: trận tiếp theo, kết quả/lịch theo vòng, bảng xếp hạng, vua phá lưới, treo giò. */
App.main = async function () {
  var $ = App.$;
  var t = (await App.loadTournaments()).current;
  if (!t) {
    $('#next-card').innerHTML = '<p class="muted">Chưa có giải đấu nào.</p>';
    return;
  }

  var res = await Promise.all([
    App.get('/api/tournaments/' + t.id + '/standings'),
    App.get('/api/matches?tournamentId=' + t.id + '&status=Scheduled&pageSize=1'),
    App.get('/api/tournaments/' + t.id + '/top-scorers?top=5'),
    App.get('/api/tournaments/' + t.id + '/suspensions'),
  ]);
  var standings = res[0].ok ? res[0].data : [];
  var nextMatch = res[1].ok && res[1].data.items.length ? res[1].data.items[0] : null;

  // Số vòng: vòng tròn n đội có (n-1) vòng mỗi lượt (n lẻ thì n vòng).
  var n = standings.length;
  var totalRounds = n < 2 ? 0 : (n % 2 === 0 ? n - 1 : n) * t.numberOfLegs;
  var playedRounds = nextMatch ? nextMatch.round - 1 : (n ? totalRounds : 0);
  var goals = standings.reduce(function (sum, s) { return sum + s.goalsFor; }, 0);

  $('#hero-tags').innerHTML = [t.season, 'Sân 7 người', t.numberOfLegs === 2 ? 'Vòng tròn 2 lượt' : 'Vòng tròn 1 lượt']
    .map(function (x) { return '<span class="pill pill-glass">' + App.esc(x) + '</span>'; }).join('');
  $('#hero-stats').innerHTML =
    '<div class="hero-stat"><strong>' + n + '</strong><span>đội tham dự</span></div>' +
    '<div class="hero-stat"><strong>' + playedRounds + '<small>/' + totalRounds + '</small></strong><span>vòng đã đấu</span></div>' +
    '<div class="hero-stat"><strong>' + goals + '</strong><span>bàn thắng</span></div>';

  // Trận tiếp theo
  var rankOf = {};
  standings.forEach(function (s) { rankOf[s.teamId] = s; });
  function teamInfo(id) {
    var s = rankOf[id];
    return s ? 'Hạng ' + s.rank + ' · ' + s.points + ' điểm' : '';
  }
  if (nextMatch) {
    $('#next-card').innerHTML =
      '<div class="row-between"><span class="eyebrow">Trận tiếp theo</span><span class="note">Vòng ' + nextMatch.round + ' · ' +
      App.dayLong(nextMatch.kickoffTime) + ' · ' + App.hhmm(nextMatch.kickoffTime) + '</span></div>' +
      '<div class="versus"><div class="versus-team">' + App.avatar(nextMatch.homeTeamId, nextMatch.homeTeam, 'md') + App.teamCode(nextMatch.homeTeam) + '<b>' + App.esc(nextMatch.homeTeam) +
      '</b><small>' + teamInfo(nextMatch.homeTeamId) + '</small></div><span class="versus-vs">VS</span>' +
      '<div class="versus-team">' + App.avatar(nextMatch.awayTeamId, nextMatch.awayTeam, 'md') + App.teamCode(nextMatch.awayTeam) + '<b>' + App.esc(nextMatch.awayTeam) +
      '</b><small>' + teamInfo(nextMatch.awayTeamId) + '</small></div></div>' +
      '<div class="info-strip"><span>' + App.esc(nextMatch.venue || 'Chưa có sân') + '</span><span>Trọng tài: ' + App.esc(nextMatch.referee || 'Chưa phân công') + '</span>' +
      '<span>Hạn nộp đội hình: ' + App.hhmmDmy(nextMatch.lineupDeadline) + '</span></div>' +
      '<a class="btn btn-primary btn-block" href="/schedule?round=' + nextMatch.round + '">Xem lịch vòng ' + nextMatch.round + '</a>';
  } else {
    $('#next-card').innerHTML = '<span class="eyebrow">Trận tiếp theo</span><p class="muted">Hiện chưa có trận nào sắp diễn ra.</p>' +
      '<a class="btn btn-primary btn-block" href="/schedule">Xem lịch thi đấu</a>';
  }

  // Kết quả vòng vừa đá + lịch vòng tới
  var rounds = [];
  if (playedRounds >= 1) rounds.push(playedRounds);
  if (nextMatch) rounds.push(nextMatch.round);
  var wanted = +new URLSearchParams(location.search).get('round');
  var selected = rounds.indexOf(wanted) >= 0 ? wanted : rounds[0];
  $('#round-chips').innerHTML = rounds.map(function (r) {
    return '<a class="chip' + (r === selected ? ' is-active' : '') + '" href="/?round=' + r + '#lich">Vòng ' + r + '</a>';
  }).join('') + '<a class="chip" href="/schedule">Toàn bộ lịch</a>';

  if (selected) {
    $('#round-title').textContent = (selected <= playedRounds ? 'Kết quả vòng ' : 'Lịch thi đấu vòng ') + selected;
    var mres = await App.get('/api/matches?tournamentId=' + t.id + '&round=' + selected + '&pageSize=50');
    $('#round-matches').innerHTML = mres.ok ? mres.data.items.map(App.render.matchCard).join('') : '';
  } else {
    $('#round-title').textContent = 'Lịch thi đấu';
    $('#round-matches').innerHTML = '<p class="muted">Chưa có lịch thi đấu.</p>';
  }

  $('#standings').innerHTML = App.render.standings(standings);
  $('#scorers').innerHTML = App.render.topScorers(res[2].ok ? res[2].data : []);
  $('#susp-title').textContent = nextMatch ? 'Treo giò vòng ' + nextMatch.round : 'Treo giò';
  $('#suspensions').innerHTML = App.render.suspensions(res[3].ok ? res[3].data : []);
};
