// JS interop helpers for the Blazor UI.

window.submitCharacterForm = () => {
    document.getElementById("character-form")?.requestSubmit();
};

window.blazorReady = () => {};

// Telegram-style horizontal swipe between folders on the home page.
// The finger drags the current pane; on release it either slides out and asks Blazor
// to switch folders (the new pane animates in via CSS), or snaps back.
window.ddSwipe = (() => {
    const handlers = new WeakMap();

    const prefersReducedMotion = () =>
        window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    function init(area, dotNetRef) {
        if (!area) return;
        destroy(area);

        let startX = 0, startY = 0, startTime = 0, dx = 0;
        let tracking = false, axis = null, busy = false, pane = null;

        const canGo = dir => area.dataset[dir > 0 ? "canNext" : "canPrev"] === "true";
        const resetPane = () => {
            if (!pane) return;
            pane.style.transition = "";
            pane.style.transform = "";
            pane.style.opacity = "";
        };

        function onStart(e) {
            if (busy || e.touches.length !== 1) { tracking = false; return; }
            // Do not hijack gestures that begin in text fields (cursor / selection moves).
            if (e.target.closest && e.target.closest("input, textarea, select")) { tracking = false; return; }

            const t = e.touches[0];
            startX = t.clientX;
            startY = t.clientY;
            startTime = Date.now();
            dx = 0;
            axis = null;
            tracking = true;
            pane = area.querySelector(".folder-pane");
        }

        function onMove(e) {
            if (!tracking || !pane) return;

            const t = e.touches[0];
            const mx = t.clientX - startX;
            const my = t.clientY - startY;

            if (axis === null) {
                if (Math.abs(mx) < 10 && Math.abs(my) < 10) return;
                axis = Math.abs(mx) > Math.abs(my) * 1.2 ? "x" : "y";
            }
            if (axis !== "x") return;

            dx = mx;
            const dir = dx < 0 ? 1 : -1;
            // Rubber-band resistance when there is no folder in that direction.
            const shown = canGo(dir) ? dx : dx * 0.25;

            pane.style.transition = "none";
            pane.style.transform = `translateX(${shown}px)`;
            pane.style.opacity = String(Math.max(0.4, 1 - Math.abs(shown) / (area.clientWidth * 1.2)));
        }

        function onEnd() {
            if (!tracking) return;
            tracking = false;
            if (axis !== "x" || !pane) { return; }

            const dir = dx < 0 ? 1 : -1;
            const width = area.clientWidth || 1;
            const elapsed = Math.max(1, Date.now() - startTime);
            const fast = Math.abs(dx) > 40 && Math.abs(dx) / elapsed > 0.5; // px per ms
            const commit = canGo(dir) && (Math.abs(dx) > width * 0.28 || fast);

            if (!commit) {
                pane.style.transition = "transform .2s ease, opacity .2s ease";
                pane.style.transform = "translateX(0)";
                pane.style.opacity = "1";
                const p = pane;
                setTimeout(() => { if (p.isConnected) { p.style.transition = ""; p.style.transform = ""; p.style.opacity = ""; } }, 220);
                return;
            }

            busy = true;
            const leaving = pane;
            const duration = prefersReducedMotion() ? 0 : 160;
            leaving.style.transition = `transform ${duration}ms ease-in, opacity ${duration}ms ease-in`;
            leaving.style.transform = `translateX(${dir > 0 ? -100 : 100}%)`;
            leaving.style.opacity = "0";

            setTimeout(async () => {
                try {
                    await dotNetRef.invokeMethodAsync("OnSwipe", dir);
                } catch (_) {
                    // Component may be disposed; nothing to do.
                } finally {
                    // If Blazor kept the same element (switch was rejected), bring it back.
                    if (leaving.isConnected) {
                        leaving.style.transition = "";
                        leaving.style.transform = "";
                        leaving.style.opacity = "";
                    }
                    busy = false;
                }
            }, duration);
        }

        function onCancel() {
            if (!tracking) return;
            tracking = false;
            if (pane && axis === "x") {
                pane.style.transition = "transform .2s ease, opacity .2s ease";
                pane.style.transform = "translateX(0)";
                pane.style.opacity = "1";
                const p = pane;
                setTimeout(() => { if (p.isConnected) { p.style.transition = ""; p.style.transform = ""; p.style.opacity = ""; } }, 220);
            }
        }

        area.addEventListener("touchstart", onStart, { passive: true });
        area.addEventListener("touchmove", onMove, { passive: true });
        area.addEventListener("touchend", onEnd, { passive: true });
        area.addEventListener("touchcancel", onCancel, { passive: true });

        handlers.set(area, { onStart, onMove, onEnd, onCancel });
    }

    function destroy(area) {
        const h = area && handlers.get(area);
        if (!h) return;
        area.removeEventListener("touchstart", h.onStart);
        area.removeEventListener("touchmove", h.onMove);
        area.removeEventListener("touchend", h.onEnd);
        area.removeEventListener("touchcancel", h.onCancel);
        handlers.delete(area);
    }

    // Keeps the active folder tab visible in the scrollable tab bar after a swipe.
    function scrollActiveTabIntoView() {
        const tab = document.querySelector(".folder-tabs .folder-tab.active");
        if (!tab) return;
        tab.scrollIntoView({
            behavior: prefersReducedMotion() ? "auto" : "smooth",
            inline: "center",
            block: "nearest"
        });
    }

    return { init, destroy, scrollActiveTabIntoView };
})();
