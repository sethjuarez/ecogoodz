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
