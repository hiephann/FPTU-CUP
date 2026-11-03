/* Tìm kiếm cầu thủ (FR13): GET /api/players?search=&tournamentId=. */
App.main = async function () {
  var $ = App.$;
  var t = (await App.loadTournaments()).current;
  var state = { page: 1, search: new URLSearchParams(location.search).get('search') || '' };
  $('#search').value = state.search;
  if (t) $('#players-sub').textContent = 'Tìm theo tên, MSSV hoặc đội · ' + t.name;

  async function load() {
    var res = await App.get('/api/players' + App.qs({ page: state.page, pageSize: 12, search: state.search, tournamentId: t ? t.id : '' }));
    if (!res.ok) { $('#player-table').innerHTML = '<p class="muted">' + App.esc(res.message) + '</p>'; return; }
    var d = res.data;
    $('#count').textContent = d.totalCount + ' cầu thủ';
    $('#player-table').innerHTML = d.items.length
      ? '<div class="table-scroll"><table class="tbl tbl-left"><thead><tr><th>Số áo</th><th>Họ và tên</th><th>Vị trí</th><th>MSSV</th><th>Đội</th><th></th></tr></thead><tbody>' +
        d.items.map(function (p) {
          return '<tr><td class="time">' + p.shirtNumber + '</td><td style="font-weight:600">' + App.esc(p.fullName) + '</td><td>' + App.render.positionBadge(p.position) +
            '</td><td>' + App.esc(p.studentCode) + '</td><td><a href="/teams/' + p.teamId + '">' + App.esc(p.team) + '</a></td>' +
            '<td class="al-right"><a href="/players/' + p.id + '">Thống kê</a></td></tr>';
        }).join('') + '</tbody></table></div>'
      : '<p class="muted">Không tìm thấy cầu thủ nào.</p>';
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
