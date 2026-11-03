/* Các hàm dựng HTML dùng chung cho nhiều trang (bảng xếp hạng, vua phá lưới, treo giò, thẻ trận). */
(function () {
  var R = (App.render = {});

  R.standings = function (rows) {
    if (!rows.length) return '<p class="muted">Chưa có đội nào được duyệt.</p>';
    var body = rows.map(function (s) {
      var gd = s.goalDifference > 0 ? '+' + s.goalDifference : String(s.goalDifference);
      var form = s.form.split('').map(function (f) { return '<span class="form-' + f + '">' + f + '</span>'; }).join('');
      return '<tr><td><span class="rank' + (s.rank === 1 ? ' is-first' : '') + '">' + s.rank + '</span></td>' +
        '<td class="al-left"><span class="team-cell">' + App.avatar(s.teamId, s.team, 'sm').replace('tone-' + App.tone(s.teamId), 'tone-neutral') + App.esc(s.team) + '</span></td>' +
        '<td>' + s.played + '</td><td>' + s.won + '</td><td>' + s.drawn + '</td><td>' + s.lost + '</td>' +
        '<td>' + s.goalsFor + '</td><td>' + s.goalsAgainst + '</td><td>' + gd + '</td>' +
        '<td class="points">' + s.points + '</td><td><span class="form">' + form + '</span></td></tr>';
    }).join('');
    return '<div class="table-scroll"><table class="tbl tbl-center"><thead><tr><th>#</th><th class="al-left">Đội</th>' +
      '<th>Tr</th><th>T</th><th>H</th><th>B</th><th>BT</th><th>BB</th><th>HS</th><th>Điểm</th><th>Phong độ</th></tr></thead><tbody>' +
      body + '</tbody></table></div>';
  };

  R.topScorers = function (items) {
    if (!items.length) return '<p class="muted">Chưa có bàn thắng nào.</p>';
    return items.map(function (p) {
      return '<div class="list-row"><span class="rk">' + p.rank + '</span><span class="who"><b>' + App.esc(p.player) +
        '</b><small>' + App.esc(p.team) + '</small></span><span class="num">' + p.goals + '</span></div>';
    }).join('');
  };

  R.suspensions = function (items) {
    if (!items.length) return '<p class="muted">Không có cầu thủ nào bị treo giò.</p>';
    return items.map(function (s) {
      var red = /đỏ/i.test(s.reason);
      return '<div class="list-row is-plain"><span class="' + (red ? 'card-red' : 'card-yellow') + '" role="img" aria-label="' + (red ? 'Thẻ đỏ' : 'Thẻ vàng') + '"></span>' +
        '<span class="who"><b>' + App.esc(s.player) + '</b><small>' + App.esc(s.team) + ' · ' + App.esc(s.reason) + '</small></span>' +
        '<span class="note">' + s.matchesRemaining + ' trận</span></div>';
    }).join('');
  };

  /** Thẻ trận trên trang chủ; mọi trận đều xem được chi tiết. */
  R.matchCard = function (m) {
    var done = m.status === 'Finished';
    var homeWin = done && m.homeScore > m.awayScore;
    var awayWin = done && m.awayScore > m.homeScore;
    return '<a class="match-card is-link" href="/matches/' + m.id + '"' + (m.forfeitReason ? ' title="' + App.esc(m.forfeitReason) + '"' : '') + '>' +
      '<div class="match-card-meta"><span>' + App.day(m.kickoffTime) + ' · ' + App.hhmm(m.kickoffTime) + '</span>' +
      '<span class="status' + (done && !m.forfeitTeamId ? '' : ' is-upcoming') + '">' + App.matchStatusText(m) + '</span></div>' +
      '<div class="match-card-row' + (homeWin ? ' is-winner' : '') + '"><b>' + App.esc(m.homeTeam) + '</b><span class="score">' + (done ? m.homeScore : '–') + '</span></div>' +
      '<div class="match-card-row' + (awayWin ? ' is-winner' : '') + '"><b>' + App.esc(m.awayTeam) + '</b><span class="score">' + (done ? m.awayScore : '–') + '</span></div>' +
      '<div class="match-card-foot">' + App.esc(m.venue || 'Chưa có sân') + '</div></a>';
  };

  /** Nút Trước/Sau cho dữ liệu phân trang { page, totalPages }; nút mang data-page để trang tự gắn sự kiện. */
  R.pager = function (d) {
    if (!d || d.totalPages <= 1) return '';
    return '<button class="btn btn-sm" data-page="' + (d.page - 1) + '"' + (d.page <= 1 ? ' disabled' : '') + '>‹ Trước</button>' +
      '<span class="note">Trang ' + d.page + '/' + d.totalPages + '</span>' +
      '<button class="btn btn-sm" data-page="' + (d.page + 1) + '"' + (d.page >= d.totalPages ? ' disabled' : '') + '>Sau ›</button>';
  };

  R.positionBadge = function (pos) {
    return '<span class="pos pos-' + String(pos).toLowerCase() + '">' + App.esc(pos) + '</span>';
  };

  R.statusBadge = function (kind, text) {
    return '<span class="badge badge-' + kind + '">' + App.esc(text) + '</span>';
  };
})();
