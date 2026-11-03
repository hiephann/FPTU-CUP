/* Nộp đội hình (FR29): đúng 7 cầu thủ xuất phát, đúng 1 thủ môn, tối đa 7 dự bị, không ai bị treo giò (BR19). */
App.main = async function () {
  var root = App.$('#lineup-root');
  var matchId = +location.pathname.split('/').pop();

  var mres = await App.get('/api/matches/' + matchId);
  if (!mres.ok) { root.innerHTML = '<p class="muted">' + App.esc(mres.message) + '</p><a class="btn" href="/manager/team">Về đội của tôi</a>'; return; }
  var m = mres.data;

  var mine = (await App.get('/api/teams/mine')).data || [];
  var team = mine.filter(function (t) { return t.id === m.homeTeam.id || t.id === m.awayTeam.id; })[0];
  if (!team) { root.innerHTML = '<p class="muted">Đội của bạn không thi đấu trận này.</p><a class="btn" href="/manager/team">Về đội của tôi</a>'; return; }

  var isHome = team.id === m.homeTeam.id;
  var mySide = isHome ? m.homeTeam : m.awayTeam;
  var opponent = isHome ? m.awayTeam : m.homeTeam;
  var deadline = new Date(m.lineupDeadline);
  var overdue = App.vnNow() > deadline;
  var locked = m.status !== 'Scheduled' || overdue;

  var res = await Promise.all([
    App.get('/api/teams/' + team.id + '/players'),
    App.get('/api/tournaments/' + m.tournamentId + '/suspensions'),
  ]);
  var players = res[0].ok ? res[0].data : [];
  var suspended = {};
  (res[1].ok ? res[1].data : []).forEach(function (s) { suspended[s.playerId] = s.reason; });

  var role = {};
  mySide.startingLineup.forEach(function (p) { role[p.playerId] = 'starter'; });
  mySide.substitutes.forEach(function (p) { role[p.playerId] = 'bench'; });

  root.innerHTML =
    '<div class="row-between" style="align-items:flex-end"><div><span class="eyebrow">Vòng ' + m.round + ' · ' + (isHome ? 'Sân nhà' : 'Sân khách') + '</span>' +
    '<h1>' + App.esc(team.name) + ' vs ' + App.esc(opponent.name) + '</h1><p class="muted">' + App.dayLongFull(m.kickoffTime) + ' · ' + App.hhmm(m.kickoffTime) + ' · ' + App.esc(m.venue || 'Chưa có sân') + '</p></div>' +
    '<a class="btn" href="/manager/team">Danh sách cầu thủ</a></div>' +
    '<div class="banner ' + (locked ? 'banner-warn' : 'banner-info') + '">' + (
      m.forfeitReason ? '<b>' + (m.forfeitTeamId ? 'Xử thua:' : 'Hủy trận:') + '</b> ' + App.esc(m.forfeitReason)
      : m.status !== 'Scheduled' ? 'Trận đang ở trạng thái ' + App.esc(App.MATCH_STATUS[m.status]) + ': đội hình đã bị khóa và không sửa được (BR20).'
      : overdue ? 'Đã quá hạn nộp đội hình (' + App.hhmmDmy(m.lineupDeadline) + '). Đội không nộp đội hình sẽ bị xử thua 0–3 (BR23).'
      : 'Hạn nộp đội hình: <b>' + App.hhmmDmy(m.lineupDeadline) + '</b> (trước giờ đá 1 ngày) – quá hạn mà chưa nộp sẽ bị xử thua 0–3 (BR23). ' +
        'Sau khi trận bắt đầu, đội hình bị khóa kể cả khi có thay người (BR20). Cầu thủ đang bị treo giò không được đăng ký (BR19).') + '</div>' +
    '<form id="lineup-form"><div class="lineup-summary" aria-live="polite">' +
    '<div class="summary-item" data-summary="starters"><span class="display">0</span><small>/ 7 xuất phát</small></div>' +
    '<div class="summary-item" data-summary="gk"><span class="display">0</span><small>/ 1 thủ môn</small></div>' +
    '<div class="summary-item" data-summary="bench"><span class="display">0</span><small>dự bị (tối đa 7)</small></div>' +
    '<button class="btn btn-primary" id="lineup-submit" type="submit" disabled>Nộp đội hình</button></div>' +
    '<ul class="issues" id="lineup-issues"></ul>' +
    '<section class="panel"><div class="table-scroll"><table class="tbl tbl-left lineup-table"><thead><tr><th>Số áo</th><th>Cầu thủ</th><th>Vị trí</th><th>Vai trò trong trận</th></tr></thead><tbody>' +
    players.map(function (p) {
      var susp = suspended[p.id];
      var r = susp ? '' : (role[p.id] || '');
      var dis = (susp || locked) ? ' disabled' : '';
      function radio(value, label) {
        return '<label><input type="radio" name="role-' + p.id + '" value="' + value + '"' + (r === value ? ' checked' : '') + dis + '><span>' + label + '</span></label>';
      }
      return '<tr data-player data-id="' + p.id + '" data-pos="' + p.position + '"' + (susp ? ' class="is-suspended"' : '') + '><td class="time">' + p.shirtNumber + '</td>' +
        '<td><b>' + App.esc(p.fullName) + '</b>' + (susp ? '<br><span class="unassigned small">Đang treo giò: ' + App.esc(susp) + '</span>' : '') + '</td>' +
        '<td>' + App.render.positionBadge(p.position) + '</td><td><div class="segmented" role="radiogroup" aria-label="Vai trò của ' + App.esc(p.fullName) + '">' +
        radio('starter', 'Xuất phát') + radio('bench', 'Dự bị') + radio('', 'Không đăng ký') + '</div></td></tr>';
    }).join('') + '</tbody></table></div></section></form>';

  var form = App.$('#lineup-form');
  var rows = App.$$('[data-player]', form);
  var submit = App.$('#lineup-submit');

  function roleOf(row) {
    var c = App.$('input[type=radio]:checked', row);
    return c ? c.value : '';
  }
  function setSummary(key, value, state) {
    var el = App.$('[data-summary="' + key + '"]', form);
    App.$('.display', el).textContent = value;
    el.classList.toggle('is-ok', state === 'ok');
    el.classList.toggle('is-bad', state === 'bad');
  }
  function refresh() {
    var starters = 0, gk = 0, bench = 0;
    rows.forEach(function (row) {
      var r = roleOf(row);
      if (r === 'starter') { starters++; if (row.dataset.pos === 'GK') gk++; } else if (r === 'bench') bench++;
      row.classList.toggle('is-starter', r === 'starter');
      row.classList.toggle('is-bench', r === 'bench');
    });
    var issues = [];
    if (starters !== 7) issues.push('Cần đúng 7 cầu thủ xuất phát (đang có ' + starters + ').');
    if (gk !== 1) issues.push('Cần đúng 1 thủ môn trong đội hình xuất phát (đang có ' + gk + ').');
    if (bench > 7) issues.push('Tối đa 7 cầu thủ dự bị (đang có ' + bench + ').');
    setSummary('starters', starters, starters === 7 ? 'ok' : starters > 7 ? 'bad' : '');
    setSummary('gk', gk, gk === 1 ? 'ok' : gk > 1 ? 'bad' : '');
    setSummary('bench', bench, bench > 7 ? 'bad' : bench > 0 ? 'ok' : '');
    App.$('#lineup-issues').innerHTML = issues.map(function (x) { return '<li>' + x + '</li>'; }).join('');
    submit.disabled = locked || issues.length > 0;
  }

  form.addEventListener('change', refresh);
  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    if (submit.disabled) return;
    var body = { starters: [], substitutes: [] };
    rows.forEach(function (row) {
      var r = roleOf(row);
      if (r === 'starter') body.starters.push(+row.dataset.id);
      else if (r === 'bench') body.substitutes.push(+row.dataset.id);
    });
    App.report(await App.put('/api/matches/' + matchId + '/lineups/' + team.id, body), 'Đã nộp đội hình');
  });

  refresh();
};
