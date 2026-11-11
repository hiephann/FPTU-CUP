/* Chi tiết trận (FR31): tỉ số, diễn biến theo phút, đội hình xuất phát và dự bị. */
App.main = async function () {
  var id = location.pathname.split('/').pop();
  var res = await App.get('/api/matches/' + id);
  var root = App.$('#match-root');
  if (!res.ok) {
    root.innerHTML = '<div class="container page-head"><h1>Không tìm thấy trận</h1><p class="muted">' + App.esc(res.message) + '</p><p><a href="/schedule">Về lịch thi đấu</a></p></div>';
    return;
  }

  var m = res.data;
  var home = m.homeTeam, away = m.awayTeam;
  document.title = 'Chi tiết trận – ' + home.name + ' ' + home.score + '–' + away.score + ' ' + away.name;

  var finished = m.status === 'Finished';
  var forfeit = !!m.forfeitReason;
  var statusText = m.forfeitTeamId ? 'Xử thua' : forfeit ? 'Hủy (quá hạn nộp đội hình)' : finished ? 'Kết thúc · 60\'' : App.MATCH_STATUS[m.status];
  // Tỉ số hiệp 1: bàn thắng đến hết phút 30, tính cho đội được ghi bàn.
  var firstHalf = [0, 0];
  m.events.forEach(function (e) {
    if (e.type === 'Goal' && e.minute <= 30) firstHalf[e.teamId === home.id ? 0 : 1]++;
  });
  var showScore = finished || m.status === 'InProgress';

  function eventIcon(e) {
    if (e.type === 'Goal') return '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#10201A" stroke-width="2" role="img" aria-label="Bàn thắng"><circle cx="12" cy="12" r="9"></circle><path d="M12 7l4 3-1.5 4.5h-5L8 10z"></path></svg>';
    if (e.type === 'YellowCard') return '<span class="event-card" role="img" aria-label="Thẻ vàng"></span>';
    if (e.type === 'RedCard') return '<span class="event-card is-red" role="img" aria-label="Thẻ đỏ"></span>';
    return '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#1F7A4D" stroke-width="2" role="img" aria-label="Thay người"><path d="M7 4v16M7 20l-3-3M7 20l3-3M17 20V4M17 4l-3 3M17 4l3 3"></path></svg>';
  }
  function eventText(e) {
    if (e.type === 'Substitution') return '<span class="event-text"><b>Vào: ' + App.esc(e.playerIn) + '</b><small>Ra: ' + App.esc(e.playerOut) + '</small></span>';
    var sub = e.type === 'Goal' ? (e.isOwnGoal ? 'Phản lưới nhà' : '') : e.type === 'YellowCard' ? 'Thẻ vàng' : 'Thẻ đỏ';
    return '<span class="event-text"><b>' + App.esc(e.player) + '</b>' + (sub ? '<small>' + sub + '</small>' : '') + '</span>';
  }

  var events = '', halfShown = false;
  m.events.forEach(function (e) {
    if (!halfShown && e.minute > 30) {
      halfShown = true;
      events += '<div class="divider">Nghỉ giữa hiệp · ' + firstHalf[0] + '–' + firstHalf[1] + '</div>';
    }
    var isHome = e.teamId === home.id;
    var cell = '<span class="event-icon">' + eventIcon(e) + '</span>' + eventText(e);
    events += '<div class="event-row"><div class="event-side is-home">' + (isHome ? eventText(e) + '<span class="event-icon">' + eventIcon(e) + '</span>' : '') + '</div>' +
      '<span class="minute">' + e.minute + '\'</span><div class="event-side">' + (isHome ? '' : cell) + '</div></div>';
  });
  if (!m.events.length) events = '<p class="muted">' + (forfeit ? 'Trận không diễn ra nên không có diễn biến.' : 'Chưa có diễn biến nào.') + '</p>';
  else if (finished) events += '<div class="divider">Hết trận · ' + home.score + '–' + away.score + '</div>';

  var notice = forfeit
    ? '<div class="banner banner-warn" style="flex:1 1 100%"><b>' + (m.forfeitTeamId ? 'Xử thua:' : 'Hủy trận:') + '</b> ' + App.esc(m.forfeitReason) + '</div>'
    : (m.status === 'Scheduled' ? '<div class="banner banner-info" style="flex:1 1 100%">Hạn nộp đội hình: <b>' + App.hhmmDmy(m.lineupDeadline) + '</b> (trước giờ đá 1 ngày). Đội không nộp đúng hạn sẽ bị xử thua 0–3 (BR23).</div>' : '');

  function lineup(team, blue) {
    var rows = team.startingLineup.map(function (p) {
      return '<div class="lineup-row"><span class="no">' + p.shirtNumber + '</span><span class="nm">' + App.esc(p.name) + '</span>' + App.render.positionBadge(p.position) + '</div>';
    }).join('');
    var bench = team.substitutes.map(function (p) { return p.shirtNumber + ' ' + App.esc(p.name); }).join(' · ');
    return '<div class="lineup"><span class="lineup-title' + (blue ? ' is-blue' : '') + '">' + App.teamLabel(team.id, team.name) + '</span>' +
      (rows || '<p class="muted">Chưa nộp đội hình.</p>') +
      (team.substitutes.length ? '<span class="bench-title">Dự bị</span><span class="bench">' + bench + '</span>' : '') + '</div>';
  }

  root.innerHTML =
    '<section class="match-hero"><div class="container">' +
    '<nav class="breadcrumb" aria-label="Đường dẫn"><a href="/schedule">Lịch thi đấu</a><span aria-hidden="true">/</span><span>Vòng ' + m.round +
    '</span><span aria-hidden="true">/</span><span class="current">' + App.esc(home.name) + ' – ' + App.esc(away.name) + '</span></nav>' +
    '<div class="center-row"><span class="status-pill">' + statusText + '</span></div>' +
    '<div class="scoreboard"><div class="scoreboard-team">' + App.avatar(home.id, home.name, 'lg') + App.teamCode(home.name) + '<h2>' + App.esc(home.name) + '</h2><small>Chủ nhà</small></div>' +
    '<div class="scoreboard-score"><strong>' + (showScore ? home.score + '<i>–</i>' + away.score : 'VS') + '</strong>' +
    (showScore && !forfeit ? '<small>Hiệp 1: ' + firstHalf[0] + '–' + firstHalf[1] + '</small>' : '') + '</div>' +
    '<div class="scoreboard-team">' + App.avatar(away.id, away.name, 'lg') + App.teamCode(away.name) + '<h2>' + App.esc(away.name) + '</h2><small>Đội khách</small></div></div>' +
    '<div class="hero-meta"><span>Vòng ' + m.round + ' · ' + App.dayLongFull(m.kickoffTime) + ' · ' + App.hhmm(m.kickoffTime) + '</span><span>' +
    App.esc(m.venue || 'Chưa có sân') + '</span><span>Trọng tài: ' + App.esc(m.referee || 'Chưa phân công') + '</span></div></div></section>' +
    '<main class="detail-main">' + notice + '<section class="card"><div class="card-head"><h2 class="card-title-md">Diễn biến trận đấu</h2><div class="legend">' +
    '<span>Bàn thắng</span><span><span class="mini-card" aria-hidden="true"></span>Thẻ vàng</span><span>Thay người</span></div></div>' +
    '<div class="events-head"><span>' + App.teamLabel(home.id, home.name) + '</span><span></span><span>' + App.teamLabel(away.id, away.name) + '</span></div>' + events + '</section>' +
    '<section class="card"><h2 class="card-title-md">Đội hình xuất phát</h2><p class="note">Đội hình khi bóng lăn, không thay đổi sau các lần thay người.</p>' +
    '<div class="lineups">' + lineup(home, false) + lineup(away, true) + '</div></section></main>';
};
