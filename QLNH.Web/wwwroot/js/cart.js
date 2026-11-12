(function () {
  "use strict";

  async function capNhatGioHang(submitter) {
    const form = document.getElementById("formGio");
    if (!form) return;

    const scrollPos = window.scrollY;
    const fd = new FormData(form);
    if (submitter && submitter.name) {
      fd.set(submitter.name, submitter.value);
    } else {
      fd.set("op", "refresh");
    }

    try {
      const res = await fetch(form.action, {
        method: "POST",
        body: fd,
        credentials: "same-origin",
      });
      if (
        res.redirected &&
        res.url &&
        !res.url.toLowerCase().includes("/cart")
      ) {
        window.location.href = res.url;
        return;
      }
      const html = await res.text();
      const doc = new DOMParser().parseFromString(html, "text/html");
      const newContainer = doc.getElementById("cart-page-container");
      const curContainer = document.getElementById("cart-page-container");
      if (newContainer && curContainer) {
        curContainer.innerHTML = newContainer.innerHTML;
        const newBadge = doc.querySelector("#cart-btn .cart-badge");
        const curCartBtn = document.getElementById("cart-btn");
        if (curCartBtn) {
          const curBadge = curCartBtn.querySelector(".cart-badge");
          if (newBadge) {
            if (curBadge) curBadge.textContent = newBadge.textContent;
          } else if (curBadge) {
            curBadge.remove();
          }
        }
        window.scrollTo({ top: scrollPos, behavior: "instant" });
      }
    } catch (err) {
      form.submit();
    }
  }

  function guiLaiGioHang() {
    capNhatGioHang(document.getElementById("btnRefresh"));
  }

  function capNhatNgayGioNhanBan() {
    const ngayInput = document.getElementById("ngayNhanBan");
    const gioInput = document.getElementById("gioNhanBan");
    const giaTriGui = document.getElementById("ThoiGianDen");

    if (!ngayInput || !gioInput || !giaTriGui || !ngayInput.value) {
      return;
    }

    const bayGio = new Date();

    for (const option of gioInput.options) {
      const thoiDiem = new Date(`${ngayInput.value}T${option.value}`);
      option.disabled = thoiDiem <= bayGio;
    }

    const luaChonHienTai = gioInput.selectedOptions[0];

    if (!luaChonHienTai || luaChonHienTai.disabled) {
      const gioConLai = Array.from(gioInput.options).find(
        (option) => !option.disabled
      );

      if (!gioConLai) {
        giaTriGui.value = "";
        ngayInput.setCustomValidity(
          "Ngày này đã hết khung giờ. Vui lòng chọn ngày khác."
        );
        ngayInput.reportValidity();
        return;
      }

      gioInput.value = gioConLai.value;
    }

    ngayInput.setCustomValidity("");
    giaTriGui.value = `${ngayInput.value}T${gioInput.value}`;
    guiLaiGioHang();
  }

  document.addEventListener("submit", function (e) {
    const form = e.target;
    if (!form || form.id !== "formGio") return;
    const submitter = e.submitter;
    if (submitter && submitter.name === "op" && submitter.value === "next") {
      return;
    }
    e.preventDefault();
    capNhatGioHang(submitter);
  });

  document.addEventListener("click", function (e) {
    const quickDayBtn = e.target.closest("[data-quick-day]");
    if (!quickDayBtn) return;

    const ngayInput = document.getElementById("ngayNhanBan");
    if (!ngayInput) return;

    ngayInput.value = quickDayBtn.getAttribute("data-quick-day");
    capNhatNgayGioNhanBan();
  });

  document.addEventListener("change", function (e) {
    const target = e.target;
    if (!(target instanceof HTMLElement)) return;

    if (target.id === "ngayNhanBan" || target.id === "gioNhanBan") {
      capNhatNgayGioNhanBan();
      return;
    }

    if (target.hasAttribute("data-cart-refresh")) {
      guiLaiGioHang();
      return;
    }

    if (target.hasAttribute("data-payment-refresh")) {
      const formTT = document.getElementById("formTT");
      const btnRefresh = document.getElementById("btnRefresh");
      if (formTT && btnRefresh) {
        formTT.requestSubmit(btnRefresh);
      }
    }
  });

  function initHoldTimer() {
    const el = document.getElementById("hold");
    const btn = document.getElementById("btnPay");
    if (!el || !btn) return;

    let s = parseInt(el.dataset.sec || "0", 10);

    function tick() {
      if (s <= 0) {
        el.className = "alert alert-danger small mt-3 mb-2";
        el.textContent =
          "Hết thời gian giữ chỗ. Quay lại giỏ hàng để đặt lại.";
        btn.disabled = true;
        return;
      }
      const m = Math.floor(s / 60);
      const r = s % 60;
      el.textContent =
        "Bàn đang được giữ cho bạn trong " +
        String(m).padStart(2, "0") +
        ":" +
        String(r).padStart(2, "0");
      s--;
      setTimeout(tick, 1000);
    }

    tick();
  }

  function initBookingStatusPolling() {
    const container = document.querySelector("[data-booking-status-id]");
    if (!container) return;

    const bookingId = container.getAttribute("data-booking-status-id");
    let currentStatus =
      container.getAttribute("data-booking-initial-status") || "";

    const badge = document.getElementById("status-badge");
    const desc = document.getElementById("status-desc");
    const reminder = document.getElementById("arrival-reminder");

    function getBadgeClass(tt) {
      switch (tt) {
        case "Đã xác nhận":
          return "badge bg-success";
        case "Đang chế biến":
          return "badge bg-primary";
        case "Đã hoàn thành":
        case "Hoàn thành":
        case "Đã thanh toán":
          return "badge bg-info text-dark";
        case "Đã hủy":
          return "badge bg-danger";
        default:
          return "badge bg-warning text-dark";
      }
    }

    function getDesc(tt) {
      switch (tt) {
        case "Đã xác nhận":
          return "Nhà hàng đã xác nhận đơn đặt bàn của bạn. Vui lòng đến đúng giờ hẹn.";
        case "Đang chế biến":
          return "Nhà hàng đang chuẩn bị món ăn phục vụ bàn của bạn.";
        case "Đã hoàn thành":
        case "Hoàn thành":
        case "Đã thanh toán":
          return "Đơn đặt bàn đã hoàn tất và thanh toán. Cảm ơn quý khách!";
        case "Đã hủy":
          return "Đơn đặt bàn này đã bị hủy.";
        default:
          return "Nhà hàng sẽ đối chiếu khoản cọc và xác nhận với bạn sớm nhất.";
      }
    }

    async function checkStatus() {
      try {
        const res = await fetch(`/Cart/Status/${bookingId}`, {
          cache: "no-store",
        });
        if (!res.ok) return;
        const data = await res.json();
        if (data.trangThai && data.trangThai !== currentStatus) {
          currentStatus = data.trangThai;
          if (badge) {
            badge.textContent = currentStatus;
            badge.className = getBadgeClass(currentStatus);
          }
          if (desc) {
            desc.textContent = getDesc(currentStatus);
          }
          if (reminder) {
            reminder.classList.toggle(
              "d-none",
              currentStatus !== "Đang chờ duyệt" &&
                currentStatus !== "Đã xác nhận"
            );
          }
        }
      } catch (e) {}
    }

    setInterval(checkStatus, 2000);
  }

  initHoldTimer();
  initBookingStatusPolling();
})();
