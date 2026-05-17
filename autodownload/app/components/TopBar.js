"use client";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { currentUser } from "../lib/mockData";

export default function TopBar() {
  const router = useRouter();

  return (
    <header className="topbar">
      <div className="topbar-left">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
          <polyline points="7 10 12 15 17 10" />
          <line x1="12" y1="15" x2="12" y2="3" />
        </svg>
        <span>AutoDownload</span>
      </div>

      <div className="topbar-search">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="11" cy="11" r="8" />
          <line x1="21" y1="21" x2="16.65" y2="16.65" />
        </svg>
        <input type="text" placeholder="Buscar boletos, contas..." />
      </div>

      <div className="topbar-right">
        <button
          className="topbar-btn"
          onClick={() => router.push("/dashboard/notificacoes")}
          title="Notificações"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
            <path d="M13.73 21a2 2 0 0 1-3.46 0" />
          </svg>
          <span className="topbar-badge">2</span>
        </button>

        <button
          className="topbar-user"
          onClick={() => router.push("/dashboard/configuracoes")}
        >
          <div className="user-avatar" style={{ width: 32, height: 32, borderRadius: "50%", background: "var(--primary)", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 13, fontWeight: 700, color: "#fff" }}>
            {currentUser.initials}
          </div>
          <span className="topbar-user-name">{currentUser.name.split(" ")[0]}</span>
        </button>
      </div>
    </header>
  );
}
