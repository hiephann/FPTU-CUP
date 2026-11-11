/* Quản lý đội (FR08, FR12): đăng ký đội, sửa thông tin, thêm/sửa/xóa cầu thủ, lối vào nộp đội hình. */
App.main = async function () {
  var $ = App.$;
  var teams = [], team = null, players = [], upcoming = [];

  var POSITIONS = ['GK', 'DF', 'MF', 'FW'].map(function (p) { return { value: p, label: p }; });
  var STATUS_BADGE = { Pending: 'warn', Approved: 'ok', Rejected: 'error' };

  function playerFields(p) {
    p = p || {};
    return [
      { name: 'studentCode', label: 'MSSV', value: p.studentCode || '', required: true },
      { name: 'fullName', label: 'Họ và tên', value: p.fullName || '', required: true },
      { name: 'shirtNumber', label: 'Số áo (1–99)', type: 'number', min: 1, max: 99, value: p.shirtNumber || '', required: true },
      { name: 'position', label: 'Vị trí', type: 'select', value: p.position || 'FW', options: POSITIONS },
      { name: 'dateOfBirth', label: 'Ngày sinh', type: 'date', value: p.dateOfBirth || '' },
    ];
  }
  function playerBody(v) {
    return { studentCode: v.studentCode.trim(), fullName: v.fullName.trim(), shirtNumber: +v.shirtNumber, position: v.position, dateOfBirth: v.dateOfBirth || null };
  }

  function render() {
    // BR04: chỉ sửa cầu thủ khi giải đang Registration – Admin (quyền tối cao) được sửa mọi lúc.
    var editable = team.tournamentStatus === 'Registration' || App.isAdmin();
    var count = players.length;
    var enough = count >= 7 && count <= 14;
    var percent = Math.min(100, Math.round(count * 100 / 14));

    var html =
      '<div class="split"><section class="panel panel-main team-head">' + App.avatar(team.id, team.name, 'lg') +
      '<div class="team-head-text"><h2>' + App.esc(team.name) + ' ' + App.teamCode(team.name) + '</h2><p class="muted">' + App.esc(team.tournament) + '</p><div class="chips">' +
      App.render.statusBadge(STATUS_BADGE[team.status], App.TEAM_STATUS[team.status]) +
      '<span class="pill pill-chip">Khoa: ' + App.esc(team.faculty || '—') + '</span><span class="pill pill-chip">Màu áo: ' + App.esc(team.kitColor || '—') + '</span>' +
      '<button class="btn btn-sm" id="edit-team">Sửa thông tin đội</button>' +
      // Upload logo: PNG/JPG/WEBP/GIF tối đa 1 MB -> POST /api/teams/{id}/logo (multipart)
      '<label class="btn btn-sm mb-0" for="logo-file">' + (team.logoUrl ? 'Đổi logo' : 'Tải logo lên') + '</label>' +
      '<input type="file" id="logo-file" accept="image/png,image/jpeg,image/webp,image/gif" hidden>' +
      (team.logoUrl ? '<button class="btn btn-sm btn-danger" id="delete-logo">Xóa logo</button>' : '') +
      '</div><span class="note">Logo: PNG, JPG, WEBP hoặc GIF, tối đa 1 MB; nên dùng ảnh vuông.</span></div></section>' +
      '<section class="panel panel-side"><span class="label muted">Số cầu thủ</span><span class="display big-number">' + count + '<small> / 14</small></span>' +
      '<span class="progress"><span style="width:' + percent + '%"></span></span>' +
      (enough ? '<span class="note">Đủ điều kiện (tối thiểu 7, tối đa 14 cầu thủ).</span>' : '<span class="note unassigned">Chưa đủ 7 cầu thủ, đội chưa được duyệt (BR01).</span>') +
      '</section></div>';

    if (!editable)
      html += '<div class="banner banner-warn"><b>Danh sách cầu thủ đang bị khóa.</b> Chỉ thêm, sửa, xóa cầu thủ khi giải ở trạng thái Registration (BR04); giải này đang ở trạng thái ' + App.esc(team.tournamentStatus) + '.</div>';

    html += '<section class="panel"><div class="card-head"><h2>Danh sách cầu thủ</h2><span class="note">Số áo duy nhất trong đội (BR03) · MSSV chỉ thuộc một đội trong giải (BR02)</span></div>' +
      '<div class="table-scroll"><table class="tbl tbl-left"><thead><tr><th>Số áo</th><th>Họ và tên</th><th>MSSV</th><th>Vị trí</th><th>Ngày sinh</th><th><span class="sr-only">Thao tác</span></th></tr></thead><tbody>' +
      players.map(function (p) {
        return '<tr><td class="time">' + p.shirtNumber + '</td><td style="font-weight:600">' + App.esc(p.fullName) + '</td><td>' + App.esc(p.studentCode) + '</td><td>' +
          App.render.positionBadge(p.position) + '</td><td>' + (p.dateOfBirth ? App.dmy(p.dateOfBirth) : '—') + '</td><td class="al-right"><div class="cell-actions">' +
          '<button class="btn btn-sm" data-act="edit" data-id="' + p.id + '"' + (editable ? '' : ' disabled') + '>Sửa</button>' +
          '<button class="btn btn-sm btn-danger" data-act="delete" data-id="' + p.id + '"' + (editable ? '' : ' disabled') + '>Xóa</button></div></td></tr>';
      }).join('') + '</tbody></table></div>' +
      '<div><button class="btn btn-primary" id="add-player"' + (editable && count < 14 ? '' : ' disabled') + '>Thêm cầu thủ</button></div></section>';

    html += '<section class="panel"><h2>Trận sắp tới</h2>' + (upcoming.length ? '<div class="table-scroll"><table class="tbl tbl-left"><tbody>' +
      upcoming.map(function (m) {
        var late = App.vnNow() > new Date(m.lineupDeadline);
        return '<tr><td>Vòng ' + m.round + '</td><td>' + App.day(m.kickoffTime) + ' · <b>' + App.hhmm(m.kickoffTime) + '</b></td><td style="font-weight:600">' +
          App.teamLabel(m.homeTeamId, m.homeTeam) + ' <span class="vs">vs</span> ' + App.teamLabel(m.awayTeamId, m.awayTeam) + '</td><td>' + App.esc(m.venue || '—') + '</td>' +
          '<td>' + App.render.statusBadge(m.lineupsSubmitted === 2 ? 'ok' : 'warn', m.lineupsSubmitted + '/2 đội đã nộp') + '</td>' +
          '<td class="' + (late ? 'unassigned' : '') + '">Hạn nộp: ' + App.hhmmDmy(m.lineupDeadline) + (late ? ' (quá hạn)' : '') + '</td>' +
          '<td class="al-right"><a class="btn btn-sm btn-primary" href="/manager/lineup/' + m.id + '">' + (late ? 'Xem' : 'Nộp đội hình') + '</a></td></tr>';
      }).join('') + '</tbody></table></div>' : '<p class="muted">Chưa có trận nào sắp diễn ra.</p>') + '</section>';

    $('#team-area').innerHTML = html;
  }

  async function loadTeam() {
    var res = await Promise.all([
      App.get('/api/teams/' + team.id + '/players'),
      App.get('/api/matches?teamId=' + team.id + '&status=Scheduled&pageSize=4'),
    ]);
    players = res[0].ok ? res[0].data : [];
    upcoming = res[1].ok ? res[1].data.items : [];
    render();
  }

  async function loadTeams(keepId) {
    var res = await App.get('/api/teams/mine');
    teams = res.ok ? res.data : [];
    if (!teams.length) {
      $('#team-area').innerHTML = '<section class="panel"><p class="muted">Bạn chưa đăng ký đội nào. Đăng ký đội ở bên dưới khi có giải đang mở đăng ký.</p></section>';
      return;
    }
    var wanted = keepId || +new URLSearchParams(location.search).get('team');
    team = teams.filter(function (x) { return x.id === wanted; })[0] || teams[0];
    var select = $('#team-select');
    select.hidden = teams.length < 2;
    select.innerHTML = teams.map(function (x) { return '<option value="' + x.id + '"' + (x.id === team.id ? ' selected' : '') + '>' + App.esc(x.name) + ' – ' + App.esc(x.tournament) + '</option>'; }).join('');
    await loadTeam();
  }

  $('#team-select').addEventListener('change', function (e) {
    team = teams.filter(function (x) { return x.id === +e.target.value; })[0];
    loadTeam();
  });

  /** Gửi file logo dạng multipart (không qua App.api vì App.api luôn gửi JSON). */
  async function uploadLogo(file, teamId) {
    if (file.size > 1024 * 1024) { App.toast('Logo tối đa 1 MB', 'error'); return false; }
    var data = new FormData();
    data.append('file', file);
    var res = await fetch('/api/teams/' + teamId + '/logo', { method: 'POST', headers: { Authorization: 'Bearer ' + App.token() }, body: data })
      .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, data: j.data, message: j.message }; }); })
      .catch(function () { return { ok: false, message: 'Không kết nối được máy chủ' }; });
    if (App.report(res)) { App.setLogo(teamId, res.data.logoUrl); return true; }
    return false;
  }

  $('#team-area').addEventListener('change', async function (e) {
    if (e.target.id === 'logo-file' && e.target.files.length && await uploadLogo(e.target.files[0], team.id)) loadTeams(team.id);
  });

  $('#team-area').addEventListener('click', async function (e) {
    var btn = e.target.closest('button');
    if (!btn || btn.disabled) return;

    if (btn.id === 'delete-logo') {
      if (await App.confirm('Xóa logo của ' + team.name + '?') && App.report(await App.del('/api/teams/' + team.id + '/logo'))) {
        App.setLogo(team.id, null);
        loadTeams(team.id);
      }
      return;
    }

    if (btn.id === 'add-player') {
      var v = await App.formDialog({ title: 'Thêm cầu thủ', fields: playerFields(), submitText: 'Thêm' });
      if (v && App.report(await App.post('/api/teams/' + team.id + '/players', playerBody(v)))) loadTeams(team.id);
    } else if (btn.id === 'edit-team') {
      var t = await App.formDialog({
        title: 'Sửa thông tin đội',
        fields: [
          { name: 'name', label: 'Tên đội', value: team.name, required: true },
          { name: 'faculty', label: 'Khoa', value: team.faculty || '' },
          { name: 'kitColor', label: 'Màu áo', value: team.kitColor || '' },
        ],
      });
      // Logo đổi bằng nút "Tải logo lên"/"Xóa logo"; gửi kèm logo hiện tại để PUT không xóa mất logo.
      if (t) t.logoUrl = team.logoUrl || null;
      if (t && App.report(await App.put('/api/teams/' + team.id, t))) loadTeams(team.id);
    } else if (btn.dataset.act) {
      var p = players.filter(function (x) { return x.id === +btn.dataset.id; })[0];
      if (btn.dataset.act === 'edit') {
        var u = await App.formDialog({ title: 'Sửa cầu thủ', fields: playerFields(p) });
        if (u && App.report(await App.put('/api/players/' + p.id, playerBody(u)))) loadTeams(team.id);
      } else if (await App.confirm('Xóa cầu thủ ' + p.fullName + '?')) {
        if (App.report(await App.del('/api/players/' + p.id))) loadTeams(team.id);
      }
    }
  });

  // Đăng ký đội mới vào giải đang mở đăng ký (FR08)
  var open = (await App.get('/api/tournaments?status=Registration&pageSize=50'));
  var openList = open.ok ? open.data.items : [];
  if (openList.length) {
    $('#register-panel').hidden = false;
    $('#r-tournament').innerHTML = openList.map(function (x) { return '<option value="' + x.id + '">' + App.esc(x.name) + '</option>'; }).join('');
    var form = $('#register-team-form');
    form.addEventListener('submit', async function (e) {
      e.preventDefault();
      var f = form.elements;
      var logo = f.logo.files[0];
      if (logo && logo.size > 1024 * 1024) { App.toast('Logo tối đa 1 MB', 'error'); return; }
      var body = { name: f.name.value.trim(), faculty: f.faculty.value.trim() || null, kitColor: f.kitColor.value.trim() || null };
      var res = await App.post('/api/tournaments/' + f.tournamentId.value + '/teams', body);
      if (!App.report(res)) return;
      // Đội tạo xong mới có id -> upload logo (nếu có chọn) bằng POST /api/teams/{id}/logo
      if (logo) await uploadLogo(logo, res.data.id);
      form.reset();
      loadTeams(res.data.id);
    });
  }

  loadTeams();
};
