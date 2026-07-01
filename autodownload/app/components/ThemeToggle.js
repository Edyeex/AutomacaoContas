"use client";

import { useEffect, useState } from "react";
import { applyTheme, getStoredTheme, saveTheme } from "../lib/themeState";

function SunIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2v2" />
      <path d="M12 20v2" />
      <path d="m4.93 4.93 1.41 1.41" />
      <path d="m17.66 17.66 1.41 1.41" />
      <path d="M2 12h2" />
      <path d="M20 12h2" />
      <path d="m6.34 17.66-1.41 1.41" />
      <path d="m19.07 4.93-1.41 1.41" />
    </svg>
  );
}

function MoonIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M20.99 13.78A8.5 8.5 0 1 1 10.22 3.01 7 7 0 0 0 20.99 13.78Z" />
    </svg>
  );
}

export default function ThemeToggle({ className = "", showText = false }) {
  const [theme, setTheme] = useState("light");

  useEffect(() => {
    const storedTheme = getStoredTheme();
    setTheme(storedTheme);
    applyTheme(storedTheme);
  }, []);

  const isDark = theme === "dark";
  const nextTheme = isDark ? "light" : "dark";
  const currentLabel = isDark ? "Modo escuro" : "Modo claro";
  const actionLabel = isDark ? "Alternar para modo claro" : "Alternar para modo escuro";
  const classes = ["theme-toggle", isDark ? "theme-toggle-dark" : "theme-toggle-light", className]
    .filter(Boolean)
    .join(" ");

  function toggleTheme() {
    setTheme(nextTheme);
    saveTheme(nextTheme);
  }

  return (
    <button
      type="button"
      className={classes}
      onClick={toggleTheme}
      aria-label={`${currentLabel}. ${actionLabel}.`}
      title={actionLabel}
    >
      <span className="theme-toggle-icon">{isDark ? <MoonIcon /> : <SunIcon />}</span>
      {showText && <span>{currentLabel}</span>}
    </button>
  );
}
