"use client";

const THEME_STORAGE_KEY = "autodownload.theme";
const DEFAULT_THEME = "light";

export function getStoredTheme() {
  if (typeof window === "undefined") return DEFAULT_THEME;

  const storedTheme = window.localStorage.getItem(THEME_STORAGE_KEY);
  return storedTheme === "dark" ? "dark" : DEFAULT_THEME;
}

export function applyTheme(theme) {
  if (typeof document === "undefined") return;

  document.documentElement.dataset.theme = theme === "dark" ? "dark" : DEFAULT_THEME;
}

export function saveTheme(theme) {
  if (typeof window === "undefined") return;

  const normalizedTheme = theme === "dark" ? "dark" : DEFAULT_THEME;
  window.localStorage.setItem(THEME_STORAGE_KEY, normalizedTheme);
  applyTheme(normalizedTheme);
}
