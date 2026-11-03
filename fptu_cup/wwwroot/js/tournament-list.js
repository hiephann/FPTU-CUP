/* Danh sách giải đấu công khai (FR07): lọc, phân trang, chọn giải để xem. */
App.main = async function () {
  var $ = App.$;
  var state = { page: 1, search: '', status: '' };
  var BADGE = { Draft: 'neutral', Registration: 'warn', Ongoing: 'ok', Finished: 'neutral' };
  var selected = (await App.loadTournaments()).current;

  async function load() {
    var res = await App.get('/api/tournaments' + App.qs({ page: state.page, pageSize: 6, search: state.search, status: state.status }));
    if (!res.ok) { $('#list').innerHTML = '<section class="card"><p class="muted">' + App.esc(res.message) + '</p></section>'; return; }
    var d = res.data;
    $('#count').textContent = d.totalCount + ' giải';
    $('#list').innerHTML = d.items.length ? d.items.map(function (t) {
      var current = selected && selected.id === t.id;
      return '<section class="card"><div class="card-head"><h2 class="card-title">' + App.esc(t.name) + '</h2>' +
        App.render.statusBadge(BADGE[t.status], App.TOURNAMENT_STATUS[t.status]) + '</div>' +
        '<p class="muted">' + App.esc(t.season) + ' · ' + App.dmy(t.startDate) + ' – ' + App.dmy(t.endDate) + ' · ' +
        t.approvedTeams + '/' + t.maxTeams + ' đội · vòng tròn ' + t.numberOfLegs + ' lượt</p>' +
        '<div>' + (current ? '<span class="pill pill-green">Đang xem</span>' : '<button class="btn btn-sm btn-primary" data-pick="' + t.id + '">Xem giải này</button>') + '</div></section>';
    }).join('') : '<section class="card"><p class="muted">Không có giải đấu nào khớp.</p></section>';
    $('#pager').innerHTML = App.render.pager(d);
  }

  $('#filter').addEventListener('submit', function (e) {
    e.preventDefault();
    state.search = e.target.elements.search.value.trim();
    state.status = e.target.elements.status.value;
    state.page = 1;
    load();
  });
  $('#pager').addEventListener('click', function (e) {
    var b = e.target.closest('[data-page]');
    if (b && !b.disabled) { state.page = +b.dataset.page; load(); }
  });
  $('#list').addEventListener('click', function (e) {
    var b = e.target.closest('[data-pick]');
    if (b) { App.setTournament(b.dataset.pick); location.href = '/'; }
  });

  load();
};
