(function () {
    const scriptTag = document.currentScript;
    const widgetUrl = scriptTag.getAttribute("data-widget-url");
    const widgetToken = scriptTag.getAttribute("data-widget-token");

    if (!widgetUrl || !widgetToken) {
        console.error("BizBot widget: missing data-widget-url or data-widget-token");
        return;
    }

    // Floating Button
    const btn = document.createElement("div");
    btn.innerHTML = "💬";
    Object.assign(btn.style, {
        position: "fixed",
        bottom: "24px",
        right: "24px",
        width: "56px",
        height: "56px",
        background: "#4f46e5",
        color: "white",
        fontSize: "28px",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        borderRadius: "50%",
        cursor: "pointer",
        zIndex: "999999",
    });
    document.body.appendChild(btn);

    // Panel
    const panel = document.createElement("div");
    Object.assign(panel.style, {
        position: "fixed",
        bottom: "100px",
        right: "24px",
        width: "360px",
        height: "520px",
        background: "white",
        borderRadius: "14px",
        boxShadow: "0 6px 18px rgba(0,0,0,0.3)",
        opacity: "0",
        pointerEvents: "none",
        transition: "0.3s",
        zIndex: "999998",
    });
    document.body.appendChild(panel);

    // Iframe (created once)
    const frame = document.createElement("iframe");
    frame.style.width = "100%";
    frame.style.height = "100%";
    frame.style.border = "none";
    frame.setAttribute(
        "sandbox",
        "allow-scripts allow-same-origin"
    );
    frame.setAttribute("loading", "lazy");
    panel.appendChild(frame);

    let loaded = false;

    // Fetch widget config securely
    fetch(`${widgetUrl}/api/widget/config`, {
        headers: {
            "Authorization": `Bearer ${widgetToken}`
        }
    })
        .then(r => r.json())
        .then(cfg => {
            btn.style.background = cfg.theme?.primaryColor || "#4f46e5";

            const welcome =
                cfg.welcomeMessage || "Hi friend, how can I help you today?";

            frame.src =
                `${widgetUrl}/embed?token=${encodeURIComponent(widgetToken)}&welcome=${encodeURIComponent(welcome)}`;

            if (cfg.showBranding) {
                const badge = document.createElement("div");
                badge.innerText = "Powered by BizBot";
                badge.style.fontSize = "11px";
                badge.style.textAlign = "center";
                panel.appendChild(badge);
            }

            loaded = true;
        });

    // Toggle
    let open = false;
    btn.onclick = () => {
        open = !open;
        panel.style.opacity = open ? "1" : "0";
        panel.style.pointerEvents = open ? "auto" : "none";
    };
})();
