/**
 * cart-fly.js  – Hiệu ứng ảnh bay vào giỏ hàng
 *
 *  Cách hoạt động:
 *  1. Lắng nghe click trên form.cart-add-form
 *  2. Clone ảnh món ăn, gán position:fixed tại vị trí ảnh gốc
 *  3. Animate clone bay về icon giỏ hàng (#cart-btn)
 *  4. Gửi AJAX (fetch) POST thêm vào giỏ trong nền
 *  5. Cập nhật counter badge trên nút giỏ hàng
 */

(function () {
    'use strict';

    /* ─── CSS animation ─────────────────────────────────── */
    const style = document.createElement('style');
    style.textContent = `
    .cart-fly-clone {
        position: fixed;
        border-radius: 50%;
        object-fit: cover;
        box-shadow: 0 4px 20px rgba(0,0,0,.35);
        pointer-events: none;
        z-index: 9999;
        transition: transform .7s cubic-bezier(.2,.8,.3,1),
                    left .7s cubic-bezier(.2,.8,.3,1),
                    top  .7s cubic-bezier(.2,.8,.3,1),
                    width  .7s cubic-bezier(.2,.8,.3,1),
                    height .7s cubic-bezier(.2,.8,.3,1),
                    opacity .7s ease;
    }

    #cart-btn {
        position: relative;
        transition: transform .2s;
    }
    #cart-btn.cart-bounce {
        animation: cartBounce .4s ease;
    }
    @keyframes cartBounce {
        0%   { transform: scale(1); }
        40%  { transform: scale(1.35); }
        70%  { transform: scale(.9); }
        100% { transform: scale(1); }
    }

    .cart-badge {
        position: absolute;
        top: -6px;
        right: -8px;
        background: #dc3545;
        color: #fff;
        border-radius: 50%;
        font-size: .7rem;
        min-width: 18px;
        height: 18px;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 0 3px;
        font-weight: 700;
        pointer-events: none;
        transition: transform .25s;
    }
    `;
    document.head.appendChild(style);

    /* ─── Helpers ────────────────────────────────────────── */
    function getCartBtn() {
        return document.getElementById('cart-btn');
    }

    function getOrCreateBadge(btn) {
        let badge = btn.querySelector('.cart-badge');
        if (!badge) {
            badge = document.createElement('span');
            badge.className = 'cart-badge';
            badge.textContent = '0';
            btn.style.position = 'relative';
            btn.appendChild(badge);
        }
        return badge;
    }

    function incrementBadge(btn) {
        const badge = getOrCreateBadge(btn);
        const n = parseInt(badge.textContent || '0', 10);
        badge.textContent = n + 1;
        badge.style.transform = 'scale(1.4)';
        setTimeout(() => { badge.style.transform = 'scale(1)'; }, 250);

        btn.classList.remove('cart-bounce');
        void btn.offsetWidth; // reflow
        btn.classList.add('cart-bounce');
        btn.addEventListener('animationend', () => btn.classList.remove('cart-bounce'), { once: true });
    }

    /* ─── Animation ──────────────────────────────────────── */
    function flyToCart(imgSrc, startRect, cartBtn) {
        const clone = document.createElement('img');
        clone.src = imgSrc;
        clone.className = 'cart-fly-clone';

        const SIZE_START = Math.min(startRect.width, startRect.height, 120);
        const SIZE_END = 30;

        Object.assign(clone.style, {
            width:  SIZE_START + 'px',
            height: SIZE_START + 'px',
            left:   (startRect.left + startRect.width  / 2 - SIZE_START / 2) + 'px',
            top:    (startRect.top  + startRect.height / 2 - SIZE_START / 2) + 'px',
            opacity: '1',
        });

        document.body.appendChild(clone);

        // Kích hoạt transition ở frame tiếp theo
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                const btnRect = cartBtn.getBoundingClientRect();
                const destLeft = btnRect.left + btnRect.width  / 2 - SIZE_END / 2;
                const destTop  = btnRect.top  + btnRect.height / 2 - SIZE_END / 2;

                Object.assign(clone.style, {
                    left:    destLeft + 'px',
                    top:     destTop  + 'px',
                    width:   SIZE_END + 'px',
                    height:  SIZE_END + 'px',
                    opacity: '0',
                });
            });
        });

        clone.addEventListener('transitionend', () => {
            clone.remove();
            incrementBadge(cartBtn);
        }, { once: true });
    }

    /* ─── AJAX add to cart ───────────────────────────────── */
    async function ajaxAdd(form) {
        const data = new FormData(form);
        try {
            await fetch(form.action, {
                method: 'POST',
                body: data,
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                redirect: 'manual', // không follow redirect
            });
        } catch (_) { /* bỏ qua lỗi mạng */ }
    }

    /* ─── Event delegation ───────────────────────────────── */
    document.addEventListener('submit', function (e) {
        const form = e.target.closest('form.cart-add-form');
        if (!form) return;

        const cartBtn = getCartBtn();
        if (!cartBtn) return; // fallback: submit bình thường

        e.preventDefault();

        // Lấy ảnh từ card gần nhất hoặc .cart-item-img trong form
        let img =
            form.querySelector('img.cart-item-img') ||
            form.closest('.card')?.querySelector('img') ||
            form.closest('.row')?.querySelector('img') ||
            null;

        if (img) {
            flyToCart(img.src, img.getBoundingClientRect(), cartBtn);
        } else {
            incrementBadge(cartBtn);
        }

        ajaxAdd(form);
    });
})();
