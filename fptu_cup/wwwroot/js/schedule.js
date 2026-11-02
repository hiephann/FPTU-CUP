/* Lịch thi đấu công khai: lọc theo vòng và đội (FR21). */
App.main = async function () {
  var $ = App.$;
  var t = (await App.loadTournaments()).current;
  if (!t) { $('#schedule-sub').textContent = 'Chưa có giải đấu nào.'; return; }

  var all = await App.getAll('/api/matches?tournamentId=' + t.id);
  if (!all.ok) { App.toast(all.message, 'error'); return; }
  var matches = all.data;
  var maxRound = matches.reduce(function (m, x) { return Math.max(m, x.round); }, 0);
  var firstOpen = matches.filter(function (m) { return m.status !== 'Finished' && m.status !== 'Cancelled'; })[0];
  var currentRound = firstOpen ? firstOpen.round : maxRound;

  var params = new URLSearchParams(location.search);
  var round = params.has('round') ? +params.get('round') : currentRound; // 0 = tất cả
  var team = +params.get('team') || 0;

  $('#schedule-sub').textContent = t.name + ' · ' + matches.length + ' trận, ' + maxRound + ' vòng. Vòng hiện tại đánh dấu •';

  var teamMap = {};
  matches.forEach(function (m) { teamMap[m.homeTeamId] = m.homeTeam; teamMap[m.awayTeamId] = m.awayTeam; });
  $('#team').innerHTML = '<option value="">Tất cả các đội</option>' + Object.keys(teamMap)
    .sort(function (a, b) { return teamMap[a].localeCompare(teamMap[b], 'vi'); })
    .map(function (id) { return '<option value="' + id + '"' + (+id === team ? ' selected' : '') + '>' + App.esc(teamMap[id]) + '</option>'; }).join('');
  $('#round-input').value = round;

  var teamQuery = team ? '&team=' + team : '';
  var chips = '<a class="chip' + (round === 0 ? ' is-active' : '') + '" href="/schedule?round=0' + teamQuery + '">Tất cả</a>';
  for (var r = 1; r <= maxRound; r++) {
    chips += '<a class="chip' + (r === round ? ' is-active' : '') + '" href="/schedule?round=' + r + teamQuery + '">' + r + (r === currentRound ? ' •' : '') + '</a>';
  }
  $('#round-chips').innerHTML = chips;

  var shown = matches.filter(function (m) {
    return (round === 0 || m.round === round) && (!team || m.homeTeamId === team || m.awayTeamId === team);
  });
  $('#match-count').textContent = shown.length + ' trận';

  var rounds = {};
  shown.forEach(function (m) { (rounds[m.round] = rounds[m.round] || []).push(m); });
  var html = Object.keys(rounds).map(function (r) {
    var list = rounds[r];
    var rows = list.map(function (m) {
      var done = m.status === 'Finished';
      var homeWin = done && m.homeScore > m.awayScore;
      var awayWin = done && m.awayScore > m.homeScore;
      var note = m.forfeitTeamId ? ' · xử thua' : m.status === 'Postponed' ? ' (hoãn)' : m.status === 'Cancelled' ? ' (hủy)' : '';
      return '<div class="fixture"><span class="fx-time">' + App.hhmm(m.kickoffTime) + '</span>' +
        '<span class="fx-home' + (homeWin ? ' is-winner' : '') + '">' + App.esc(m.homeTeam) + '</span>' +
        '<span class="fx-score' + (done ? '' : ' is-pending') + '">' + (done ? m.homeScore + ' – ' + m.awayScore : 'vs') + '</span>' +
        '<span class="fx-away' + (awayWin ? ' is-winner' : '') + '">' + App.esc(m.awayTeam) + '</span>' +
        '<span class="fx-meta">' + App.esc(m.venue || 'Chưa có sân') + ' · ' + App.esc(m.referee || 'Chưa phân công trọng tài') + note + '</span>' +
        '<span class="fx-link"><a href="/matches/' + m.id + '">Chi tiết</a></span></div>';
    }).join('');
    return '<section class="card"><div class="card-head"><h2 class="card-title">Vòng ' + r + '</h2><span class="note">' +
      App.dayLongFull(list[0].kickoffTime) + '</span></div><div class="fixtures">' + rows + '</div></section>';
  }).join('');
  $('#fixtures').innerHTML = html || '<section class="card"><p class="muted">Không có trận nào khớp bộ lọc.</p></section>';
};
