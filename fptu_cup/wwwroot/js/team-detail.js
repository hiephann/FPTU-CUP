/* Chi tiết đội: thông tin, danh sách cầu thủ (link tới thống kê) và các trận của đội. */
App.main = async function () {
  var root = App.$('#team-root');
  var id = +location.pathname.split('/').pop();
  var res = await Promise.all([
    App.get('/api/teams/' + id),
    App.get('/api/teams/' + id + '/players'),
    App.getAll('/api/matches?teamId=' + id),
  ]);
  if (!res[0].ok) { root.innerHTML = '<section class="card"><p class="muted">' + App.esc(res[0].message) + '</p></section>'; return; }

  var t = res[0].data;
  var players = res[1].ok ? res[1].data : [];
  var matches = res[2].ok ? res[2].data : [];
  App.$('#crumb').textContent = t.name;
  document.title = t.name + ' – FPTU Cup';

  var fixtures = matches.map(function (m) {
    var done = m.status === 'Finished';
    return '<div class="fixture"><span class="fx-time">V' + m.round + '</span><span class="fx-home">' + App.esc(m.homeTeam) + '</span>' +
      '<span class="fx-score' + (done ? '' : ' is-pending') + '">' + (done ? m.homeScore + ' – ' + m.awayScore : 'vs') + '</span><span class="fx-away">' + App.esc(m.awayTeam) + '</span>' +
      '<span class="fx-meta">' + App.day(m.kickoffTime) + ' · ' + App.hhmm(m.kickoffTime) + ' · ' + App.esc(m.venue || 'Chưa có sân') + '</span>' +
      '<span class="fx-link"><a href="/matches/' + m.id + '">Chi tiết</a></span></div>';
  }).join('');

  root.innerHTML =
    '<section class="card team-head">' + App.avatar(t.id, t.name, 'lg') + '<div class="team-head-text"><h2>' + App.esc(t.name) + '</h2>' +
    '<p class="muted">' + App.esc(t.tournament) + '</p><div class="chips"><span class="pill pill-chip">Khoa: ' + App.esc(t.faculty || '—') + '</span>' +
    '<span class="pill pill-chip">Màu áo: ' + App.esc(t.kitColor || '—') + '</span><span class="pill pill-chip">Quản lý: ' + App.esc(t.managerName) + '</span></div></div></section>' +
    '<section class="card"><h2 class="card-title">Cầu thủ (' + players.length + ')</h2>' +
    (players.length ? '<div class="table-scroll"><table class="tbl tbl-left"><thead><tr><th>Số áo</th><th>Họ và tên</th><th>Vị trí</th><th>MSSV</th><th></th></tr></thead><tbody>' +
      players.map(function (p) {
        return '<tr><td class="time">' + p.shirtNumber + '</td><td style="font-weight:600">' + App.esc(p.fullName) + '</td><td>' + App.render.positionBadge(p.position) +
          '</td><td>' + App.esc(p.studentCode) + '</td><td class="al-right"><a href="/players/' + p.id + '">Thống kê</a></td></tr>';
      }).join('') + '</tbody></table></div>' : '<p class="muted">Chưa có cầu thủ nào.</p>') + '</section>' +
    '<section class="card"><h2 class="card-title">Các trận của đội</h2>' + (fixtures ? '<div class="fixtures">' + fixtures + '</div>' : '<p class="muted">Chưa có lịch thi đấu.</p>') + '</section>';
};
