/* Danh sách trận được phân công cho trọng tài đang đăng nhập: GET /api/matches?mine=true. */
App.main = async function () {
  var res = await App.getAll('/api/matches?mine=true');
  var box = App.$('#match-table');
  if (!res.ok) { box.innerHTML = '<p class="muted">' + App.esc(res.message) + '</p>'; return; }
  if (!res.data.length) { box.innerHTML = '<p class="muted">Bạn chưa được phân công trận nào.</p>'; return; }

  // Trận chưa đá lên trước, trận đã đá xếp sau (mới nhất trước).
  var list = res.data.slice().sort(function (a, b) {
    var ad = a.status === 'Finished', bd = b.status === 'Finished';
    if (ad !== bd) return ad ? 1 : -1;
    return ad ? new Date(b.kickoffTime) - new Date(a.kickoffTime) : new Date(a.kickoffTime) - new Date(b.kickoffTime);
  });

  box.innerHTML = '<div class="table-scroll"><table class="tbl tbl-left"><thead><tr><th>Vòng</th><th>Thời gian</th><th>Trận đấu</th><th>Sân</th><th>Đội hình</th><th>Trạng thái</th><th><span class="sr-only">Thao tác</span></th></tr></thead><tbody>' +
    list.map(function (m) {
      var done = m.status === 'Finished';
      var live = m.status === 'InProgress';
      var scheduled = m.status === 'Scheduled';
      return '<tr><td class="time">' + m.round + '</td><td>' + App.day(m.kickoffTime) + ' · <b>' + App.hhmm(m.kickoffTime) + '</b></td>' +
        '<td style="font-weight:600">' + App.esc(m.homeTeam) + (done || live ? ' <span class="display score-inline">' + m.homeScore + '–' + m.awayScore + '</span> ' : ' <span class="vs">vs</span> ') + App.esc(m.awayTeam) + '</td>' +
        '<td>' + App.esc(m.venue || '—') + '</td><td>' + (done ? '' : App.render.statusBadge(m.lineupsSubmitted === 2 ? 'ok' : 'warn', m.lineupsSubmitted + '/2 đội')) + '</td>' +
        '<td>' + App.render.statusBadge(done || m.status === 'Cancelled' ? 'neutral' : 'ok', App.matchStatusText(m)) + '</td>' +
        '<td class="al-right">' + (scheduled || live
          ? '<a class="btn btn-sm btn-primary" href="/referee/matches/' + m.id + '">' + (live ? 'Tiếp tục ghi trận' : 'Vào trận') + '</a>'
          : '<a class="btn btn-sm" href="/matches/' + m.id + '">Biên bản</a>') + '</td></tr>';
    }).join('') + '</tbody></table></div>';
};
