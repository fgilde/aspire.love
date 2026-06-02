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
    for (const el of document.querySelectorAll(".mini, .step, .card, .feature, .shot-frame, .section-title, .section-lead, .faq-item")) {
        el.classList.add("reveal");
        observer.observe(el);
    }

    // Scrollspy: highlight the nav link for the section currently in view.
    const navLinks = [...document.querySelectorAll(".nav-links a[href^='#']")];
    const linkById = new Map(navLinks.map((a) => [a.getAttribute("href").slice(1), a]));
    const spied = [...linkById.keys()]
        .map((id) => document.getElementById(id))
        .filter(Boolean);

    if (spied.length) {
        const spy = new IntersectionObserver(
            (entries) => {
                for (const entry of entries) {
                    if (!entry.isIntersecting) continue;
                    for (const a of navLinks) a.classList.remove("active");
                    linkById.get(entry.target.id)?.classList.add("active");
                }
            },
            { rootMargin: "-45% 0px -50% 0px", threshold: 0 }
        );
        for (const section of spied) spy.observe(section);
    }

    // Live GitHub star count on the open-source CTA (best-effort, silently skipped on failure).
    const starCount = document.getElementById("starCount");
    if (starCount) {
        fetch("https://api.github.com/repos/fgilde/aspire.love")
            .then((r) => (r.ok ? r.json() : null))
            .then((data) => {
                const n = data && typeof data.stargazers_count === "number" ? data.stargazers_count : null;
                if (n === null) return;
                starCount.textContent = n >= 1000 ? (n / 1000).toFixed(1) + "k" : String(n);
                starCount.hidden = false;
            })
            .catch(() => {
                /* offline or rate-limited — leave the count hidden */
            });
    }
})();
