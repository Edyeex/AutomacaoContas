"use client";
import { boletos, contas } from "./mockData";
import { apiRequest } from "./apiClient";

export const DASHBOARD_COUNTS_CHANGED_EVENT = "autodownload:dashboard-counts-changed";

export function fallbackDashboardCounts() {
  return {
    totalContas: contas.length,
    totalBoletos: boletos.length,
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
