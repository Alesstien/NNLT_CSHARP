(function () {
  'use strict';
  var D = window.AURA;
  var $ = function (s) { return document.querySelector(s); };
  var esc = function (s) {
    return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
    });
  };
  var money = function (n) { return Number(n).toLocaleString('vi-VN') + ' ₫'; };
  var icon = function (id) { return '<svg class="ic"><use href="#' + id + '"/></svg>'; };

  var CATS = [['all', 'Tất cả món'], ['Khai vị', 'Món Khai Vị'], ['Món chính', 'Món Chính Cao Cấp'],
              ['Rượu vang & đồ uống', 'Rượu Vang & Đồ Uống'], ['Tráng miệng', 'Tráng Miệng']];
  var MODES = [['CocCoDinh', 'CỌC CỐ ĐỊNH GIỮ BÀN', 'Giữ bàn tiêu chuẩn, thanh toán số còn lại khi dùng bữa.'],
               ['Coc30', 'ĐẶT CỌC 30% HÓA ĐƠN', 'Phù hợp khi có chọn trước các món ăn cao cấp.'],
               ['Tra100', 'THANH TOÁN 100% TRỌN GÓI', 'Tiện lợi, không cần thanh toán thêm tại nhà hàng.']];
  var METHODS = [['VIETQR', 'Chuyển Khoản VietQR', 'Quét mã QR tự động nhận diện'],
                 ['WALLET', 'Ví Điện Tử MoMo / VNPay', 'Thanh toán ví điện tử nhanh'],
                 ['CARD', 'Thẻ VISA / Mastercard', 'Thanh toán thẻ quốc tế']];
  var DRAFT_KEY = 'aura_draft_v2';

  var S = { step: 1, guests: 2, areaId: D.areas.length ? D.areas[0].id : 0, cart: {}, cat: 'all', avail: {},
            hold: null, promo: '', mode: 'CocCoDinh', method: 'VIETQR', wallet: 'MOMO',
            bank: D.banks.length ? D.banks[0].code : '', booking: null };

  function ymd(d) {
    var m = d.getMonth() + 1, day = d.getDate();
    return d.getFullYear() + '-' + (m < 10 ? '0' : '') + m + '-' + (day < 10 ? '0' : '') + day;
  }
  function toast(msg, ok) {
    var t = $('#toast'); t.textContent = msg; t.className = 'toast show' + (ok ? ' ok' : '');
    clearTimeout(toast._t); toast._t = setTimeout(function () { t.className = 'toast'; }, 4500);
  }
  function areaById(id) { return D.areas.filter(function (a) { return a.id === id; })[0]; }
  function monById(id) { return D.mons.filter(function (m) { return m.id === id; })[0]; }
  function tongMon() {
    return Object.keys(S.cart).reduce(function (s, id) { var m = monById(+id); return s + (m ? m.gia * S.cart[id] : 0); }, 0);
  }
  function modeAmount(mode) {
    var t = S.hold ? S.hold.tongHoaDon : 0;
    if (mode === 'Coc30') return Math.round(t * D.rules.ty30);
    if (mode === 'Tra100') return t;
    return D.rules.cocCoDinh;
  }
  function conLaiOf(mode) { return Math.max(0, (S.hold ? S.hold.tongHoaDon : 0) - modeAmount(mode)); }
  function methodCode() { return S.method === 'WALLET' ? S.wallet : S.method === 'CARD' ? 'THE' : 'VIETQR'; }

  /* ---------- Gọi API ---------- */
  function postJson(url, body) {
    return fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').value },
      body: JSON.stringify(body)
    }).then(function (r) {
      if (r.status === 401) return { status: 401 };
      return r.json().catch(function () { return {}; }).then(function (j) { return { status: r.status, ok: r.ok, j: j }; });
    });
  }
  function errMsg(j) {
    if (j.loi) return j.loi;
    if (j.errors) { var k = Object.keys(j.errors)[0]; if (k) return j.errors[k][0]; }
    return 'Không thể thực hiện, vui lòng thử lại.';
  }
  function toLogin() {
    saveDraft();
    toast('Vui lòng đăng nhập để tiếp tục đặt cọc…', true);
    setTimeout(function () { location.href = '/Account/Login?returnUrl=' + encodeURIComponent('/DatBan'); }, 900);
  }

  /* ---------- Bước 1: ngày giờ ---------- */
  function buildSlots() {
    var sel = $('#f-gio'), prev = sel.value, ngay = $('#f-ngay').value;
    var limit = new Date(Date.now() + 3600 * 1000);
    sel.innerHTML = D.slots.map(function (s) {
      var past = ngay && new Date(ngay + 'T' + s.value + ':00') < limit;
      return '<option value="' + s.value + '"' + (past ? ' disabled' : '') + '>' + esc(s.label) + '</option>';
    }).join('');
    var ok = Array.prototype.filter.call(sel.options, function (o) { return !o.disabled; });
    if (!ok.length) return false;
    var keep = ok.filter(function (o) { return o.value === (prev || '19:00'); })[0];
    sel.value = (keep || ok[0]).value;
    return true;
  }

  /* ---------- Bước 1: khu vực ---------- */
  function renderAreas() {
    $('#areas').innerHTML = D.areas.map(function (a) {
      var av = S.avail[a.id], off = av && !av.duCho;
      var price = a.phuThu > 0 ? '+' + money(a.phuThu) : 'Miễn phí';
      if (off) price = av.conLai === 0 ? 'Hết chỗ' : 'Còn ' + av.conLai + ' chỗ';
      return '<button type="button" class="area' + (a.id === S.areaId ? ' sel' : '') + (off ? ' off' : '') + '" data-id="' + a.id + '">' +
        '<div class="area-head">' + icon('i-pin') + '<span>' + esc(a.ten) + '</span><span class="price' + (a.phuThu > 0 ? '' : ' free') + '">' + esc(price) + '</span></div>' +
        '<p>' + esc(a.moTa) + '</p></button>';
    }).join('');
  }
  var availTimer;
  function scheduleAvail() { clearTimeout(availTimer); availTimer = setTimeout(loadAvail, 250); }
  function loadAvail() {
    var ngay = $('#f-ngay').value, gio = $('#f-gio').value;
    if (!ngay || !gio) return Promise.resolve();
    return fetch('/api/dat-ban/kha-dung?ngay=' + ngay + '&gio=' + encodeURIComponent(gio) + '&khach=' + S.guests)
      .then(function (r) { return r.ok ? r.json() : []; })
      .then(function (arr) {
        S.avail = {};
        arr.forEach(function (x) { S.avail[x.khuVucId] = x; });
        var cur = S.avail[S.areaId];
        if (cur && !cur.duCho) {
          var alt = D.areas.filter(function (a) { return S.avail[a.id] && S.avail[a.id].duCho; })[0];
          if (alt) { S.areaId = alt.id; toast('Khu vực bạn chọn đã hết chỗ, hệ thống chuyển sang "' + alt.ten + '".'); }
        }
        renderAreas();
      }).catch(function () { renderAreas(); });
  }

  /* ---------- Bước 1: thực đơn ---------- */
  function renderCats() {
    $('#cats').innerHTML = CATS.map(function (c) {
      return '<button type="button" class="chip' + (S.cat === c[0] ? ' on' : '') + '" data-cat="' + esc(c[0]) + '">' + esc(c[1]) + '</button>';
    }).join('');
  }
  function renderMenu() {
    var list = D.mons.filter(function (m) { return S.cat === 'all' || m.danhMuc === S.cat; });
    $('#menu').innerHTML = list.map(function (m) {
      var q = S.cart[m.id] || 0;
      var img = m.hinhAnh ? '<img src="' + esc(m.hinhAnh) + '" alt="" loading="lazy">' : '<div class="tile">' + esc(m.emoji) + '</div>';
      return '<div class="dish"><div class="dish-img">' + img + '</div><div class="dish-body">' +
        '<div class="dish-name" title="' + esc(m.ten) + '">' + esc(m.ten) + '</div><div class="dish-desc" title="' + esc(m.moTa) + '">' + esc(m.moTa) + '</div>' +
        '<div class="dish-price">' + money(m.gia) + '</div></div>' +
        (q ? '<div class="qstep"><button type="button" data-dec="' + m.id + '" aria-label="Giảm">' + icon('i-minus') + '</button><b>' + q + '</b><button type="button" data-inc="' + m.id + '" aria-label="Tăng">' + icon('i-plus') + '</button></div>'
           : '<button type="button" class="btn-add" data-add="' + m.id + '">' + icon('i-plus') + ' Thêm</button>') + '</div>';
    }).join('');
    Array.prototype.forEach.call($('#menu').querySelectorAll('img'), function (im) {
      im.onerror = function () {
        var m = D.mons.filter(function (x) { return x.hinhAnh === im.getAttribute('src'); })[0];
        im.parentNode.innerHTML = '<div class="tile">' + esc(m ? m.emoji : '🍽️') + '</div>';
      };
    });
  }
  function renderCart() {
    var ids = Object.keys(S.cart), box = $('#cartBox');
    if (!ids.length) { box.innerHTML = ''; box.hidden = true; return; }
    box.hidden = false;
    var n = ids.reduce(function (s, i) { return s + S.cart[i]; }, 0);
    var rows = ids.map(function (id) {
      var m = monById(+id), q = S.cart[id];
      return '<div class="cart-item"><div class="ci-l"><div class="ci-name">' + esc(m.ten) + '</div><div class="ci-sub">' + money(m.gia) + ' x ' + q + '</div></div>' +
        '<div class="ci-r"><b>' + money(m.gia * q) + '</b><button type="button" class="ci-del" data-del="' + id + '" aria-label="Xóa món">' + icon('i-trash') + '</button></div></div>';
    }).join('');
    box.innerHTML = '<div class="cart"><div class="cart-head"><span class="cart-title">' + icon('i-bag') + ' Món ăn đã chọn trong giỏ (' + n + ')</span>' +
      '<span class="cart-hint">Tự động cộng vào hóa đơn cọc</span></div>' + rows +
      '<div class="cart-total"><span>Tổng tiền món ăn pre-order:</span><span>' + money(tongMon()) + '</span></div></div>';
  }
  var CART_KEY = 'aura_cart_v1';
  function saveCart() { try { localStorage.setItem(CART_KEY, JSON.stringify(S.cart)); } catch (e) { } }
  function loadCart() {
    try {
      var c = JSON.parse(localStorage.getItem(CART_KEY) || '{}'), ok = {};
      Object.keys(c).forEach(function (id) { if (monById(+id) && c[id] > 0) ok[id] = Math.min(c[id], 50); });
      S.cart = ok;
    } catch (e) { S.cart = {}; }
  }
  function changeQty(id, d) {
    var q = (S.cart[id] || 0) + d;
    if (q <= 0) delete S.cart[id]; else S.cart[id] = Math.min(q, 50);
    saveCart(); renderMenu(); renderCart();
  }

  /* ---------- Điều hướng bước ---------- */
  function go(n) {
    S.step = n;
    [1, 2, 3].forEach(function (i) { $('#p' + i).hidden = i !== n; });
    Array.prototype.forEach.call(document.querySelectorAll('.tab'), function (t) {
      var i = +t.dataset.step;
      t.classList.toggle('active', i === n);
      t.classList.toggle('done', i < n);
      t.disabled = S.booking ? i !== 3 : (i === 3 || (i === 2 && !S.hold));
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
  function dataForm() {
    return { hoTen: $('#f-ten').value.trim(), soDienThoai: $('#f-sdt').value.trim(), email: $('#f-email').value.trim(),
             ngay: $('#f-ngay').value, gio: $('#f-gio').value, dip: $('#f-dip').value, ghiChu: $('#f-note').value.trim() };
  }

  /* ---------- Giữ chỗ (bước 1 -> 2) ---------- */
  function holdBody(extra) {
    var f = dataForm();
    var b = { id: S.hold ? S.hold.id : null, hoTen: f.hoTen, soDienThoai: f.soDienThoai, email: f.email, ngay: f.ngay, gio: f.gio,
              soKhach: S.guests, khuVucId: S.areaId, dipDacBiet: f.dip, ghiChu: f.ghiChu, maUuDai: S.promo, cheDoCoc: S.mode,
              mons: Object.keys(S.cart).map(function (id) { return { mucThucDonId: +id, soLuong: S.cart[id] }; }) };
    if (extra) Object.keys(extra).forEach(function (k) { b[k] = extra[k]; });
    return b;
  }
  // Trả về Promise<đối tượng giữ chỗ | null>
  function hold(extra) {
    return postJson('/api/dat-ban/giu-cho', holdBody(extra)).then(function (res) {
      if (res.status === 401) { toLogin(); return null; }
      if (!res.ok) {
        toast(errMsg(res.j));
        if (res.j.maLoi === 'HET_HAN') { S.hold = null; go(1); }
        return null;
      }
      S.hold = res.j;
      S.promo = res.j.maUuDai || '';
      return res.j;
    }).catch(function () { toast('Lỗi kết nối, vui lòng thử lại.'); return null; });
  }
  function toStep2() {
    var btn = $('#btnNext'); btn.disabled = true;
    return loadAvail().then(function () {
      var av = S.avail[S.areaId];
      if (av && !av.duCho) { toast('Khu vực đã chọn không đủ chỗ. Vui lòng chọn khu vực hoặc giờ khác.'); return null; }
      if (S.mode !== 'CocCoDinh' && tongMon() === 0) S.mode = 'CocCoDinh';
      return hold();
    }).then(function (h) {
      btn.disabled = false;
      if (!h) return;
      $('#f-promo').value = S.promo;
      renderStep2(); go(2);
    });
  }

  /* ---------- Bước 2 ---------- */
  function renderStep2() {
    renderBill(); renderModes(); renderMethods(); renderPayPanel();
  }
  function renderBill() {
    var h = S.hold, row = function (k, v, c) { return '<div class="bill-row ' + (c || '') + '"><span>' + k + '</span><span>' + v + '</span></div>'; };
    var html = row('Tiền thực đơn đã chọn:', money(h.tongMon), 'main');
    if (h.phuThu > 0) html += row('Phụ thu khu vực:', money(h.phuThu));
    html += row('Thuế VAT (' + Math.round(D.rules.vat * 100) + '%):', money(h.vat));
    html += row('Phí phục vụ tiêu chuẩn (' + Math.round(D.rules.phiPhucVu * 100) + '%):', money(h.phiPhucVu));
    if (h.giamGia > 0) html += row('Ưu đãi (' + esc(h.maUuDai) + '):', '− ' + money(h.giamGia), 'disc');
    html += '<div class="bill-total"><span>Tổng hóa đơn ước tính:</span><span>' + money(h.tongHoaDon) + '</span></div>';
    $('#bill').innerHTML = html;
  }
  function renderModes() {
    $('#modes').innerHTML = MODES.map(function (m) {
      var amt = modeAmount(m[0]), off = m[0] !== 'CocCoDinh' && S.hold.tongHoaDon <= 0;
      var shown = m[0] === 'CocCoDinh' ? Number(amt).toLocaleString('vi-VN') + ' VNĐ' : money(amt);
      return '<button type="button" class="mode' + (S.mode === m[0] ? ' sel' : '') + (off ? ' off' : '') + '" data-mode="' + m[0] + '"' + (off ? ' title="Hãy chọn món trước để dùng chế độ này"' : '') + '>' +
        '<small>' + m[1] + '</small><b>' + shown + '</b><span>' + m[2] + '</span></button>';
    }).join('');
    $('#due').innerHTML = '<div><div class="due-l">Số tiền cọc cần thanh toán ngay:</div><div class="due-v">' + money(modeAmount(S.mode)) + '</div></div>' +
      '<div class="due-r"><div>Số tiền còn lại trả tại nhà hàng:</div><b>' + money(conLaiOf(S.mode)) + '</b></div>';
    $('#btnConfirm').innerHTML = icon('i-checkc') + ' Hoàn Tất Đặt Cọc (' + money(modeAmount(S.mode)) + ')';
    $('#holdNote').textContent = 'Chỗ của bạn được giữ đến ' + (S.hold.giuChoDen || '') + ' (' + D.rules.giuCho + ' phút). Hết thời gian này mà chưa xác nhận cọc, hệ thống sẽ tự nhả chỗ.';
  }
  function renderMethods() {
    $('#methods').innerHTML = METHODS.map(function (m) {
      return '<button type="button" class="mtab' + (S.method === m[0] ? ' sel' : '') + '" data-method="' + m[0] + '"><b>' + m[1] + '</b><small>' + m[2] + '</small></button>';
    }).join('');
  }
  function renderPayPanel() {
    var code = S.hold.maDatBan, amt = modeAmount(S.mode), box = $('#payPanel'), html = '';
    var line = function (k, v) { return '<div>' + k + ' ' + v + '</div>'; };
    if (S.method === 'VIETQR') {
      var bank = D.banks.filter(function (b) { return b.code === S.bank; })[0] || D.banks[0];
      html = '<div class="payinfo"><div class="bank-chips">' + D.banks.map(function (b) {
        return '<button type="button" class="bchip' + (b.code === bank.code ? ' on' : '') + '" data-bank="' + esc(b.code) + '">' + esc(b.ten) + '</button>';
      }).join('') + '</div><div class="qrbox" id="vqr"></div>' +
        '<div class="pi-name">' + esc(bank.tenDayDu) + '</div>' +
        line('Số tài khoản:', '<b class="amber">' + esc(bank.soTaiKhoan) + '</b>') +
        line('Chủ tài khoản:', '<b>' + esc(bank.chuTaiKhoan) + '</b>') +
        line('Số tiền đặt cọc:', '<b class="amber">' + money(amt) + '</b>') +
        line('Nội dung chuyển khoản:', '<b class="amber">' + esc(code) + '</b>') + '</div>';
      box.innerHTML = html; drawVietQR(bank, amt, code);
    } else if (S.method === 'WALLET') {
      html = '<div class="payinfo"><div class="bank-chips">' + [['MOMO', 'MoMo'], ['VNPAY', 'VNPay']].map(function (w) {
        return '<button type="button" class="bchip' + (S.wallet === w[0] ? ' on' : '') + '" data-wallet="' + w[0] + '">' + w[1] + '</button>';
      }).join('') + '</div><div class="pi-name">Thanh toán qua ví ' + (S.wallet === 'MOMO' ? 'MoMo' : 'VNPay') + '</div>' +
        line('Số tiền đặt cọc:', '<b class="amber">' + money(amt) + '</b>') +
        line('Nội dung / mã đơn:', '<b class="amber">' + esc(code) + '</b>') +
        '<p class="note">Cổng ví đang ở chế độ thử nghiệm: sau khi hoàn tất thanh toán trong ứng dụng ví, hãy bấm "Xác Nhận Đã Thanh Toán Cọc" bên dưới.</p></div>';
      box.innerHTML = html;
    } else {
      box.innerHTML = '<div class="payinfo"><div class="pi-name">Thẻ VISA / Mastercard</div>' +
        line('Số tiền đặt cọc:', '<b class="amber">' + money(amt) + '</b>') +
        line('Mã đơn:', '<b class="amber">' + esc(code) + '</b>') +
        '<p class="note">Thanh toán thẻ quốc tế đang ở chế độ thử nghiệm. Hệ thống không thu thập hay lưu số thẻ. Bấm "Xác Nhận Đã Thanh Toán Cọc" để hoàn tất.</p></div>';
    }
  }
  function drawVietQR(bank, amt, code) {
    var box = $('#vqr'), acc = String(bank.soTaiKhoan).replace(/[\s-]/g, '');
    function local() {
      box.innerHTML = '';
      try {
        new QRCode(box, { text: ['NH:' + bank.code, 'STK:' + bank.soTaiKhoan, 'CTK:' + bank.chuTaiKhoan, 'SOTIEN:' + amt, 'ND:' + code].join('\n'), width: 190, height: 190, colorDark: '#0a0d14', colorLight: '#ffffff' });
      } catch (e) { box.textContent = code; }
    }
    if (/^\d{6,19}$/.test(acc)) {   // tài khoản thật -> QR VietQR quét được bằng app ngân hàng
      var img = new Image(); img.alt = 'VietQR'; img.width = 210; img.onerror = local;
      img.src = 'https://img.vietqr.io/image/' + encodeURIComponent(bank.code) + '-' + acc + '-compact2.png?amount=' + amt +
                '&addInfo=' + encodeURIComponent(code) + '&accountName=' + encodeURIComponent(bank.chuTaiKhoan);
      box.innerHTML = ''; box.appendChild(img);
    } else local();
  }

  function applyPromo() {
    var code = $('#f-promo').value.trim().toUpperCase();
    var btn = $('#btnPromo'); btn.disabled = true;
    hold({ maUuDai: code }).then(function (h) {
      btn.disabled = false;
      if (!h) return;
      $('#f-promo').value = S.promo;
      toast(S.promo ? 'Đã áp dụng mã ' + S.promo + '.' : 'Đã gỡ mã ưu đãi.', true);
      renderStep2();
    });
  }

  function confirmPay() {
    var btn = $('#btnConfirm'), label = btn.innerHTML;
    btn.disabled = true; btn.textContent = 'Đang ghi nhận thanh toán…';
    var reset = function () { btn.disabled = false; btn.innerHTML = label; };
    postJson('/api/dat-ban/' + S.hold.id + '/thanh-toan', { phuongThuc: methodCode(), nganHang: S.method === 'VIETQR' ? S.bank : null, cheDoCoc: S.mode })
      .then(function (res) {
        if (res.status === 401) { reset(); toLogin(); return; }
        if (!res.ok) {
          toast(errMsg(res.j)); reset();
          if (res.j.maLoi === 'HET_HAN') { S.hold = null; go(1); }
          return;
        }
        S.booking = res.j; S.cart = {}; saveCart(); renderDone(); go(3);
      }).catch(function () { toast('Lỗi kết nối, vui lòng thử lại.'); reset(); });
  }

  /* ---------- Bước 3: phiếu hoàn tất ---------- */
  function renderDone() {
    var b = S.booking;
    var mail = b.emailDaGui ? 'Thông tin chi tiết đã gửi tới Email: ' + esc(b.email)
      : 'Email xác nhận chưa được gửi (nhà hàng chưa cấu hình máy chủ email). Bạn có thể xem lại phiếu tại "Đặt bàn của tôi".';
    var pt = { VIETQR: 'VIETQR', MOMO: 'MOMO', VNPAY: 'VNPAY', THE: 'THẺ VISA / MASTERCARD' }[b.phuongThuc] || b.phuongThuc;
    var row = function (k, v, c) { return '<div class="sum-row"><span>' + k + '</span><span class="' + (c || '') + '">' + v + '</span></div>'; };
    $('#done').innerHTML =
      '<div class="done-ic">' + icon('i-check') + '</div><span class="pill-ok">ĐÃ XÁC NHẬN CỌC</span>' +
      '<h2 class="done-title">PHIẾU ĐẶT CHỖ HOÀN TẤT</h2><p class="muted">' + mail + '</p>' +
      '<div class="vcard"><dl><dt>Mã Đặt Bàn VIP:</dt><dd class="code">' + esc(b.maDatBan) + '</dd>' +
      '<dt>Khách hàng:</dt><dd>' + esc(b.hoTen) + ' (' + esc(b.soDienThoai) + ')</dd>' +
      '<dt>Thời gian đón khách:</dt><dd>' + esc(b.gio) + ' - Ngày ' + esc(b.ngay) + '</dd>' +
      '<dt>Khu vực &amp; Số khách:</dt><dd class="amber">' + esc(b.khuVuc) + ' (' + b.soKhach + ' Khách)</dd></dl>' +
      '<div class="vcard-qr"><div class="qr" id="qr"></div><small>Xuất trình cho Lễ tân khi tới</small></div></div>' +
      '<h4 class="sub">CHI TIẾT ĐẶT CỌC &amp; THANH TOÁN:</h4>' +
      row('Số tiền cọc đã thanh toán:', money(b.tienCoc), 'green') +
      row('Số tiền còn lại thanh toán tại nhà hàng:', money(b.conLai), 'strong') +
      row('Phương thức thanh toán cọc:', esc(pt), 'strong') +
      '<p class="note">Đơn đang ở trạng thái "' + esc(b.trangThai) + '": nhà hàng sẽ đối chiếu giao dịch và xác nhận. Bạn có thể theo dõi tại mục "Đặt bàn của tôi".</p>' +
      '<div class="actions center no-print"><button type="button" class="btn-ghost" id="btnPrint">' + icon('i-print') + ' In / Lưu Vé Đặt Chỗ</button>' +
      '<button type="button" class="btn-main" id="btnNew">' + icon('i-refresh') + ' Tạo Lượt Đặt Bàn Mới</button></div>';
    try { new QRCode($('#qr'), { text: b.maDatBan, width: 130, height: 130, colorDark: '#0a0d14', colorLight: '#ffffff' }); } catch (e) { $('#qr').textContent = b.maDatBan; }
    $('#btnPrint').onclick = function () { window.print(); };
    $('#btnNew').onclick = function () { location.href = '/DatBan'; };
  }

  /* ---------- Lưu nháp khi phải đăng nhập ---------- */
  function saveDraft() {
    try { localStorage.setItem(DRAFT_KEY, JSON.stringify({ f: dataForm(), guests: S.guests, areaId: S.areaId, cart: S.cart, promo: S.promo, mode: S.mode })); } catch (e) { }
  }
  function loadDraft() {
    try {
      var raw = localStorage.getItem(DRAFT_KEY); if (!raw || !D.user.auth) return false;
      var d = JSON.parse(raw); localStorage.removeItem(DRAFT_KEY);
      $('#f-ten').value = d.f.hoTen; $('#f-sdt').value = d.f.soDienThoai; $('#f-email').value = d.f.email;
      $('#f-ngay').value = d.f.ngay; buildSlots(); $('#f-gio').value = d.f.gio;
      $('#f-dip').value = d.f.dip; $('#f-note').value = d.f.ghiChu;
      S.guests = d.guests; S.areaId = d.areaId; S.cart = d.cart || {}; S.promo = d.promo || ''; S.mode = d.mode || 'CocCoDinh';
      return true;
    } catch (e) { return false; }
  }

  /* ---------- Khởi tạo ---------- */
  function init() {
    var today = new Date(), ngay = $('#f-ngay');
    ngay.min = ymd(today); ngay.value = ymd(today);
    if (!buildSlots()) { var tm = new Date(today.getTime() + 86400000); ngay.value = ymd(tm); buildSlots(); }
    if (D.user.auth) { $('#f-ten').value = D.user.name; $('#f-sdt').value = D.user.phone; $('#f-email').value = D.user.email; }

    loadCart();
    var restored = loadDraft();
    $('#g-val').textContent = S.guests + ' Khách';
    renderCats(); renderMenu(); renderCart(); renderAreas();
    loadAvail().then(function () {
      if (restored) { toast('Đã khôi phục thông tin đặt bàn của bạn, đang chuyển sang bước đặt cọc…', true); toStep2(); }
    });

    ngay.addEventListener('change', function () { if (!buildSlots()) toast('Hôm nay đã hết khung giờ, vui lòng chọn ngày khác.'); scheduleAvail(); });
    $('#f-gio').addEventListener('change', scheduleAvail);
    $('#g-minus').onclick = function () { if (S.guests > 1) { S.guests--; $('#g-val').textContent = S.guests + ' Khách'; scheduleAvail(); } };
    $('#g-plus').onclick = function () {
      if (S.guests < D.rules.khachToiDa) { S.guests++; $('#g-val').textContent = S.guests + ' Khách'; scheduleAvail(); }
      else toast('Tối đa ' + D.rules.khachToiDa + ' khách mỗi đơn. Với đoàn đông vui lòng liên hệ nhà hàng.');
    };
    $('#areas').addEventListener('click', function (e) {
      var b = e.target.closest('.area'); if (!b) return;
      var id = +b.dataset.id, av = S.avail[id];
      if (av && !av.duCho) { toast('Khu vực này không đủ chỗ cho ' + S.guests + ' khách vào khung giờ đã chọn.'); return; }
      S.areaId = id; renderAreas();
    });
    $('#cats').addEventListener('click', function (e) { var b = e.target.closest('.chip'); if (b) { S.cat = b.dataset.cat; renderCats(); renderMenu(); } });
    $('#menu').addEventListener('click', function (e) {
      var a = e.target.closest('[data-add]'), i = e.target.closest('[data-inc]'), d = e.target.closest('[data-dec]');
      if (a) changeQty(+a.dataset.add, 1); else if (i) changeQty(+i.dataset.inc, 1); else if (d) changeQty(+d.dataset.dec, -1);
    });
    $('#cartBox').addEventListener('click', function (e) {
      var x = e.target.closest('[data-del]');
      if (x) { delete S.cart[x.dataset.del]; saveCart(); renderMenu(); renderCart(); }
    });

    $('#f1').addEventListener('submit', function (e) { e.preventDefault(); toStep2(); });
    $('#btnBack').onclick = function () { go(1); };
    $('#btnBack2').onclick = function () { go(1); };
    $('#btnPromo').onclick = applyPromo;
    $('#f-promo').addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); applyPromo(); } });
    $('#modes').addEventListener('click', function (e) {
      var b = e.target.closest('.mode'); if (!b) return;
      if (b.classList.contains('off')) { toast('Hãy chọn món trước để dùng chế độ cọc theo hóa đơn.'); return; }
      S.mode = b.dataset.mode; renderModes(); renderPayPanel();
    });
    $('#methods').addEventListener('click', function (e) { var b = e.target.closest('.mtab'); if (b) { S.method = b.dataset.method; renderMethods(); renderPayPanel(); } });
    $('#payPanel').addEventListener('click', function (e) {
      var bk = e.target.closest('[data-bank]'), w = e.target.closest('[data-wallet]');
      if (bk) { S.bank = bk.dataset.bank; renderPayPanel(); } else if (w) { S.wallet = w.dataset.wallet; renderPayPanel(); }
    });
    $('#btnConfirm').onclick = confirmPay;
    $('#tabs').addEventListener('click', function (e) {
      var t = e.target.closest('.tab'); if (!t || t.disabled) return;
      var n = +t.dataset.step; if (S.booking) return; if (n === 1) go(1); else if (n === 2 && S.hold && $('#f1').reportValidity()) toStep2();
    });
    go(1);
  }
  init();
})();
