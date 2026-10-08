(() => {
    let mode = "system";
    try {
        const storage = window.localStorage;
        const newThemeKey = "fluentui-blazor:theme-settings";
        let theme = storage.getItem(newThemeKey);
        if (!theme) {
            const oldTheme = storage.getItem("theme");
            if (oldTheme) {
                const legacySettings = JSON.parse(oldTheme);
                const legacyColor = legacySettings.primaryColor;
                const color = typeof legacyColor === "string" && /^#?[0-9a-f]{6}$/i.test(legacyColor)
                    ? (legacyColor.startsWith("#") ? legacyColor : `#${legacyColor}`)
                    : "#0f6cbd";
                const migratedSettings = {
                    color,
                    hue: 0,
                    vibrancy: 0,
                    exact: false,
                    base: "brand"
                };
                if (legacySettings.mode === "light" || legacySettings.mode === "dark") {
                    migratedSettings.mode = legacySettings.mode;
                }
                theme = JSON.stringify(migratedSettings);
                storage.setItem(newThemeKey, theme);
            }
        }

        if (theme) {
            const settings = JSON.parse(theme);
            if (settings.mode === "dark" || settings.mode === "light") {
                mode = settings.mode;
            } else {
                mode = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
            }
        } else {
            mode = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
        }
    } catch (error) {
        console.warn("Could not read Fluent UI theme settings before startup.", error);
    }

    if (mode === "system") {
        mode = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }
    document.documentElement.dataset.themeMode = mode;
    document.documentElement.style.colorScheme = mode;
})();
