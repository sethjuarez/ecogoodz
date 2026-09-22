(() => {
    const historyBack = document.querySelector("[data-history-back]");
    const current = {
        path: `${window.location.pathname}${window.location.search}${window.location.hash}`,
        title: document.title.replace(/\s+-\s+EcoGoodz$/, "").trim() || "Previous page",
    };

    try {
        const key = "ecogoodz.navigationHistory";
        const stored = JSON.parse(window.sessionStorage.getItem(key) || "[]");
        const history = Array.isArray(stored) ? stored : [];
        const previous = [...history].reverse().find((entry) => entry?.path && entry.path !== current.path);

        if (historyBack && previous) {
            historyBack.textContent = `Back to ${previous.title || "previous page"}`;
            historyBack.setAttribute("href", previous.path);
            historyBack.hidden = false;
            historyBack.addEventListener("click", (event) => {
                event.preventDefault();
                window.location.assign(previous.path);
            });
        }

        const nextHistory = [...history.filter((entry) => entry?.path !== current.path), current].slice(-12);
        window.sessionStorage.setItem(key, JSON.stringify(nextHistory));
    } catch {
        if (historyBack) {
            historyBack.hidden = true;
        }
    }
})();

(() => {
    if (!window.TomSelect) {
        return;
    }

    document.querySelectorAll("select[data-searchable-select='true']").forEach((select) => {
        if (select.tomselect) {
            return;
        }

        const searchUrl = select.dataset.searchUrl;
        const placeholderOption = select.querySelector("option[value='']");
        const placeholder = placeholderOption?.textContent?.trim() ?? "";

        new TomSelect(select, {
            create: false,
            maxOptions: 100,
            placeholder,
            plugins: ["dropdown_input"],
            valueField: "value",
            labelField: "text",
            searchField: "text",
            preload: searchUrl ? "focus" : false,
            loadThrottle: 250,
            load: searchUrl
                ? (query, callback) => {
                    const url = new URL(searchUrl, window.location.origin);
                    if (query) {
                        url.searchParams.set("q", query);
                    }

                    fetch(url)
                        .then((response) => response.ok ? response.json() : [])
                        .then(callback)
                        .catch(() => callback());
                }
                : null,
            sortField: {
                field: "text",
                direction: "asc",
            },
        });
    });
})();
