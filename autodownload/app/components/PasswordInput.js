"use client";

import { useState } from "react";

function EyeIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z" />
      <circle cx="12" cy="12" r="3" />
    </svg>
  );
}

function EyeOffIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="m3 3 18 18" />
      <path d="M10.73 5.08A10.43 10.43 0 0 1 12 5c6.5 0 10 7 10 7a17.54 17.54 0 0 1-2.01 3.02" />
      <path d="M6.61 6.61C3.75 8.55 2 12 2 12s3.5 7 10 7a9.7 9.7 0 0 0 5.39-1.61" />
      <path d="M9.88 9.88A3 3 0 0 0 14.12 14.12" />
    </svg>
  );
}

export default function PasswordInput({ className = "", inputClassName = "form-input", onReveal, ...props }) {
  const [visible, setVisible] = useState(false);
  const [loadingReveal, setLoadingReveal] = useState(false);
  const classes = ["password-field", className].filter(Boolean).join(" ");

  async function handleToggleVisibility() {
    if (visible) {
      setVisible(false);
      return;
    }

    if (onReveal) {
      setLoadingReveal(true);
      try {
        await onReveal();
      } catch {
        return;
      } finally {
        setLoadingReveal(false);
      }
    }

    setVisible(true);
  }

  return (
    <div className={classes}>
      <input
        {...props}
        className={`${inputClassName} password-field-input`}
        type={visible ? "text" : "password"}
      />
      <button
        type="button"
        className="password-toggle"
        onClick={handleToggleVisibility}
        disabled={loadingReveal}
        aria-label={visible ? "Ocultar senha" : "Mostrar senha"}
        title={visible ? "Ocultar senha" : "Mostrar senha"}
      >
        {visible ? <EyeOffIcon /> : <EyeIcon />}
      </button>
    </div>
  );
}
