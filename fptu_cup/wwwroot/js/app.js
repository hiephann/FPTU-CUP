/*
 * Lõi dùng chung cho giao diện: phiên đăng nhập (JWT trong localStorage), gọi API, thông báo,
 * hộp thoại nhập liệu và các hàm định dạng. Các trang chỉ là khung HTML, dữ liệu lấy từ /api/...
 */
(function () {
  var TOKEN_KEY = 'fptu_token';
  var USER_KEY = 'fptu_user';
  var TID_KEY = 'fptu_tid';
  var App = (window.App = {});

  // ------------------------------------------------------------ Tiện ích

  App.esc = function (s) {
    return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
    });
  };

  App.qs = function (params) {
    var parts = [];
    Object.keys(params).forEach(function (k) {
      var v = params[k];
      if (v !== null && v !== undefined && v !== '') parts.push(encodeURIComponent(k) + '=' + encodeURIComponent(v));
    });
    return parts.length ? '?' + parts.join('&') : '';
  };

  App.$ = function (sel, root) { return (root || document).querySelector(sel); };
  App.$$ = function (sel, root) { return [].slice.call((root || document).querySelectorAll(sel)); };

  // ------------------------------------------------------------ Phiên đăng nhập

  App.token = function () { return localStorage.getItem(TOKEN_KEY); };

  App.user = function () {
    try { return JSON.parse(localStorage.getItem(USER_KEY) || 'null'); } catch (e) { return null; }
  };

  App.setSession = function (token, user) {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  };

  App.clearSession = function () {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  };

  App.logout = function () {
    App.clearSession();
    location.href = '/login';
  };

  App.homeFor = function (role) {
    return role === 'Admin' ? '/admin' : role === 'TeamManager' ? '/manager/team' : role === 'Referee' ? '/referee/matches' : '/';
  };

  App.requireRole = function (role) {
    var u = App.user();
    if (!App.token() || !u || u.role !== role) {
      location.href = '/login?next=' + encodeURIComponent(location.pathname + location.search);
      return false;
    }
    return true;
  };

  // ------------------------------------------------------------ Gọi API

  App.api = function (method, url, body) {
    var headers = { 'Content-Type': 'application/json' };
    if (App.token()) headers.Authorization = 'Bearer ' + App.token();
    return fetch(url, { method: method, headers: headers, body: body === undefined ? undefined : JSON.stringify(body) })
      .then(function (res) {
        return res.json().catch(function () { return null; }).then(function (json) {
          if (res.status === 401 && App.token()) {
            App.clearSession();
            location.href = '/login?next=' + encodeURIComponent(location.pathname + location.search);
          }
          var message = (json && json.message) || res.statusText || 'Có lỗi xảy ra';
          if (res.status === 400 && json && json.data && typeof json.data === 'object') {
            var details = [].concat.apply([], Object.keys(json.data).map(function (k) { return json.data[k]; }));
            if (details.length) message += ': ' + details.join('; ');
          }
          return { ok: res.ok, status: res.status, data: json ? json.data : null, message: message };
        });
      })
      .catch(function () { return { ok: false, status: 0, data: null, message: 'Không kết nối được máy chủ' }; });
  };
  App.get = function (url) { return App.api('GET', url); };
  App.post = function (url, body) { return App.api('POST', url, body === undefined ? {} : body); };
  App.put = function (url, body) { return App.api('PUT', url, body); };
  App.patch = function (url, body) { return App.api('PATCH', url, body); };
  App.del = function (url) { return App.api('DELETE', url); };

  /** Đọc hết các trang của một API danh sách (pageSize tối đa 50). */
  App.getAll = function (url) {
    var items = [];
    function next(page) {
      var sep = url.indexOf('?') >= 0 ? '&' : '?';
      return App.get(url + sep + 'page=' + page + '&pageSize=50').then(function (res) {
        if (!res.ok) return res;
        items = items.concat(res.data.items);
        return page < res.data.totalPages ? next(page + 1) : { ok: true, data: items };
      });
    }
    return next(1);
  };

  /** Hiện toast cho kết quả một lệnh ghi; trả về true nếu thành công. */
  App.report = function (res, okMessage) {
    App.toast(res.ok ? (okMessage || res.message) : res.message, res.ok ? '' : 'error');
    return res.ok;
  };

  // ------------------------------------------------------------ Thông báo & hộp thoại

  var toastTimer;
  App.toast = function (message, kind) {
    var el = document.getElementById('toast');
    if (!el) return;
    el.textContent = message;
    el.className = 'toast' + (kind === 'error' ? ' toast-error' : '');
    el.hidden = false;
    clearTimeout(toastTimer);
    toastTimer = setTimeout(function () { el.hidden = true; }, kind === 'error' ? 5200 : 3000);
  };

  /**
   * Hộp thoại nhập liệu. fields: [{ name, label, type, value, options: [{value,label}], min, max, required, hint }].
   * Trả về Promise<object|null> (null nếu hủy).
   */
  App.formDialog = function (opts) {
    return new Promise(function (resolve) {
      var dlg = document.createElement('dialog');
      dlg.className = 'dialog';
      var html = '<form method="dialog" class="dialog-form"><h2>' + App.esc(opts.title) + '</h2>';
      (opts.fields || []).forEach(function (f) {
        var v = f.value == null ? '' : f.value;
        html += '<div class="field"><label for="df-' + f.name + '">' + App.esc(f.label) + '</label>';
        if (f.type === 'select') {
          html += '<select class="input" id="df-' + f.name + '" name="' + f.name + '">';
          (f.options || []).forEach(function (o) {
            html += '<option value="' + App.esc(o.value) + '"' + (String(o.value) === String(v) ? ' selected' : '') + '>' + App.esc(o.label) + '</option>';
          });
          html += '</select>';
        } else {
          html += '<input class="input" id="df-' + f.name + '" name="' + f.name + '" type="' + (f.type || 'text') + '" value="' + App.esc(v) + '"' +
            (f.min != null ? ' min="' + f.min + '"' : '') + (f.max != null ? ' max="' + f.max + '"' : '') +
            (f.required ? ' required' : '') + '>';
        }
        if (f.hint) html += '<span class="hint">' + App.esc(f.hint) + '</span>';
        html += '</div>';
      });
      html += '<div class="dialog-actions"><button class="btn" type="button" data-cancel>Hủy</button>' +
        '<button class="btn btn-primary" type="submit">' + App.esc(opts.submitText || 'Lưu') + '</button></div></form>';
      dlg.innerHTML = html;
      document.body.appendChild(dlg);

      var done = false;
      function close(result) {
        if (done) return;
        done = true;
        dlg.close();
        dlg.remove();
        resolve(result);
      }
      dlg.querySelector('[data-cancel]').addEventListener('click', function () { close(null); });
      dlg.addEventListener('cancel', function (e) { e.preventDefault(); close(null); });
      dlg.querySelector('form').addEventListener('submit', function (e) {
        e.preventDefault();
        var result = {};
        (opts.fields || []).forEach(function (f) { result[f.name] = dlg.querySelector('[name="' + f.name + '"]').value; });
        close(result);
      });
      dlg.showModal();
    });
  };

  App.confirm = function (message) {
    return App.formDialog({ title: message, fields: [], submitText: 'Đồng ý' }).then(function (r) { return r !== null; });
  };

  // ------------------------------------------------------------ Định dạng

  var DAYS = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
  var DAYS_LONG = ['Chủ nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'];
  function p2(n) { return (n < 10 ? '0' : '') + n; }

  App.d = function (s) { return new Date(s); };
  App.hhmm = function (s) { var d = new Date(s); return p2(d.getHours()) + ':' + p2(d.getMinutes()); };
  App.dm = function (s) { var d = new Date(s); return p2(d.getDate()) + '/' + p2(d.getMonth() + 1); };
  App.dmy = function (s) { var d = new Date(s); return p2(d.getDate()) + '/' + p2(d.getMonth() + 1) + '/' + d.getFullYear(); };
  App.day = function (s) { return DAYS[new Date(s).getDay()] + ' ' + App.dm(s); };
  App.dayLong = function (s) { return DAYS_LONG[new Date(s).getDay()] + ' ' + App.dm(s); };
  App.dayLongFull = function (s) { return DAYS_LONG[new Date(s).getDay()] + ' ' + App.dmy(s); };
  App.hhmmDmy = function (s) { return App.hhmm(s) + ' ' + App.dmy(s); };
  /** Giờ Việt Nam hiện tại, dưới dạng Date mà các getter local trả đúng giờ treo tường VN (so sánh được với KickoffTime). */
  App.vnNow = function () {
    var now = new Date();
    return new Date(now.getTime() + now.getTimezoneOffset() * 60000 + 7 * 3600000);
  };
  /** Trạng thái hiển thị của trận: xử thua (BR23) được tách riêng khỏi Finished/Cancelled. */
  App.matchStatusText = function (m) {
    if (m.forfeitTeamId) return 'Xử thua';
    if (m.status === 'Cancelled' && m.forfeitReason) return 'Hủy (quá hạn)';
    return App.MATCH_STATUS[m.status];
  };
  /** Giá trị cho input datetime-local. */
  App.inputDateTime = function (s) {
    var d = new Date(s);
    return d.getFullYear() + '-' + p2(d.getMonth() + 1) + '-' + p2(d.getDate()) + 'T' + p2(d.getHours()) + ':' + p2(d.getMinutes());
  };

  /** Mã viết tắt trong vòng tròn của đội: chữ cái đầu của các từ (bỏ "FC"), tối đa 3 ký tự. */
  App.code = function (name) {
    var words = String(name || '').replace(/\bFC\b/i, '').trim().split(/\s+/).filter(Boolean);
    if (words.length === 0) return '?';
    if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
    return words.slice(0, 3).map(function (w) { return w[0]; }).join('').toUpperCase();
  };
  App.tone = function (id) { return ['green', 'orange', 'blue', 'neutral'][Math.abs(+id || 0) % 4]; };
  App.avatar = function (id, name, size) {
    return '<span class="avatar avatar-' + (size || 'sm') + ' tone-' + App.tone(id) + '">' + App.esc(App.code(name)) + '</span>';
  };
  App.initials = function (name) {
    var w = String(name || '?').trim().split(/\s+/);
    return ((w[0] || '')[0] + (w.length > 1 ? w[w.length - 1][0] : '')).toUpperCase();
  };

  App.TOURNAMENT_STATUS = { Draft: 'Nháp', Registration: 'Đăng ký', Ongoing: 'Diễn ra', Finished: 'Kết thúc' };
  App.TEAM_STATUS = { Pending: 'Chờ duyệt', Approved: 'Đã duyệt', Rejected: 'Từ chối' };
  App.MATCH_STATUS = { Scheduled: 'Sắp diễn ra', InProgress: 'Đang diễn ra', Finished: 'Kết thúc', Postponed: 'Hoãn', Cancelled: 'Hủy' };

  // ------------------------------------------------------------ Giải đang xem

  App.pickTournament = function (list) {
    var id = +localStorage.getItem(TID_KEY);
    return list.filter(function (t) { return t.id === id; })[0] ||
      list.filter(function (t) { return t.status === 'Ongoing'; })[0] ||
      list.filter(function (t) { return t.status === 'Registration'; })[0] || list[0];
  };
  App.setTournament = function (id) { localStorage.setItem(TID_KEY, id); };

  /** Danh sách giải và giải đang chọn: Promise<{ list, current }>. */
  App.loadTournaments = function () {
    return App.get('/api/tournaments?pageSize=50').then(function (res) {
      var list = res.ok ? res.data.items : [];
      return { list: list, current: list.length ? App.pickTournament(list) : null };
    });
  };

  // ------------------------------------------------------------ Khởi tạo layout

  /** Layout công khai: nút Đăng nhập đổi thành tên người dùng + Đăng xuất. */
  function initPublicNav() {
    var link = document.getElementById('auth-link');
    var u = App.user();
    if (!link || !App.token() || !u) return;
    link.outerHTML = '<span class="nav-user"><a href="' + App.homeFor(u.role) + '">' + App.esc(u.fullName) + '</a>' +
      '<button class="btn btn-header" type="button" id="logout-btn">Đăng xuất</button></span>';
    document.getElementById('logout-btn').addEventListener('click', App.logout);
  }

  /** Layout quản lý: kiểm tra vai trò, hiện thông tin user, chọn giải (Admin). */
  function initPortal() {
    var role = document.body.dataset.portal;
    if (!App.requireRole(role)) return Promise.resolve(false);

    var u = App.user();
    App.$('#me-name').textContent = u.fullName;
    App.$('#me-role').textContent = u.role;
    App.$('#me-avatar').textContent = App.initials(u.fullName);
    App.$('#logout-link').addEventListener('click', function (e) { e.preventDefault(); App.logout(); });

    var select = document.getElementById('tournament-select');
    var picker = document.getElementById('tournament-picker');
    if (role !== 'Admin') {
      if (picker) picker.hidden = true;
      return Promise.resolve(true);
    }
    return App.loadTournaments().then(function (t) {
      App.tournament = t.current;
      App.tournaments = t.list;
      select.innerHTML = t.list.map(function (x) {
        return '<option value="' + x.id + '"' + (t.current && x.id === t.current.id ? ' selected' : '') + '>' +
          App.esc(x.name) + ' · ' + App.TOURNAMENT_STATUS[x.status] + '</option>';
      }).join('');
      select.addEventListener('change', function () {
        App.setTournament(select.value);
        location.reload();
      });
      return true;
    });
  }

  App.ready = Promise.resolve(true);
  document.addEventListener('DOMContentLoaded', function () {
    if (document.body.dataset.portal) {
      App.ready = initPortal();
    } else {
      initPublicNav();
    }
    App.ready.then(function (ok) {
      if (ok && typeof App.main === 'function') App.main();
    });
  });
})();
