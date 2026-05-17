"use client";
import { useState } from "react";
import { notificacoes as initialNotificacoes } from "../../lib/mockData";

function timeAgo(dateStr) {
  const now = new Date("2025-04-29T12:00:00");
  const d = new Date(dateStr);
  const diff = Math.floor((now - d) / 1000);
  if (diff < 60) return "agora";
  if (diff < 3600) return `${Math.floor(diff / 60)}min atrás`;
  if (diff < 86400) return `${Math.floor(diff / 3600)}h atrás`;
  return `${Math.floor(diff / 86400)}d atrás`;
}

export default function NotificacoesPage() {
  const [notificacoes, setNotificacoes] = useState(initialNotificacoes);

  function markAllRead() {
    setNotificacoes((prev) => prev.map((n) => ({ ...n, lida: true })));
  }

  function toggleRead(id) {
    setNotificacoes((prev) =>
      prev.map((n) => (n.id === id ? { ...n, lida: !n.lida } : n))
    );
  }

  const unreadCount = notificacoes.filter((n) => !n.lida).length;

  return (
    <>
      <div className="page-header">
        <h1>Notificações</h1>
        <div className="page-header-actions">
          {unreadCount > 0 && (
            <button className="btn btn-secondary btn-sm" onClick={markAllRead}>
              Marcar todas como lidas
            </button>
          )}
        </div>
      </div>

      <div className="page-body">
        <div className="card">
          {notificacoes.length === 0 ? (
            <div className="empty-state">
              <p>Nenhuma notificação.</p>
            </div>
          ) : (
            <ul className="notification-list">
              {notificacoes.map((n) => (
                <li
                  key={n.id}
                  className="notification-item"
                  onClick={() => toggleRead(n.id)}
                  style={{ cursor: "pointer" }}
                >
                  <span className={`notification-dot ${n.lida ? "read" : "unread"}`} />
                  <div className="notification-text">
                    <p>{n.texto}</p>
                    <time>{timeAgo(n.data)}</time>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </>
  );
}
