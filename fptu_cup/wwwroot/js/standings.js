/* Bảng xếp hạng đầy đủ, vua phá lưới (top 10) và cầu thủ bị treo giò. */
App.main = async function () {
  var $ = App.$;
  var t = (await App.loadTournaments()).current;
  if (!t) { $('#standings-sub').textContent = 'Chưa có giải đấu nào.'; return; }

  var res = await Promise.all([
    App.get('/api/tournaments/' + t.id + '/standings'),
    App.get('/api/tournaments/' + t.id + '/top-scorers?top=10'),
    App.get('/api/tournaments/' + t.id + '/suspensions'),
  ]);
  $('#standings-sub').textContent = t.name + ' · tính từ các trận đã kết thúc, đội đứng đầu khi hết giải là nhà vô địch.';
  $('#standings').innerHTML = App.render.standings(res[0].ok ? res[0].data : []);
  $('#scorers').innerHTML = App.render.topScorers(res[1].ok ? res[1].data : []);
  $('#suspensions').innerHTML = App.render.suspensions(res[2].ok ? res[2].data : []);
};
