/*
 * Tìm kiếm cầu thủ (FR13) bằng OData: GET /odata/Players?$filter=...&$orderby=...&$top=&$skip=&$count=true
 * Lọc theo giải, tên / MSSV / tên đội (contains) và vị trí; sắp xếp theo tên, số áo hoặc đội.
 */
App.main = async function () {
  var $ = App.$;
  var PAGE_SIZE = 12;
  var t = (await App.loadTournaments()).current;
  var state = { page: 1, search: new URLSearchParams(location.search).get('search') || '', position: '', orderBy: 'FullName' };
  $('#search').value = state.search;
  if (t) $('#players-sub').textContent = 'Tìm theo tên, MSSV hoặc đội · ' + t.name;

  /** Chuỗi trong OData đặt trong nháy đơn, nháy đơn bên trong nhân đôi. */
  function lit(s) { return "'" + String(s).replace(/'/g, "''") + "'"; }

  function buildQuery() {
    var filters = [];
    if (t) filters.push('TournamentId eq ' + t.id);
    if (state.search) {
      var s = lit(state.search);
      filters.push('(contains(FullName,' + s + ') or contains(StudentCode,' + s + ') or contains(TeamName,' + s + '))');
    }
    if (state.position) filters.push('Position eq ' + lit(state.position));
    var q = {
      $select: 'Id,TeamId,TeamName,StudentCode,FullName,ShirtNumber,Position',
      $orderby: state.orderBy + (state.orderBy === 'FullName' ? '' : ',FullName'),
      $top: PAGE_SIZE,
      $skip: (state.page - 1) * PAGE_SIZE,
      $count: 'true',
    };
    if (filters.length) q.$filter = filters.join(' and ');
    return '/odata/Players' + App.qs(q);
  }

  async function load() {
    var url = buildQuery();
    $('#odata-url').textContent = decodeURIComponent(url);
    var res = await fetch(url, { headers: { Accept: 'application/json' } })
      .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, json: j }; }); })
      .catch(function () { return { ok: false, json: null }; });
    if (!res.ok) {
      var msg = res.json && res.json.error ? res.json.error.message : 'Không kết nối được máy chủ';
      $('#player-table').innerHTML = '<p class="muted">' + App.esc(msg) + '</p>';
      return;
    }
    var items = res.json.value;
    var total = res.json['@odata.count'];
    $('#count').textContent = total + ' cầu thủ';
    $('#player-table').innerHTML = items.length
      ? '<div class="table-scroll"><table class="tbl tbl-left"><thead><tr><th>Số áo</th><th>Họ và tên</th><th>Vị trí</th><th>MSSV</th><th>Đội</th><th></th></tr></thead><tbody>' +
        items.map(function (p) {
          return '<tr><td class="time">' + p.ShirtNumber + '</td><td style="font-weight:600">' + App.esc(p.FullName) + '</td><td>' + App.render.positionBadge(p.Position) +
            '</td><td>' + App.esc(p.StudentCode) + '</td><td><a href="/teams/' + p.TeamId + '">' + App.esc(p.TeamName) + '</a></td>' +
            '<td class="al-right"><a href="/players/' + p.Id + '">Thống kê</a></td></tr>';
        }).join('') + '</tbody></table></div>'
      : '<p class="muted">Không tìm thấy cầu thủ nào.</p>';
    $('#pager').innerHTML = App.render.pager({ page: state.page, totalPages: Math.max(1, Math.ceil(total / PAGE_SIZE)) });
  }

  $('#filter').addEventListener('submit', function (e) {
    e.preventDefault();
    state.search = e.target.elements.search.value.trim();
    state.position = e.target.elements.position.value;
    state.orderBy = e.target.elements.orderBy.value;
    state.page = 1;
    load();
  });
  $('#pager').addEventListener('click', function (e) {
    var b = e.target.closest('[data-page]');
    if (b && !b.disabled) { state.page = +b.dataset.page; load(); }
  });

  load();
};
