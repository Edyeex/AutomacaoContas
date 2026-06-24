"use client";
import { apiRequest } from "./apiClient";

export const DASHBOARD_COUNTS_CHANGED_EVENT = "autodownload:dashboard-counts-changed";

export function fallbackDashboardCounts() {
  return {
    totalContas: 0,
    totalBoletos: 0,
  };
}

export function publishDashboardCountsChanged() {
  if (typeof window === "undefined") return;
  window.dispatchEvent(new CustomEvent(DASHBOARD_COUNTS_CHANGED_EVENT));
}

export async function fetchDashboardCounts() {
  const dashboard = await apiRequest("/dashboard");
  return {
    totalContas: Number(dashboard.totalContas) || 0,
    totalBoletos: Number(dashboard.totalBoletos) || 0,
  };
}
