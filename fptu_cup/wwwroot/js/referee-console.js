/*
 * Bảng điều khiển trận của trọng tài (FR22, FR23, FR30).
 * Mọi thao tác gọi API; server kiểm tra BR09, BR10, BR14, BR20, BR21, BR22 và trả lỗi hiển thị lại ở đây.
 */
App.main = async function () {
  var root = App.$('#console-root');
  var matchId = +location.pathname.split('/').pop();
  var me = App.user();
  var m = null;
  var tab = 'goal';

  async function load() {
    var res = await App.get('/api/matches/' + matchId);
    if (!res.ok) { root.innerHTML = '<p class="muted">' + App.esc(res.message) + '</p><a class="btn" href="/referee/matches">Danh sách trận</a>'; return false; }
    m = res.data;
    return true;
  }

  /** Cầu thủ đang trên sân / dự bị chưa vào sân của một đội, dựng từ đội hình + các lần thay người + thẻ đỏ. */
  function pitch(team) {
    var on = {}, bench = {}, all = {};
    team.startingLineup.forEach(function (p) { on[p.playerId] = p; all[p.playerId] = p; });
    team.substitutes.forEach(function (p) { bench[p.playerId] = p; all[p.playerId] = p; });
    m.events.forEach(function (e) {
      if (e.type === 'Substitution' && e.teamId === team.id) {
        delete on[e.playerOutId];
        if (bench[e.playerInId]) { on[e.playerInId] = bench[e.playerInId]; delete bench[e.playerInId]; }
      }
      if (e.type === 'RedCard' && e.teamId === team.id) delete on[e.playerId];
    });
    return { on: Object.keys(on).map(function (k) { return on[k]; }), bench: Object.keys(bench).map(function (k) { return bench[k]; }), all: all };
  }

  function playerOptions(list) {
    return list.slice().sort(function (a, b) { return a.shirtNumber - b.shirtNumber; })
      .map(function (p) { return '<option value="' + p.playerId + '">#' + p.shirtNumber + ' ' + App.esc(p.name) + '</option>'; }).join('');
  }

  function eventText(e) {
    var team = e.teamId === m.homeTeam.id ? m.homeTeam.name : m.awayTeam.name;
    if (e.type === 'Goal') return 'Bàn thắng – ' + e.player + ' (' + team + ')' + (e.isOwnGoal ? ', phản lưới nhà' : '');
    if (e.type === 'YellowCard') return 'Thẻ vàng – ' + e.player + ' (' + team + ')';
    if (e.type === 'RedCard') return 'Thẻ đỏ – ' + e.player + ' (' + team + ')';
    return 'Thay người (' + team + ') – vào: ' + e.playerIn + ', ra: ' + e.playerOut;
  }
  var DELETE_PATH = { Goal: 'goals', YellowCard: 'cards', RedCard: 'cards', Substitution: 'substitutions' };

  function render() {
    var live = m.status === 'InProgress';
    var scheduled = m.status === 'Scheduled';
    var finished = m.status === 'Finished';
    var assigned = m.refereeId === me.id;
    var home = m.homeTeam, away = m.awayTeam;
    var bothLineups = home.lineupSubmitted && away.lineupSubmitted;

    var hint;
    if (!assigned) hint = '<div class="banner banner-error">Bạn không được phân công trận này; mọi thao tác sẽ bị từ chối (BR09).</div>';
    else if (scheduled && !bothLineups) hint = '<div class="banner banner-warn">Chưa đủ đội hình: cả hai đội phải nộp đội hình trước khi bắt đầu trận (BR20).</div>';
    else if (scheduled) hint = '<div class="banner banner-info">Cả hai đội đã nộp đội hình (BR20). Bấm <b>Bắt đầu trận</b> để khóa đội hình và bắt đầu ghi diễn biến.</div>';
    else if (live) hint = '<div class="banner banner-info">Trận đang diễn ra. Đội hình xuất phát đã bị khóa; thay người chỉ lưu thêm, không sửa đội hình (BR20).</div>';
    else if (m.forfeitReason) hint = '<div class="banner banner-warn"><b>' + (m.forfeitTeamId ? 'Xử thua:' : 'Hủy trận:') + '</b> ' + App.esc(m.forfeitReason) + '</div>';
    else if (finished) hint ='<div class="banner banner-info">Trận đã kết thúc: <b>' + home.score + '–' + away.score + '</b>. Hệ thống đã tính lại tỉ số từ bàn thắng, giảm án treo giò cũ và sinh án mới cho thẻ đỏ / 3 thẻ vàng (BR11, BR14).</div>';
    else hint = '<div class="banner banner-warn">Trận đang ở trạng thái ' + App.esc(App.MATCH_STATUS[m.status]) + '.</div>';

    var hp = pitch(home), ap = pitch(away);

    root.innerHTML =
      '<div class="row-between" style="align-items:flex-end"><div><span class="eyebrow">Vòng ' + m.round + '</span><h1>Bảng điều khiển trận</h1>' +
      '<p class="muted">' + App.dayLongFull(m.kickoffTime) + ' · ' + App.hhmm(m.kickoffTime) + ' · ' + App.esc(m.venue || 'Chưa có sân') + '</p></div>' +
      '<a class="btn" href="/referee/matches">Danh sách trận</a></div>' +
      '<section class="console-board"><div class="console-team">' + App.avatar(home.id, home.name, 'md') + '<b>' + App.esc(home.name) + '</b></div>' +
      '<div class="console-score"><span class="display">' + home.score + '</span><i>–</i><span class="display">' + away.score + '</span>' +
      '<span class="status-pill" data-status="' + m.status + '">' + App.MATCH_STATUS[m.status] + '</span></div>' +
      '<div class="console-team">' + App.avatar(away.id, away.name, 'md') + '<b>' + App.esc(away.name) + '</b></div>' +
      '<div class="console-actions"><button class="btn btn-primary" id="btn-start"' + (scheduled && bothLineups ? '' : ' disabled') + '>Bắt đầu trận</button>' +
      '<button class="btn btn-dark" id="btn-finish"' + (live ? '' : ' disabled') + '>Kết thúc trận</button></div></section>' +
      hint + '<div class="banner banner-error" id="console-error" hidden></div>' +
      '<div class="split"><section class="panel panel-main"><h2>Ghi diễn biến</h2><div class="tabs" role="tablist">' +
      [['goal', 'Bàn thắng'], ['card', 'Thẻ phạt'], ['sub', 'Thay người']].map(function (t) {
        return '<button class="tab' + (tab === t[0] ? ' is-active' : '') + '" type="button" data-tab="' + t[0] + '">' + t[1] + '</button>';
      }).join('') + '</div>' +

      '<form class="event-form" data-form="goal"' + (tab === 'goal' ? '' : ' hidden') + '><fieldset class="fieldset-plain"' + (live ? '' : ' disabled') + '><div class="form-grid">' +
      '<div class="field"><label>Đội</label><select class="input" name="side"><option value="home">' + App.esc(home.name) + '</option><option value="away">' + App.esc(away.name) + '</option></select></div>' +
      '<div class="field"><label>Cầu thủ ghi bàn (đang trên sân)</label><select class="input" name="player" data-players="on"></select></div>' +
      '<div class="field"><label>Phút (1–60)</label><input class="input" name="minute" type="number" min="1" max="60" value="1"></div>' +
      '<label class="check"><input type="checkbox" name="own"> Phản lưới nhà</label></div>' +
      '<button class="btn btn-primary" type="submit">Ghi bàn thắng</button></fieldset></form>' +

      '<form class="event-form" data-form="card"' + (tab === 'card' ? '' : ' hidden') + '><fieldset class="fieldset-plain"' + (live ? '' : ' disabled') + '><div class="form-grid">' +
      '<div class="field"><label>Đội</label><select class="input" name="side"><option value="home">' + App.esc(home.name) + '</option><option value="away">' + App.esc(away.name) + '</option></select></div>' +
      '<div class="field"><label>Cầu thủ</label><select class="input" name="player" data-players="all"></select></div>' +
      '<div class="field"><label>Loại thẻ</label><select class="input" name="type"><option value="Yellow">Thẻ vàng</option><option value="Red">Thẻ đỏ</option></select></div>' +
      '<div class="field"><label>Phút (1–60)</label><input class="input" name="minute" type="number" min="1" max="60" value="1"></div></div>' +
      '<button class="btn btn-primary" type="submit">Ghi thẻ phạt</button></fieldset></form>' +

      '<form class="event-form" data-form="sub"' + (tab === 'sub' ? '' : ' hidden') + '><fieldset class="fieldset-plain"' + (live ? '' : ' disabled') + '><div class="form-grid">' +
      '<div class="field"><label>Đội</label><select class="input" name="side"><option value="home">' + App.esc(home.name) + '</option><option value="away">' + App.esc(away.name) + '</option></select></div>' +
      '<div class="field"><label>Cầu thủ ra (đang trên sân)</label><select class="input" name="out" data-players="on"></select></div>' +
      '<div class="field"><label>Cầu thủ vào (dự bị)</label><select class="input" name="in" data-players="bench"></select></div>' +
      '<div class="field"><label>Phút (1–60)</label><input class="input" name="minute" type="number" min="1" max="60" value="1"></div></div>' +
      '<button class="btn btn-primary" type="submit">Ghi thay người</button></fieldset></form></section>' +

      '<section class="panel panel-side"><h2>Diễn biến</h2><ol class="event-log">' +
      (m.events.length ? m.events.map(function (e) {
        return '<li><span class="minute">' + e.minute + '\'</span><span class="txt">' + App.esc(eventText(e)) + '</span>' +
          (live ? '<button class="btn btn-sm btn-danger" data-del="' + e.type + '" data-id="' + e.eventId + '" title="Xóa sự kiện">Xóa</button>' : '') + '</li>';
      }).join('') : '<li class="muted">Chưa có sự kiện nào.</li>') + '</ol></section></div>' +

      '<section class="panel"><h2>Cầu thủ đang trên sân</h2><div class="onfield">' + [[home, hp], [away, ap]].map(function (x) {
        return '<div class="onfield-team"><b>' + App.esc(x[0].name) + '</b>' + x[1].on.sort(function (a, b) { return a.shirtNumber - b.shirtNumber; }).map(function (p) {
          return '<div class="onfield-row"><span class="no">' + p.shirtNumber + '</span><span class="nm">' + App.esc(p.name) + '</span>' + App.render.positionBadge(p.position) + '</div>';
        }).join('') + (x[1].on.length ? '' : '<p class="muted">Chưa có đội hình.</p>') + '</div>';
      }).join('') + '</div></section>';

    fillPlayers();
  }

  function fillPlayers() {
    App.$$('.event-form').forEach(function (form) {
      function fill() {
        var side = form.elements.side.value;
        var team = side === 'home' ? m.homeTeam : m.awayTeam;
        var p = pitch(team);
        App.$$('[data-players]', form).forEach(function (sel) {
          var mode = sel.dataset.players;
          var list = mode === 'on' ? p.on : mode === 'bench' ? p.bench : p.on.concat(p.bench);
          sel.innerHTML = playerOptions(list);
        });
      }
      form.elements.side.addEventListener('change', fill);
      fill();
    });
  }

  function showError(message) {
    var el = App.$('#console-error');
    el.textContent = message || '';
    el.hidden = !message;
  }

  async function act(promise, okMessage) {
    var res = await promise;
    if (res.ok) { showError(''); App.toast(okMessage || res.message); await load(); render(); }
    else showError(res.message);
    return res.ok;
  }

  root.addEventListener('click', async function (e) {
    var t = e.target.closest('[data-tab]');
    if (t) { tab = t.dataset.tab; render(); return; }

    if (e.target.id === 'btn-start') await act(App.patch('/api/matches/' + matchId + '/status', { status: 'InProgress' }), 'Trận đã bắt đầu');
    else if (e.target.id === 'btn-finish') {
      if (await App.confirm('Kết thúc trận với tỉ số ' + m.homeTeam.score + '–' + m.awayTeam.score + '?'))
        await act(App.patch('/api/matches/' + matchId + '/status', { status: 'Finished' }), 'Đã kết thúc trận');
    } else if (e.target.dataset.del) {
      await act(App.del('/api/matches/' + matchId + '/' + DELETE_PATH[e.target.dataset.del] + '/' + e.target.dataset.id), 'Đã xóa sự kiện');
    }
  });

  root.addEventListener('submit', async function (e) {
    e.preventDefault();
    var form = e.target;
    var f = form.elements;
    var kind = form.dataset.form;
    var minute = +f.minute.value;
    var needed = kind === 'sub' ? [f.out, f.in] : [f.player];
    if (needed.some(function (s) { return !s.value; })) { showError('Chưa có cầu thủ phù hợp để chọn.'); return; }
    var path = '/api/matches/' + matchId;

    if (kind === 'goal')
      await act(App.post(path + '/goals', { playerId: +f.player.value, minute: minute, isOwnGoal: f.own.checked }), 'Đã ghi bàn thắng');
    else if (kind === 'card')
      await act(App.post(path + '/cards', { playerId: +f.player.value, cardType: f.type.value, minute: minute }));
    else if (kind === 'sub') {
      var teamId = f.side.value === 'home' ? m.homeTeam.id : m.awayTeam.id;
      await act(App.post(path + '/substitutions', { teamId: teamId, playerOutId: +f.out.value, playerInId: +f.in.value, minute: minute }), 'Đã ghi thay người');
    }
  });

  if (await load()) render();
};
