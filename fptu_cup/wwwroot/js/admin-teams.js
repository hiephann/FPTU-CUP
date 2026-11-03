/* Duyệt / từ chối đội (FR09): PATCH /api/teams/{id}/approval – kiểm tra BR01, BR05 ở server. */
App.main = function () {
  var $ = App.$;
  var t = App.tournament;
  var teams = [];
  var filter = 'all';

  if (!t) { $('#team-table').innerHTML = '<p class="muted">Chưa có giải đấu nào.</p>'; return; }
  $('#teams-sub').textContent = t.name + ' · ' + App.TOURNAMENT_STATUS[t.status];
  $('#approved-max').textContent = ' / ' + t.maxTeams;
  if (t.status !== 'Registration') {
    $('#teams-lock').textContent = 'Giải đang ở trạng thái ' + t.status + ': chỉ duyệt đội khi giải ở trạng thái Registration. Chọn giải khác ở thanh trên để duyệt đội.';
    $('#teams-lock').hidden = false;
  }

  var FILTERS = [['all', 'Tất cả'], ['Pending', 'Chờ duyệt'], ['Approved', 'Đã duyệt'], ['Rejected', 'Từ chối']];
  var BADGE = { Pending: 'warn', Approved: 'ok', Rejected: 'error' };

  function render() {
    var approved = teams.filter(function (x) { return x.status === 'Approved'; }).length;
    $('#approved-count').textContent = approved;

    $('#team-filters').innerHTML = FILTERS.map(function (f) {
      var n = f[0] === 'all' ? teams.length : teams.filter(function (x) { return x.status === f[0]; }).length;
      return '<button class="chip' + (filter === f[0] ? ' is-active' : '') + '" data-filter="' + f[0] + '">' + f[1] + ' (' + n + ')</button>';
    }).join('');

    var rows = teams.filter(function (x) { return filter === 'all' || x.status === filter; });
    if (!rows.length) { $('#team-table').innerHTML = '<p class="muted">Không có đội nào.</p>'; return; }
    $('#team-table').innerHTML = '<div class="table-scroll"><table class="tbl tbl-left"><thead><tr><th>Đội</th><th>Khoa</th><th>Quản lý</th><th>Cầu thủ</th><th>Trạng thái</th><th><span class="sr-only">Thao tác</span></th></tr></thead><tbody>' +
      rows.map(function (x) {
        var enough = x.playerCount >= 7 && x.playerCount <= 14;
        var open = t.status === 'Registration';
        var pending = x.status === 'Pending';
        var full = approved >= t.maxTeams && x.status !== 'Approved';
        var why = !enough ? 'Cần 7–14 cầu thủ (BR01)' : full ? 'Đã đủ số đội tối đa (BR05)' : '';
        return '<tr><td><span class="team-cell">' + App.avatar(x.id, x.name, 'sm') + App.esc(x.name) + '</span></td><td>' + App.esc(x.faculty || '—') + '</td><td>' + App.esc(x.managerName) + '</td>' +
          '<td><span class="' + (enough ? '' : 'unassigned') + '">' + x.playerCount + '</span>' + (enough ? '' : '<br><span class="note">Chưa đủ 7 cầu thủ (BR01)</span>') + '</td>' +
          '<td>' + App.render.statusBadge(BADGE[x.status], App.TEAM_STATUS[x.status]) + '</td>' +
          '<td class="al-right"><div class="cell-actions">' +
          '<button class="btn btn-sm btn-primary" data-act="approve" data-id="' + x.id + '"' + (open && x.status !== 'Approved' && enough && !full ? '' : ' disabled') + (why ? ' title="' + why + '"' : '') + '>Duyệt</button>' +
          '<button class="btn btn-sm btn-danger" data-act="reject" data-id="' + x.id + '"' + (open && pending ? '' : ' disabled') + '>Từ chối</button>' +
          '</div></td></tr>';
      }).join('') + '</tbody></table></div>';
  }

  async function load() {
    var res = await App.getAll('/api/tournaments/' + t.id + '/teams');
    teams = res.ok ? res.data : [];
    render();
  }

  $('#team-filters').addEventListener('click', function (e) {
    var b = e.target.closest('[data-filter]');
    if (b) { filter = b.dataset.filter; render(); }
  });

  $('#team-table').addEventListener('click', async function (e) {
    var btn = e.target.closest('[data-act]');
    if (!btn || btn.disabled) return;
    var team = teams.filter(function (x) { return x.id === +btn.dataset.id; })[0];
    var body = { approved: btn.dataset.act === 'approve', reason: null };
    if (!body.approved) {
      var v = await App.formDialog({ title: 'Từ chối ' + team.name, fields: [{ name: 'reason', label: 'Lý do (không bắt buộc)' }], submitText: 'Từ chối' });
      if (!v) return;
      body.reason = v.reason || null;
    }
    if (App.report(await App.patch('/api/teams/' + team.id + '/approval', body))) load();
  });

  load();
};
