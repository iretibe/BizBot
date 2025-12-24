(function () {

    const scriptTag = document.currentScript;

    // Required attributes
    const tenant = scriptTag.getAttribute("data-tenant");
    const widgetUrl = scriptTag.getAttribute("data-widget-url");

    // Optional theme attributes
    const color = scriptTag.getAttribute("data-color") || "#4f46e5";   // default purple
    const textColor = scriptTag.getAttribute("data-text") || "white";
    const welcome = scriptTag.getAttribute("data-welcome") || "Hi 👋 How can I help you today?";

    // ---- Floating Button ----
    const btn = document.createElement("div");
    btn.innerHTML = "💬";
    Object.assign(btn.style, {
        position: "fixed",
        bottom: "24px",
        right: "24px",
        width: "56px",
        height: "56px",
        background: color,
        color: textColor,
        fontSize: "28px",
        fontWeight: "bold",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        borderRadius: "50%",
        cursor: "pointer",
        transition: "transform 0.25s ease",
        boxShadow: "0 4px 10px rgba(0,0,0,0.3)",
        zIndex: "999999",
    });
    document.body.appendChild(btn);


    // ---- Chat Panel (Sliding) ----
    const panel = document.createElement("div");
    Object.assign(panel.style, {
        position: "fixed",
        bottom: "100px",
        right: "24px",
        width: "360px",
        height: "520px",
        background: "white",
        borderRadius: "14px",
        overflow: "hidden",
        boxShadow: "0 6px 18px rgba(0,0,0,0.3)",
        transform: "translateY(40px)",
        opacity: "0",
        transition: "transform 0.35s ease, opacity 0.2s ease",
        zIndex: "999998",
        pointerEvents: "none",
    });
    document.body.appendChild(panel);

    // ---- Iframe ----
    const frame = document.createElement("iframe");
    frame.src = `${widgetUrl}?tenant=${tenant}&welcome=${encodeURIComponent(welcome)}`;
    frame.style.width = "100%";
    frame.style.height = "100%";
    frame.style.border = "none";
    panel.appendChild(frame);


    // ---- Animation Toggle ----
    let open = false;

    btn.onclick = () => {
        open = !open;
        if (open) {
            btn.style.transform = "scale(0.9)";

            panel.style.opacity = "1";
            panel.style.pointerEvents = "auto";
            panel.style.transform = "translateY(0)";
        }
        else {
            btn.style.transform = "scale(1)";

            panel.style.opacity = "0";
            panel.style.pointerEvents = "none";
            panel.style.transform = "translateY(40px)";
        }
    };

})();
