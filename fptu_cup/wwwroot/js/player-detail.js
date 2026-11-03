/* Thống kê cá nhân của cầu thủ (FR14): GET /api/players/{id}/stats. */
App.main = async function () {
  var root = App.$('#player-root');
  var id = +location.pathname.split('/').pop();
  var res = await App.get('/api/players/' + id + '/stats');
  if (!res.ok) { root.innerHTML = '<section class="card"><p class="muted">' + App.esc(res.message) + '</p></section>'; return; }

  var s = res.data, p = s.player;
  App.$('#crumb').textContent = p.fullName;
  document.title = p.fullName + ' – FPTU Cup';

  function stat(label, value, note) {
    return '<div class="stat-card"><span class="label">' + label + '</span><span class="value">' + value + '</span>' + (note ? '<span class="note">' + note + '</span>' : '') + '</div>';
  }

  root.innerHTML =
    '<section class="card team-head"><span class="avatar avatar-lg tone-neutral">' + p.shirtNumber + '</span><div class="team-head-text"><h2>' + App.esc(p.fullName) + '</h2>' +
    '<p class="muted"><a href="/teams/' + p.teamId + '">' + App.esc(p.team) + '</a></p><div class="chips">' + App.render.positionBadge(p.position) +
    (s.suspendedMatches > 0 ? App.render.statusBadge('error', 'Đang treo giò ' + s.suspendedMatches + ' trận') : '') + '</div></div></section>' +
    '<div class="stats">' +
    stat('Số trận', s.matches, 'trong đội hình các trận đã kết thúc') +
    stat('Bàn thắng', s.goals, s.ownGoals ? s.ownGoals + ' bàn phản lưới nhà (không tính)' : 'không tính phản lưới nhà') +
    stat('Thẻ vàng', '<span class="with-card"><i class="card-yellow"></i>' + s.yellowCards + '</span>') +
    stat('Thẻ đỏ', '<span class="with-card"><i class="card-red"></i>' + s.redCards + '</span>') + '</div>';
};
