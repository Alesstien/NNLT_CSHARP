(function () {
  "use strict";

  document.addEventListener("click", function (e) {
    const btn = e.target.closest("[data-toggle-password]");
    if (!btn) return;

    const inputId = btn.getAttribute("data-toggle-password");
    const input = document.getElementById(inputId);
    if (!input) return;

    const show = input.type === "password";
    input.type = show ? "text" : "password";

    const eye = btn.querySelector(".icon-eye");
    const eyeOff = btn.querySelector(".icon-eye-off");
    if (eye) eye.classList.toggle("d-none", !show);
    if (eyeOff) eyeOff.classList.toggle("d-none", show);
  });

  document.addEventListener(
    "error",
    function (e) {
      const img = e.target;
      if (!(img instanceof HTMLImageElement)) return;

      const fallback = img.getAttribute("data-fallback-src");
      if (fallback && img.getAttribute("src") !== fallback) {
        img.setAttribute("src", fallback);
      }
    },
    true
  );

  document.addEventListener("submit", function (e) {
    const form = e.target;
    if (!(form instanceof HTMLFormElement)) return;

    const confirmMsg = form.getAttribute("data-confirm");
    if (confirmMsg && !window.confirm(confirmMsg)) {
      e.preventDefault();
      return;
    }

    const promptField = form.getAttribute("data-prompt-field");
    if (promptField) {
      const promptMsg =
        form.getAttribute("data-prompt-message") ||
        "Nhập thông tin (có thể bỏ trống):";
      const val = window.prompt(promptMsg);
      if (val === null) {
        e.preventDefault();
        return;
      }
      const hiddenInput = form.querySelector(`input[name="${promptField}"]`);
      if (hiddenInput) {
        hiddenInput.value = val;
      }
    }
  });

  function initHomeCategoryFilter() {
    const filterBox = document.getElementById("home-dm-filter");
    if (!filterBox) return;

    const btns = filterBox.querySelectorAll(".dm-filter-btn");
    const items = document.querySelectorAll("#home-mon-grid .home-mon-item");
    const title = document.getElementById("home-mon-title");
    const empty = document.getElementById("home-mon-empty");
    let currentDm = "all";

    function locTheoDanhMuc(dmId, tenDm) {
      if (dmId !== "all" && currentDm === dmId) {
        dmId = "all";
        tenDm = "Tất cả";
      }
      currentDm = dmId;

      btns.forEach(function (b) {
        const isSelected = b.getAttribute("data-dm") === dmId;
        b.classList.toggle("btn-danger", isSelected);
        b.classList.toggle("btn-outline-danger", !isSelected);
        b.classList.toggle("active", isSelected);
        const badge = b.querySelector(".badge");
        if (badge) {
          badge.classList.toggle("bg-light", isSelected);
          badge.classList.toggle("text-danger", isSelected);
          badge.classList.toggle("bg-danger", !isSelected);
        }
      });

      let visibleCount = 0;
      items.forEach(function (item) {
        const match = dmId === "all" || item.getAttribute("data-dm") === dmId;
        item.classList.toggle("d-none", !match);
        if (match) visibleCount++;
      });

      if (title) {
        title.textContent =
          dmId === "all"
            ? "Thực đơn món ăn (" + visibleCount + " món)"
            : "Thực đơn món ăn — " + tenDm + " (" + visibleCount + " món)";
      }
      if (empty) {
        empty.classList.toggle("d-none", visibleCount > 0);
      }
    }

    btns.forEach(function (btn) {
      btn.addEventListener("click", function () {
        locTheoDanhMuc(
          btn.getAttribute("data-dm"),
          btn.getAttribute("data-ten")
        );
      });
    });
  }

  initHomeCategoryFilter();
})();
