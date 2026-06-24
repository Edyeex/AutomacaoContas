"use client";
import { apiRequest } from "./apiClient";

export const NOTIFICATIONS_CHANGED_EVENT = "autodownload:notifications-changed";

export function fallbackUnreadCount() {
  return 0;
}

export function publishUnreadCount(count) {
  if (typeof window === "undefined") return;

  window.dispatchEvent(
    new CustomEvent(NOTIFICATIONS_CHANGED_EVENT, {
      detail: { unreadCount: Math.max(0, Number(count) || 0) },
    })
  );
}

export async function fetchUnreadCount() {
  const list = await apiRequest("/notifications");
  return list.filter((item) => !item.lida).length;
}
