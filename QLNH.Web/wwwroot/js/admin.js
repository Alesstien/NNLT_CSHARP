(function () {
  "use strict";

  document.addEventListener("click", function (e) {
    const printBtn = e.target.closest('[data-action="print"]');
    if (printBtn) {
      window.print();
    }
  });

  document.addEventListener("submit", function (e) {
    const form = e.target;
    if (!(form instanceof HTMLFormElement)) return;
    if (!form.hasAttribute("data-admin-prompt-field")) return;

    const fieldName = form.getAttribute("data-admin-prompt-field");
    const promptMsg =
      form.getAttribute("data-prompt-message") ||
      "Lý do hủy (có thể bỏ trống):";
    const val = window.prompt(promptMsg);
    if (val === null) {
      e.preventDefault();
      return;
    }
    const input = form.querySelector(`input[name="${fieldName}"]`);
    if (input) {
      input.value = val;
    }
  });

  const qrEl = document.getElementById("qr");
  if (qrEl && qrEl.hasAttribute("data-qr-text") && typeof QRCode !== "undefined") {
    const text = qrEl.getAttribute("data-qr-text") || "";
    try {
      new QRCode(qrEl, {
        text: text,
        width: 130,
        height: 130,
        colorDark: "#0a0d14",
        colorLight: "#ffffff",
      });
    } catch (e) {}
  }

  const banner = document.getElementById("newBanner");
  if (banner && banner.hasAttribute("data-initial-pending")) {
    const n0 = parseInt(banner.getAttribute("data-initial-pending") || "0", 10);
    setInterval(function () {
      fetch("/Admin/DemChoDuyet", { credentials: "same-origin" })
        .then(function (r) {
          return r.ok ? r.json() : null;
        })
        .then(function (j) {
          if (j && j.n !== n0) {
            banner.hidden = false;
            document.title = "(" + j.n + ") Đơn chờ duyệt";
          }
        })
        .catch(function () {});
    }, 20000);
  }
})();
