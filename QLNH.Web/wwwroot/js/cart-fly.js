(function () {
  "use strict";

  const style = document.createElement("style");

  style.textContent = `
        .cart-fly-clone {
            position: fixed;
            border-radius: 50%;
            object-fit: cover;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.35);
            pointer-events: none;
            z-index: 9999;
            transition:
                left 0.7s ease,
                top 0.7s ease,
                width 0.7s ease,
                height 0.7s ease,
                opacity 0.7s ease;
        }

        #cart-btn {
            position: relative;
        }

        .cart-badge {
            position: absolute;
            top: -6px;
            right: -8px;
            background: #dc3545;
            color: white;
            border-radius: 50%;
            font-size: 12px;
            min-width: 18px;
            height: 18px;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 0 3px;
            font-weight: bold;
        }
    `;

  document.head.appendChild(style);

  function getCartBtn() {
    return document.getElementById("cart-btn");
  }

  function updateBadge(button, count) {
    if (!button) return;

    let badge = button.querySelector(".cart-badge");

    if (!badge) {
      badge = document.createElement("span");
      badge.className = "cart-badge";
      button.appendChild(badge);
    }

    badge.textContent = String(count);
  }

  function flyToCart(img, button) {
    if (!img || !button) return;

    const rect = img.getBoundingClientRect();
    const clone = document.createElement("img");

    clone.src = img.src;
    clone.className = "cart-fly-clone";

    const size = Math.min(rect.width, rect.height, 120);

    Object.assign(clone.style, {
      width: size + "px",
      height: size + "px",
      left: rect.left + rect.width / 2 - size / 2 + "px",
      top: rect.top + rect.height / 2 - size / 2 + "px",
      opacity: "1",
    });

    document.body.appendChild(clone);

    requestAnimationFrame(function () {
      requestAnimationFrame(function () {
        const target = button.getBoundingClientRect();
        const endSize = 30;

        Object.assign(clone.style, {
          width: endSize + "px",
          height: endSize + "px",
          left: target.left + target.width / 2 - endSize / 2 + "px",
          top: target.top + target.height / 2 - endSize / 2 + "px",
          opacity: "0",
        });
      });
    });

    setTimeout(function () {
      clone.remove();
    }, 850);
  }

  async function ajaxAdd(form, button) {
    const submitButton = form.querySelector(
      'button[type="submit"], input[type="submit"]',
    );

    if (submitButton) {
      submitButton.disabled = true;
    }

    try {
      const formData = new FormData(form);
      const tokens = formData.getAll("__RequestVerificationToken");
      if (tokens.length > 1) {
        formData.set("__RequestVerificationToken", tokens[0]);
      }

      const response = await fetch(form.action, {
        method: "POST",
        body: formData,
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          Accept: "application/json",
        },
        credentials: "same-origin",
      });

      const contentType = response.headers.get("content-type") || "";

      if (!contentType.includes("application/json")) {
        const body = await response.text();

        console.error("Máy chủ không trả về JSON:", {
          status: response.status,
          response: body,
        });

        alert(
          "Không nhận được phản hồi JSON từ máy chủ. " +
            "Vui lòng kiểm tra F12 > Network.",
        );

        return;
      }

      const result = await response.json();

      if (!response.ok || result.success !== true) {
        console.error("Lỗi thêm món:", response.status, result);

        alert(result.message || "Không thể thêm món vào giỏ hàng.");

        return;
      }

      updateBadge(button, result.cartCount);
    } catch (error) {
      console.error("Lỗi khi thêm món vào giỏ hàng:", error);

      alert("Có lỗi khi thêm món. " + "Vui lòng mở F12 > Console để kiểm tra.");
    } finally {
      if (submitButton) {
        submitButton.disabled = false;
      }
    }
  }

  document.addEventListener("submit", function (event) {
    const target = event.target;

    if (!(target instanceof HTMLFormElement)) return;

    const form = target.closest("form.cart-add-form");

    if (!form) return;

    const button = getCartBtn();

    // Nếu không tìm thấy nút giỏ hàng, để form gửi bình thường.
    if (!button) return;

    event.preventDefault();

    const card = form.closest(".card");
    const img = card ? card.querySelector("img.cart-item-img") : null;

    // Tạo hiệu ứng ảnh bay vào giỏ hàng.
    if (img) {
      flyToCart(img, button);
    }

    // Gửi yêu cầu thêm món đến máy chủ.
    ajaxAdd(form, button);
  });
})();
