/* Lịch & trận đấu (FR17–FR20, FR24): xem theo vòng, phân công trọng tài, đổi lịch, hoãn/hủy, sửa kết quả, tạo trận, sinh lịch. */
App.main = async function () {
  var $ = App.$;
  var t = App.tournament;
  if (!t) { $('#match-table').innerHTML = '<p class="muted">Chưa có giải đấu nào.</p>'; return; }

  var matches = [], referees = [], venues = [], teams = [];
  var maxRound = 0, round = 0;

  var STATUS_BADGE = { Scheduled: 'neutral', InProgress: 'ok', Finished: 'ok', Postponed: 'warn', Cancelled: 'error' };

  function options(list, valueKey, labelKey, blank) {
    return (blank ? '<option value="">' + blank + '</option>' : '') + list.map(function (x) {
      return '<option value="' + x[valueKey] + '">' + App.esc(x[labelKey]) + '</option>';
    }).join('');
  }

  function renderTable() {
    var rows = matches.filter(function (m) { return m.round === round; });
    $('#matches-sub').textContent = t.name + (rows.length ? ' · Vòng ' + round + ' · ' + App.dayLongFull(rows[0].kickoffTime) : '');
    if (!rows.length) {
      $('#match-table').innerHTML = '<p class="muted">' + (matches.length ? 'Vòng này chưa có trận.' : 'Giải chưa có lịch thi đấu. Dùng form “Sinh lịch vòng tròn” bên dưới.') + '</p>';
      return;
    }
    $('#match-table').innerHTML = '<div class="table-scroll"><table class="tbl tbl-left"><thead><tr><th>Giờ</th><th>Trận đấu</th><th>Sân</th><th>Trọng tài</th><th>Đội hình</th><th>Trạng thái</th><th><span class="sr-only">Thao tác</span></th></tr></thead><tbody>' +
      rows.map(function (m) {
        var done = m.status === 'Finished';
        var open = m.status === 'Scheduled' || m.status === 'Postponed';
        var refSelect = '<select class="input input-sm" data-ref="' + m.id + '"' + (open ? '' : ' disabled') + '><option value="">Chưa phân công</option>' +
          referees.map(function (r) { return '<option value="' + r.id + '"' + (r.id === m.refereeId ? ' selected' : '') + '>' + App.esc(r.fullName) + '</option>'; }).join('') + '</select>';
        var lineKind = m.lineupsSubmitted === 2 ? 'ok' : m.lineupsSubmitted === 1 ? 'warn' : 'error';
        var statusKind = m.forfeitReason ? 'warn' : STATUS_BADGE[m.status];
        return '<tr><td class="time">' + App.hhmm(m.kickoffTime) + '</td>' +
          '<td class="wrap" style="font-weight:600">' + App.teamLabel(m.homeTeamId, m.homeTeam) + (done ? ' <span class="display score-inline">' + m.homeScore + '–' + m.awayScore + '</span> ' : ' <span class="vs">vs</span> ') + App.teamLabel(m.awayTeamId, m.awayTeam) + '</td>' +
          '<td>' + App.esc(m.venue || '—') + '</td><td>' + refSelect + '</td>' +
          '<td>' + (done || m.status === 'Cancelled' ? '' : App.render.statusBadge(lineKind, m.lineupsSubmitted + '/2 đội') + '<br><span class="note">hạn ' + App.hhmmDmy(m.lineupDeadline) + '</span>') + '</td>' +
          '<td class="wrap" title="' + App.esc(m.forfeitReason || '') + '">' + App.render.statusBadge(statusKind, App.matchStatusText(m)) +
          (m.forfeitReason ? '<br><span class="note">' + App.esc(m.forfeitReason) + '</span>' : '') + '</td>' +
          '<td class="al-right"><div class="cell-actions">' +
          (done ? '<button class="btn btn-sm" data-act="result" data-id="' + m.id + '">Sửa kết quả</button>' : '') +
          (open ? '<button class="btn btn-sm" data-act="edit" data-id="' + m.id + '">Đổi lịch</button>' : '') +
          (m.status === 'Scheduled' ? '<button class="btn btn-sm" data-act="Postponed" data-id="' + m.id + '">Hoãn</button><button class="btn btn-sm btn-danger" data-act="Cancelled" data-id="' + m.id + '">Hủy</button>' : '') +
          '</div></td></tr>';
      }).join('') + '</tbody></table></div>';
  }

  function renderChips() {
    var firstOpen = matches.filter(function (m) { return m.status !== 'Finished' && m.status !== 'Cancelled'; })[0];
    var current = firstOpen ? firstOpen.round : maxRound;
    var html = '';
    for (var r = 1; r <= maxRound; r++)
      html += '<a class="chip' + (r === round ? ' is-active' : '') + '" href="/admin/matches?round=' + r + '">' + r + (r === current ? ' •' : '') + '</a>';
    $('#round-chips').innerHTML = html;
    return current;
  }

  async function load() {
    var res = await App.getAll('/api/matches?tournamentId=' + t.id);
    matches = res.ok ? res.data : [];
    maxRound = matches.reduce(function (m, x) { return Math.max(m, x.round); }, 0);
    var current = renderChips();
    if (!round) round = +new URLSearchParams(location.search).get('round') || current || 1;
    renderChips();
    renderTable();
  }

  // Dữ liệu tham chiếu cho form
  var refs = await Promise.all([
    App.get('/api/referees'),
    App.get('/api/venues?pageSize=50'),
    App.getAll('/api/tournaments/' + t.id + '/teams?status=Approved'),
  ]);
  referees = refs[0].ok ? refs[0].data : [];
  venues = refs[1].ok ? refs[1].data.items : [];
  teams = refs[2].ok ? refs[2].data : [];

  $('#g-venues').innerHTML = venues.map(function (v) {
    return '<label class="check"><input type="checkbox" name="venue" value="' + v.id + '" checked> ' + App.esc(v.name) + '</label>';
  }).join('');
  $('#m-home').innerHTML = options(teams, 'id', 'name');
  $('#m-away').innerHTML = options(teams.slice().reverse(), 'id', 'name');
  $('#m-venue').innerHTML = options(venues, 'id', 'name', 'Chưa chọn sân');
  $('#m-ref').innerHTML = options(referees, 'id', 'fullName', 'Chưa phân công');

  // Phân công trọng tài / đổi lịch / hoãn / hủy / sửa kết quả
  $('#match-table').addEventListener('change', async function (e) {
    var sel = e.target.closest('[data-ref]');
    if (!sel) return;
    var m = matches.filter(function (x) { return x.id === +sel.dataset.ref; })[0];
    var res = await App.put('/api/matches/' + m.id, { kickoffTime: m.kickoffTime, venueId: m.venueId, refereeId: sel.value ? +sel.value : null });
    App.report(res, 'Đã cập nhật trọng tài');
    load();
  });

  $('#match-table').addEventListener('click', async function (e) {
    var btn = e.target.closest('[data-act]');
    if (!btn) return;
    var m = matches.filter(function (x) { return x.id === +btn.dataset.id; })[0];
    var act = btn.dataset.act;

    if (act === 'Postponed' || act === 'Cancelled') {
      if (await App.confirm((act === 'Postponed' ? 'Hoãn' : 'Hủy') + ' trận ' + m.homeTeam + ' vs ' + m.awayTeam + '?')) {
        if (App.report(await App.patch('/api/matches/' + m.id + '/status', { status: act }))) load();
      }
    } else if (act === 'edit') {
      var v = await App.formDialog({
        title: 'Đổi lịch ' + m.homeTeam + ' vs ' + m.awayTeam,
        fields: [
          { name: 'kickoffTime', label: 'Giờ đá', type: 'datetime-local', value: App.inputDateTime(m.kickoffTime), required: true },
          { name: 'venueId', label: 'Sân', type: 'select', value: m.venueId || '', options: [{ value: '', label: 'Chưa chọn sân' }].concat(venues.map(function (x) { return { value: x.id, label: x.name }; })) },
        ],
      });
      if (v) {
        var body = { kickoffTime: v.kickoffTime, venueId: v.venueId ? +v.venueId : null, refereeId: m.refereeId };
        if (App.report(await App.put('/api/matches/' + m.id, body))) load();
      }
    } else if (act === 'result') {
      var r = await App.formDialog({
        title: 'Sửa kết quả ' + m.homeTeam + ' vs ' + m.awayTeam,
        fields: [
          { name: 'homeScore', label: 'Bàn thắng ' + m.homeTeam, type: 'number', min: 0, value: m.homeScore, required: true },
          { name: 'awayScore', label: 'Bàn thắng ' + m.awayTeam, type: 'number', min: 0, value: m.awayScore, required: true },
          { name: 'reason', label: 'Lý do sửa', hint: 'Mọi thay đổi được ghi lại người sửa và thời điểm (BR15).' },
        ],
      });
      if (r) {
        if (App.report(await App.put('/api/matches/' + m.id + '/result', { homeScore: +r.homeScore, awayScore: +r.awayScore, reason: r.reason || null }))) load();
      }
    }
  });

  // Xử lý ngay các trận quá hạn nộp đội hình (BR23) – thường để worker nền tự chạy mỗi phút
  $('#process-forfeits').addEventListener('click', async function () {
    if (await App.confirm('Xử thua / hủy ngay các trận đã quá hạn nộp đội hình?')) {
      if (App.report(await App.post('/api/matches/process-forfeits', {}))) load();
    }
  });

  // Sinh lịch vòng tròn
  var gen = $('#generate-form');
  gen.addEventListener('submit', async function (e) {
    e.preventDefault();
    var venueIds = App.$$('input[name="venue"]:checked', gen).map(function (c) { return +c.value; });
    var body = {
      firstKickoff: gen.elements.firstKickoff.value,
      daysBetweenRounds: +gen.elements.daysBetweenRounds.value,
      kickoffTimes: gen.elements.kickoffTimes.value.split(',').map(function (x) { return x.trim(); }).filter(Boolean),
      venueIds: venueIds,
    };
    var res = await App.post('/api/tournaments/' + t.id + '/matches/generate', body);
    if (App.report(res, res.ok ? 'Đã sinh ' + res.data.totalMatches + ' trận trong ' + res.data.totalRounds + ' vòng' : '')) { round = 0; load(); }
  });

  // Tạo trận thủ công
  var create = $('#create-form');
  create.addEventListener('submit', async function (e) {
    e.preventDefault();
    var f = create.elements;
    var body = {
      tournamentId: t.id,
      round: +f.round.value,
      homeTeamId: +f.homeTeamId.value,
      awayTeamId: +f.awayTeamId.value,
      venueId: f.venueId.value ? +f.venueId.value : null,
      refereeId: f.refereeId.value ? +f.refereeId.value : null,
      kickoffTime: f.kickoffTime.value,
    };
    if (App.report(await App.post('/api/matches', body))) { round = body.round; load(); }
  });

  load();
};
