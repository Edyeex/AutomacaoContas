"use client";
import { useEffect, useRef, useState } from "react";
import { notificacoes as initialNotificacoes } from "../../lib/mockData";
import { apiRequest } from "../../lib/apiClient";
import { useApiResource } from "../../lib/useApiResource";
import { publishUnreadCount } from "../../lib/notificationState";

function timeAgo(dateStr) {
  const now = new Date();
  const d = new Date(dateStr);
  const diff = Math.max(0, Math.floor((now - d) / 1000));
  if (diff < 60) return "agora";
  if (diff < 3600) return `${Math.floor(diff / 60)}min atrás`;
  if (diff < 86400) return `${Math.floor(diff / 3600)}h atrás`;
  return `${Math.floor(diff / 86400)}d atrás`;
}

function unreadCountFrom(list) {
  return list.filter((item) => !item.lida).length;
}

export default function NotificacoesPage() {
  const { data, loading, error, usingFallback, reload } = useApiResource("/notifications", initialNotificacoes);
  const [notificacoes, setNotificacoes] = useState(initialNotificacoes);
  const [pageMessage, setPageMessage] = useState("");
  const autoMarkedRead = useRef(false);

  useEffect(() => {
    setNotificacoes(data || []);
  }, [data]);

  useEffect(() => {
    if (loading || autoMarkedRead.current) return;

    const unreadCount = unreadCountFrom(data || []);
    if (unreadCount === 0) {
      publishUnreadCount(0);
      return;
    }

    autoMarkedRead.current = true;
    markAllRead();
  }, [data, loading]);

  async function markAllRead() {
    setPageMessage("");

    if (usingFallback) {
      setNotificacoes((prev) => prev.map((n) => ({ ...n, lida: true })));
      publishUnreadCount(0);
      return;
    }

    try {
      await apiRequest("/notifications/mark-all-read", { method: "PATCH" });
      setNotificacoes((prev) => prev.map((n) => ({ ...n, lida: true })));
      publishUnreadCount(0);
      await reload();
    } catch (err) {
      setPageMessage(err.message || "Não foi possível atualizar as notificações.");
    }
  }

  async function toggleRead(id) {
    setPageMessage("");
    const item = notificacoes.find((n) => n.id === id);
    if (!item) return;

    const nextList = notificacoes.map((n) => (n.id === id ? { ...n, lida: !n.lida } : n));

    if (usingFallback) {
      setNotificacoes(nextList);
      publishUnreadCount(unreadCountFrom(nextList));
      return;
    }

    try {
      await apiRequest(`/notifications/${id}/read`, {
        method: "PATCH",
        body: { read: !item.lida },
      });
      setNotificacoes(nextList);
      publishUnreadCount(unreadCountFrom(nextList));
      await reload();
    } catch (err) {
      setPageMessage(err.message || "Não foi possível atualizar a notificação.");
    }
  }

  async function removeNotification(id) {
    setPageMessage("");
    const nextList = notificacoes.filter((n) => n.id !== id);

    if (usingFallback) {
      setNotificacoes(nextList);
      publishUnreadCount(unreadCountFrom(nextList));
      return;
    }

    try {
      await apiRequest(`/notifications/${id}`, { method: "DELETE" });
      setNotificacoes(nextList);
      publishUnreadCount(unreadCountFrom(nextList));
      await reload();
    } catch (err) {
      setPageMessage(err.message || "Nao foi possivel remover a notificacao.");
    }
  }

  async function clearNotifications() {
    setPageMessage("");

    if (usingFallback) {
      setNotificacoes([]);
      publishUnreadCount(0);
      return;
    }

    try {
      await apiRequest("/notifications", { method: "DELETE" });
      setNotificacoes([]);
      publishUnreadCount(0);
      await reload();
    } catch (err) {
      setPageMessage(err.message || "Nao foi possivel limpar as notificacoes.");
    }
  }

  const unreadCount = unreadCountFrom(notificacoes);

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
          {notificacoes.length > 0 && (
            <button className="btn btn-danger btn-sm" onClick={clearNotifications}>
              Limpar todas
            </button>
          )}
        </div>
      </div>

      <div className="page-body">
        {(loading || error || pageMessage) && (
          <p style={{ fontSize: 13, color: error || pageMessage ? "var(--warning)" : "var(--text-muted)", marginBottom: 12 }}>
            {loading ? "Carregando notificações..." : pageMessage || "API indisponível; exibindo dados do protótipo."}
          </p>
        )}

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
                  <button
                    className="btn btn-danger btn-sm"
                    onClick={(event) => {
                      event.stopPropagation();
                      removeNotification(n.id);
                    }}
                  >
                    Remover
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </>
  );
}
