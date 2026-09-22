(() => {
    const breadcrumbs = document.querySelector("[data-history-breadcrumbs]");
    const current = {
        path: `${window.location.pathname}${window.location.search}${window.location.hash}`,
        title: document.title.replace(/\s+-\s+EcoGoodz$/, "").trim() || "Previous page",
    };

    try {
        const key = "ecogoodz.navigationHistory";
        const stored = JSON.parse(window.sessionStorage.getItem(key) || "[]");
        const history = Array.isArray(stored) ? stored : [];
        const nextHistory = [...history.filter((entry) => entry?.path !== current.path), current].slice(-16);

        if (breadcrumbs && nextHistory.length > 1) {
            const visibleTrail = nextHistory.slice(-5);
            breadcrumbs.replaceChildren();

            if (nextHistory.length > visibleTrail.length) {
                const ellipsis = document.createElement("li");
                ellipsis.className = "breadcrumb-item text-secondary";
                ellipsis.textContent = "...";
                breadcrumbs.append(ellipsis);
            }

            visibleTrail.forEach((entry, index) => {
                const item = document.createElement("li");
                item.className = "breadcrumb-item";
                const isCurrent = index === visibleTrail.length - 1;

                if (isCurrent) {
                    item.classList.add("active");
                    item.setAttribute("aria-current", "page");
                    item.textContent = entry.title || "Current page";
                } else {
                    const link = document.createElement("a");
                    link.href = entry.path;
                    link.textContent = entry.title || "Previous page";
                    item.append(link);
                }

                breadcrumbs.append(item);
            });
        }

        window.sessionStorage.setItem(key, JSON.stringify(nextHistory));
    } catch {
        // Keep the server-rendered hierarchy breadcrumbs if session history is unavailable.
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
