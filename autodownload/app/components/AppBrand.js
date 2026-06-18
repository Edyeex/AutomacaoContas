"use client";

export default function AppBrand({ variant = "topbar" }) {
  return (
    <div className={`app-brand app-brand-${variant}`} aria-label="AutoDownload">
      <img className="app-brand-logo" src="/brand-symbol.png" alt="" />
      <span className="app-brand-name">
        <span className="app-brand-auto">Auto</span>
        <span className="app-brand-download">Download</span>
      </span>
    </div>
  );
}
