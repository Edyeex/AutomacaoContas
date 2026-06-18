"use client";
import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import AppBrand from "./AppBrand";
import { currentUser } from "../lib/mockData";
import { clearSession, getSession } from "../lib/apiClient";
import { applyTheme, getStoredTheme, saveTheme } from "../lib/themeState";
import {
  fallbackUnreadCount,
  fetchUnreadCount,
  NOTIFICATIONS_CHANGED_EVENT,
} from "../lib/notificationState";

const accountMenuItems = [
  {
    href: "/dashboard",
    label: "Visão geral",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <rect x="3" y="3" width="7" height="7" />
        <rect x="14" y="3" width="7" height="7" />
        <rect x="3" y="14" width="7" height="7" />
        <rect x="14" y="14" width="7" height="7" />
      </svg>
    ),
  },
  {
    href: "/dashboard/contas",
    label: "Contas",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
        <circle cx="12" cy="7" r="4" />
      </svg>
    ),
  },
  {
    href: "/dashboard/boletos",
    label: "Boletos",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
        <polyline points="14 2 14 8 20 8" />
        <line x1="16" y1="13" x2="8" y2="13" />
        <line x1="16" y1="17" x2="8" y2="17" />
      </svg>
    ),
  },
  {
    href: "/dashboard/historico",
    label: "Histórico",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="12" cy="12" r="10" />
        <polyline points="12 6 12 12 16 14" />
      </svg>
    ),
  },
  {
    href: "/dashboard/notificacoes",
    label: "Notificações",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
        <path d="M13.73 21a2 2 0 0 1-3.46 0" />
      </svg>
    ),
  },
  {
    href: "/dashboard/configuracoes",
    label: "Configurações",
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="12" cy="12" r="3" />
        <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" />
      </svg>
    ),
  },
];

export default function TopBar() {
  const router = useRouter();
  const menuRef = useRef(null);
  const [user, setUser] = useState(currentUser);
  const [unreadCount, setUnreadCount] = useState(fallbackUnreadCount());
  const [menuOpen, setMenuOpen] = useState(false);
  const [theme, setTheme] = useState("light");

  useEffect(() => {
    const session = getSession();
    if (session?.user) {
      setUser(session.user);
    }

    const storedTheme = getStoredTheme();
    setTheme(storedTheme);
    applyTheme(storedTheme);

    async function loadUnreadCount() {
      try {
        setUnreadCount(await fetchUnreadCount());
      } catch {
        setUnreadCount(fallbackUnreadCount());
      }
    }

    function handleNotificationsChanged(event) {
      setUnreadCount(event.detail?.unreadCount ?? 0);
    }

    loadUnreadCount();
    window.addEventListener(NOTIFICATIONS_CHANGED_EVENT, handleNotificationsChanged);
    return () => window.removeEventListener(NOTIFICATIONS_CHANGED_EVENT, handleNotificationsChanged);
  }, []);

  useEffect(() => {
    if (!menuOpen) return;

    function handleDocumentClick(event) {
      if (menuRef.current && !menuRef.current.contains(event.target)) {
        setMenuOpen(false);
      }
    }

    function handleKeyDown(event) {
      if (event.key === "Escape") {
        setMenuOpen(false);
      }
    }

    document.addEventListener("mousedown", handleDocumentClick);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("mousedown", handleDocumentClick);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [menuOpen]);

  function changeTheme(nextTheme) {
    setTheme(nextTheme);
    saveTheme(nextTheme);
  }

  function handleLogout() {
    clearSession();
    setMenuOpen(false);
    router.push("/");
  }

  const displayName = user?.name || "Usuário";
  const displayEmail = user?.email || "";
  const initials = user?.initials || displayName
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();
  const firstName = displayName.split(" ")[0];

  return (
    <header className="topbar">
      <div className="topbar-left">
        <AppBrand variant="topbar" />
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
          {unreadCount > 0 && <span className="topbar-badge">{unreadCount}</span>}
        </button>

        <div className="topbar-user-shell" ref={menuRef}>
          <button
            className={`topbar-user${menuOpen ? " active" : ""}`}
            onClick={() => setMenuOpen((isOpen) => !isOpen)}
            aria-haspopup="menu"
            aria-expanded={menuOpen}
          >
            <div className="user-avatar">
              {initials}
            </div>
            <span className="topbar-user-name">{firstName}</span>
            <svg className="topbar-user-chevron" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="6 9 12 15 18 9" />
            </svg>
          </button>

          {menuOpen && (
            <div className="account-menu" role="menu">
              <div className="account-menu-header">
                <div className="user-avatar user-avatar-lg">
                  {initials}
                </div>
                <div>
                  <strong>{displayName}</strong>
                  {displayEmail && <span>{displayEmail}</span>}
                </div>
              </div>

              <div className="account-menu-section">
                {accountMenuItems.map((item) => (
                  <Link
                    key={item.href}
                    href={item.href}
                    className="account-menu-item"
                    role="menuitem"
                    onClick={() => setMenuOpen(false)}
                  >
                    {item.icon}
                    <span>{item.label}</span>
                  </Link>
                ))}
              </div>

              <div className="account-menu-section">
                <div className="account-menu-label">Tema do site</div>
                <div className="theme-switch" role="group" aria-label="Tema do site">
                  <button
                    type="button"
                    className={theme === "light" ? "active" : ""}
                    onClick={() => changeTheme("light")}
                  >
                    Modo claro
                  </button>
                  <button
                    type="button"
                    className={theme === "dark" ? "active" : ""}
                    onClick={() => changeTheme("dark")}
                  >
                    Modo escuro
                  </button>
                </div>
              </div>

              <div className="account-menu-section">
                <button type="button" className="account-menu-item account-menu-button" onClick={handleLogout}>
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                    <polyline points="16 17 21 12 16 7" />
                    <line x1="21" y1="12" x2="9" y2="12" />
                  </svg>
                  <span>Sair</span>
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
