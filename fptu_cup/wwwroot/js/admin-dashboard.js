/* Tổng quan giải (FR28): GET /api/tournaments/{id}/summary + trận của vòng hiện tại. */
App.main = async function () {
  var $ = App.$;
  var t = App.tournament;
  if (!t) { $('#dash-sub').textContent = 'Chưa có giải đấu nào.'; return; }
  $('#dash-sub').textContent = t.name + ' · ' + App.TOURNAMENT_STATUS[t.status];

  var sumRes = await App.get('/api/tournaments/' + t.id + '/summary');
  if (!sumRes.ok) { App.toast(sumRes.message, 'error'); return; }
  var s = sumRes.data;
  var percent = s.totalMatches ? Math.round(s.playedMatches * 100 / s.totalMatches) : 0;

  $('#stats').innerHTML =
    '<div class="stat-card"><span class="label">Trận đã đá</span><span class="value">' + s.playedMatches + '<small>/ ' + s.totalMatches + '</small></span>' +
    '<span class="progress"><span style="width:' + percent + '%"></span></span></div>' +
    '<div class="stat-card"><span class="label">Bàn thắng</span><span class="value">' + s.goals + '</span>' +
    '<span class="note">Trung bình ' + String(s.averageGoalsPerMatch).replace('.', ',') + ' bàn/trận</span></div>' +
    '<div class="stat-card"><span class="label">Thẻ phạt</span><span class="value">' +
    '<span class="with-card"><i class="card-yellow"></i>' + s.yellowCards + '</span><span class="with-card"><i class="card-red"></i>' + s.redCards + '</span></span>' +
    '<span class="note">' + s.suspendedPlayers + ' cầu thủ đang bị treo giò</span></div>' +
    '<div class="stat-card"><span class="label">Đội tham dự</span><span class="value">' + s.approvedTeams + '<small>/ ' + s.maxTeams + '</small></span>' +
    '<span class="note">Đã duyệt, ' + s.players + ' cầu thủ</span></div>';

  var todos = [];
  var pendingRes = await App.get('/api/tournaments/' + t.id + '/teams?status=Pending&pageSize=1');
  if (pendingRes.ok && pendingRes.data.totalCount > 0)
    todos.push({ n: pendingRes.data.totalCount, title: 'Đội chờ duyệt', detail: 'Duyệt hoặc từ chối trong mục Duyệt đội', href: '/admin/teams', error: false });

  if (!s.currentRound) {
    $('#round-title').textContent = 'Chưa có lịch thi đấu';
    $('#round-table').innerHTML = '<p class="muted">Giải chưa có trận nào đang chờ. Sinh lịch tại mục Lịch &amp; trận đấu.</p>';
  } else {
    var mres = await App.get('/api/matches?tournamentId=' + t.id + '&round=' + s.currentRound + '&pageSize=50');
    var matches = mres.ok ? mres.data.items : [];
    $('#round-title').textContent = 'Vòng ' + s.currentRound + (matches.length ? ' · ' + App.dayLong(matches[0].kickoffTime) : '');
    $('#round-table').innerHTML = '<div class="table-scroll"><table class="tbl tbl-left"><thead><tr><th>Giờ</th><th>Trận đấu</th><th>Sân</th><th>Trọng tài</th><th>Đội hình</th><th><span class="sr-only">Thao tác</span></th></tr></thead><tbody>' +
      matches.map(function (m) {
        var kind = m.lineupsSubmitted === 2 ? 'ok' : m.lineupsSubmitted === 1 ? 'warn' : 'error';
        return '<tr><td class="time">' + App.hhmm(m.kickoffTime) + '</td><td style="font-weight:600">' + App.esc(m.homeTeam) + ' <span class="vs">vs</span> ' + App.esc(m.awayTeam) + '</td>' +
          '<td>' + App.esc(m.venue || '—') + '</td><td>' + (m.referee ? App.esc(m.referee) : '<span class="unassigned">Chưa phân công</span>') + '</td>' +
          '<td>' + App.render.statusBadge(kind, m.lineupsSubmitted + '/2 đội') + '</td>' +
          '<td class="al-right"><a class="btn btn-sm" href="/admin/matches?round=' + m.round + '">Quản lý</a></td></tr>';
      }).join('') + '</tbody></table></div>';

    var noReferee = matches.filter(function (m) { return !m.refereeId && m.status === 'Scheduled'; }).length;
    var missing = matches.filter(function (m) { return m.status === 'Scheduled'; })
      .reduce(function (sum, m) { return sum + (2 - m.lineupsSubmitted); }, 0);
    if (noReferee) todos.push({ n: noReferee, title: 'Trận chưa có trọng tài', detail: 'Vòng ' + s.currentRound + ', cần phân công trước giờ đá', href: '/admin/matches?round=' + s.currentRound, error: false });
    if (missing) todos.push({ n: missing, title: 'Đội chưa nộp đội hình', detail: 'Hạn nộp trước giờ đá 1 ngày, quá hạn bị xử thua (BR23)', href: '/admin/matches?round=' + s.currentRound, error: false });
  }

  $('#todos').innerHTML = todos.length ? todos.map(function (x) {
    return '<a class="todo ' + (x.error ? 'is-error' : 'is-warn') + '" href="' + x.href + '"><span class="n">' + x.n + '</span><span class="t"><b>' +
      App.esc(x.title) + '</b><small>' + App.esc(x.detail) + '</small></span></a>';
  }).join('') : '<p class="muted">Không có việc nào cần xử lý.</p>';
};
