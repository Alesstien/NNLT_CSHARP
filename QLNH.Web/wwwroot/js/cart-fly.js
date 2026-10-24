```javascript
(function () {
    "use strict";

    const style = document.createElement("style");
    style.textContent = `
        .cart-fly-clone {
            position: fixed;
            border-radius: 50%;
            object-fit: cover;
            box-shadow: 0 4px 20px rgba(0,0,0,.35);
            pointer-events: none;
            z-index: 9999;
            transition:
                left .7s ease,
                top .7s ease,
                width .7s ease,
                height .7s ease,
                opacity .7s ease;
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
            left: (rect.left + rect.width / 2 - size / 2) + "px",
            top: (rect.top + rect.height / 2 - size / 2) + "px",
            opacity: "1"
        });

        document.body.appendChild(clone);

        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                const target = button.getBoundingClientRect();
                const endSize = 30;

                Object.assign(clone.style, {
                    width: endSize + "px",
                    height: endSize + "px",
                    left: (target.left + target.width / 2 - endSize / 2) + "px",
                    top: (target.top + target.height / 2 - endSize / 2) + "px",
                    opacity: "0"
                });
            });
        });

        setTimeout(() => clone.remove(), 850);
    }

    async function ajaxAdd(form, button) {
        const submitButton = form.querySelector('button[type="submit"]');

        if (submitButton) submitButton.disabled = true;

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: {
                    "X-Requested-With": "XMLHttpRequest",
                    "Accept": "application/json"
                },
                credentials: "same-origin"
            });

            const contentType = response.headers.get("content-type") || "";

            if (!contentType.includes("application/json")) {
                const body = await response.text();

                console.error("Máy chủ không trả về JSON:", {
                    status: response.status,
                    response: body
                });

                alert(
                    "Máy chủ trả về phản hồi không đúng định dạng. " +
                    "Hãy mở F12 > Network > Add để kiểm tra."
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
            alert(result.message || "Đã thêm món vào giỏ hàng!");
        } catch (error) {
            console.error("Lỗi kết nối hoặc xử lý phản hồi:", error);
            alert("Có lỗi khi thêm món. Hãy mở F12 > Console để xem chi tiết.");
        } finally {
            if (submitButton) submitButton.disabled = false;
        }
    }

    document.addEventListener("submit", function (event) {
        const form = event.target.closest("form.cart-add-form");
        if (!form) return;

        const button = getCartBtn();

        // Không có nút giỏ hàng thì gửi form bình thường.
        if (!button) return;

        event.preventDefault();

        const card = form.closest(".card");
        const img = card ? card.querySelector("img.cart-item-img") : null;

        // Hiệu ứng chỉ mang tính trang trí; kết quả thêm món do server quyết định.
        if (img) flyToCart(img, button);

        ajaxAdd(form, button);
    });
})();
```