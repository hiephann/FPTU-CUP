/* Danh sách đội của giải đang xem (FR11): GET /api/tournaments/{id}/teams. */
App.main = async function () {
  var $ = App.$;
  var t = (await App.loadTournaments()).current;
  if (!t) { $('#teams-sub').textContent = 'Chưa có giải đấu nào.'; return; }
  $('#teams-sub').innerHTML = App.esc(t.name) + ' · <a href="/tournaments">đổi giải</a>';
  var state = { page: 1, search: '' };

  async function load() {
    var res = await App.get('/api/tournaments/' + t.id + '/teams' + App.qs({ page: state.page, pageSize: 12, search: state.search }));
    if (!res.ok) { $('#teams').innerHTML = '<p class="muted">' + App.esc(res.message) + '</p>'; return; }
    var d = res.data;
    $('#count').textContent = d.totalCount + ' đội';
    $('#teams').innerHTML = d.items.length ? d.items.map(function (x) {
      return '<a class="team-card" href="/teams/' + x.id + '">' + App.avatar(x.id, x.name, 'md') + App.teamCode(x.name) + '<b>' + App.esc(x.name) + '</b>' +
        '<small>' + App.esc(x.faculty || '—') + '</small><small>' + x.playerCount + ' cầu thủ · ' + App.esc(x.managerName) + '</small></a>';
    }).join('') : '<p class="muted">Không có đội nào khớp.</p>';
    $('#pager').innerHTML = App.render.pager(d);
  }

  $('#filter').addEventListener('submit', function (e) {
    e.preventDefault();
    state.search = e.target.elements.search.value.trim();
    state.page = 1;
    load();
  });
  $('#pager').addEventListener('click', function (e) {
    var b = e.target.closest('[data-page]');
    if (b && !b.disabled) { state.page = +b.dataset.page; load(); }
  });

  load();
};
