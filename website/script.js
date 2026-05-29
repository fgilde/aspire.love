(function () {
    const STORAGE_KEY = "aspire-love-lang";
    const SUPPORTED = ["en", "de"];

    function pickInitialLang() {
        const saved = localStorage.getItem(STORAGE_KEY);
        if (saved && SUPPORTED.includes(saved)) return saved;
        const browser = (navigator.language || "en").slice(0, 2).toLowerCase();
        return SUPPORTED.includes(browser) ? browser : "en";
    }

    function applyLang(lang) {
        const dict = window.I18N[lang];
        if (!dict) return;

        document.documentElement.lang = lang;
        localStorage.setItem(STORAGE_KEY, lang);

        for (const el of document.querySelectorAll("[data-i18n]")) {
            const key = el.getAttribute("data-i18n");
            const value = dict[key];
            if (value === undefined) continue;

            if (el.tagName === "META") {
                el.setAttribute("content", value);
            } else {
                el.textContent = value;
            }
        }

        for (const span of document.querySelectorAll(".lang-toggle [data-lang]")) {
            span.classList.toggle("active", span.getAttribute("data-lang") === lang);
        }
    }

    let current = pickInitialLang();
    applyLang(current);

    document.getElementById("langToggle").addEventListener("click", function () {
        current = current === "en" ? "de" : "en";
        applyLang(current);
    });

    // Copy-to-clipboard for the install snippet.
    const copyBtn = document.getElementById("copyBtn");
    if (copyBtn) {
        copyBtn.addEventListener("click", async function () {
            const cmd = "dotnet tool install -g love.aspire\naspire-love init --path ./my-lovable-app";
            try {
                await navigator.clipboard.writeText(cmd);
                const dict = window.I18N[current];
                copyBtn.textContent = dict["start.copied"];
                copyBtn.classList.add("copied");
                setTimeout(() => {
                    copyBtn.textContent = dict["start.copy"];
                    copyBtn.classList.remove("copied");
                }, 1600);
            } catch {
                /* clipboard unavailable — ignore */
            }
        });
    }

    // Reveal-on-scroll for cards and sections.
    const observer = new IntersectionObserver(
        (entries) => {
            for (const entry of entries) {
                if (entry.isIntersecting) {
                    entry.target.classList.add("in-view");
                    observer.unobserve(entry.target);
                }
            }
        },
        { threshold: 0.12 }
    );
    for (const el of document.querySelectorAll(".mini, .step, .card, .feature, .shot-frame, .section-title, .section-lead")) {
        el.classList.add("reveal");
        observer.observe(el);
    }
})();
