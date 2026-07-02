"use client";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5080/api";
const AUTH_STORAGE_KEY = "autodownload.auth";

export const AUTH_EXPIRED_EVENT = "autodownload:auth-expired";

let redirectingToLogin = false;

export class ApiError extends Error {
  constructor(message, status, code) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

export function getSession() {
  if (typeof window === "undefined") return null;

  const raw = window.localStorage.getItem(AUTH_STORAGE_KEY);
  if (!raw) return null;

  try {
    return JSON.parse(raw);
  } catch {
    window.localStorage.removeItem(AUTH_STORAGE_KEY);
    return null;
  }
}

export function saveSession(auth) {
  if (typeof window === "undefined") return;
  redirectingToLogin = false;
  window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(auth));
}

export function clearSession() {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(AUTH_STORAGE_KEY);
}

function handleExpiredSession() {
  clearSession();

  if (typeof window === "undefined") return;

  window.dispatchEvent(new CustomEvent(AUTH_EXPIRED_EVENT));

  const publicPaths = ["/", "/cadastro", "/recuperar-senha"];
  const currentPath = window.location.pathname;
  const isPublicPath = publicPaths.includes(currentPath);

  if (!isPublicPath && !redirectingToLogin) {
    redirectingToLogin = true;
    window.location.replace("/");
  }
}

export async function apiRequest(path, options = {}) {
  const session = getSession();
  const auth = options.auth !== false;
  const controller = options.timeoutMs ? new AbortController() : null;
  const timeoutId = controller
    ? window.setTimeout(() => controller.abort(), options.timeoutMs)
    : null;
  const headers = {
    Accept: "application/json",
    ...(options.body ? { "Content-Type": "application/json" } : {}),
    ...(options.headers || {}),
  };

  if (auth) {
    if (!session?.accessToken) {
      handleExpiredSession();
      throw new ApiError("Sessão expirada. Entre novamente.", 401, "auth.required");
    }

    headers.Authorization = `Bearer ${session.accessToken}`;
  }

  let response;

  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      method: options.method || "GET",
      headers,
      body: options.body ? JSON.stringify(options.body) : undefined,
      signal: options.signal || controller?.signal,
    });
  } catch (err) {
    if (err?.name === "AbortError") {
      throw new ApiError(
        "A operação demorou mais do que o esperado. Confira o histórico e as notificações em instantes.",
        408,
        "request.timeout"
      );
    }

    throw new ApiError(
      "Não foi possível conectar ao servidor agora. Verifique se a API está ativa e tente novamente.",
      0,
      "network.error"
    );
  } finally {
    if (timeoutId) {
      window.clearTimeout(timeoutId);
    }
  }

  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get("content-type") || "";
  const payload = contentType.includes("application/json") ? await response.json() : null;

  if (!response.ok) {
    if (auth && response.status === 401) {
      handleExpiredSession();
    }

    throw new ApiError(
      problemMessage(payload, "Não foi possível concluir a operação."),
      response.status,
      payload?.code || payload?.title
    );
  }

  return payload;
}

export async function apiDownload(path, fallbackFileName = "download.pdf") {
  const session = getSession();
  if (!session?.accessToken) {
    handleExpiredSession();
    throw new ApiError("Sessão expirada. Entre novamente.", 401, "auth.required");
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${session.accessToken}`,
    },
  });

  if (!response.ok) {
    const contentType = response.headers.get("content-type") || "";
    const payload = contentType.includes("application/json") ? await response.json() : null;

    if (response.status === 401) {
      handleExpiredSession();
    }

    throw new ApiError(
      problemMessage(payload, "Não foi possível baixar o arquivo."),
      response.status,
      payload?.code || payload?.title
    );
  }

  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filenameFromContentDisposition(response.headers.get("content-disposition")) || fallbackFileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

function problemMessage(payload, fallback) {
  if (!payload) return fallback;

  if (payload.detail || payload.title) {
    return payload.detail || payload.title;
  }

  if (payload.errors && typeof payload.errors === "object") {
    const messages = Object.values(payload.errors).flat().filter(Boolean);
    if (messages.length > 0) {
      return messages[0];
    }
  }

  return fallback;
}

function filenameFromContentDisposition(contentDisposition) {
  if (!contentDisposition) return null;

  const match = contentDisposition.match(/filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i);
  return match ? decodeURIComponent(match[1] || match[2]) : null;
}
