// src/lib/client.ts
export type ApiError = {
  status: number;
  message: string;
  details?: unknown;
};

const BASE_URL =
  (typeof import.meta !== "undefined" &&
    (
      // podporujeme víc názvů, ale to důležité je VITE_API_BASE_URL
      (import.meta as any).env?.VITE_API_BASE_URL ||
      (import.meta as any).env?.VITE_API_URL ||
      (import.meta as any).env?.VITE_API_BASE
    )) ||
  (typeof window !== "undefined"
    ? `${window.location.origin.replace(/\/+$/, "")}`
    : "");

// jednoduché uložiště tokenu
export const authToken = {
  get(): string | null {
    try {
      return localStorage.getItem("auth/token");
    } catch {
      return null;
    }
  },
  set(token: string | null) {
    try {
      if (token) localStorage.setItem("auth/token", token);
      else localStorage.removeItem("auth/token");
    } catch {
      // ignore
    }
  },
};

type RequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  query?: Record<string, string | number | boolean | undefined | null>;
  body?: unknown;
  headers?: Record<string, string>;
  signal?: AbortSignal;
};

function buildUrl(path: string, query?: RequestOptions["query"]) {
  const base = BASE_URL || "";
  const url = new URL(
    path.replace(/^\/+/, ""),
    base.endsWith("/") ? base : `${base}/`
  );
  if (query) {
    Object.entries(query).forEach(([k, v]) => {
      if (v !== undefined && v !== null) url.searchParams.set(k, String(v));
    });
  }
  return url.toString();
}

export async function request<T>(
  path: string,
  opts: RequestOptions = {}
): Promise<T> {
  const { method = "GET", query, body, headers, signal } = opts;

  const token = authToken.get();
  const finalHeaders: Record<string, string> = {
    Accept: "application/json",
    ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
    ...(headers || {}),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };

  const res = await fetch(buildUrl(path, query), {
    method,
    headers: finalHeaders,
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  });

  // 204 No Content
  if (res.status === 204) {
    return undefined as unknown as T;
  }

  let data: unknown;
  const text = await res.text();
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      // odpověď není JSON (např. HTML), necháme jako raw text
      data = text;
    }
  }

  if (!res.ok) {
    const err: ApiError = {
      status: res.status,
      message:
        (typeof data === "object" &&
          data &&
          "message" in data &&
          (data as any).message) ||
        res.statusText ||
        "Request failed",
      details: data,
    };
    throw err;
  }

  return data as T;
}

// pohodlné helpery
export const http = {
  get: <T>(
    path: string,
    query?: RequestOptions["query"],
    init?: Omit<RequestOptions, "method" | "query">
  ) => request<T>(path, { ...init, method: "GET", query }),

  post: <T>(
    path: string,
    body?: unknown,
    init?: Omit<RequestOptions, "method" | "body">
  ) => request<T>(path, { ...init, method: "POST", body }),

  put: <T>(
    path: string,
    body?: unknown,
    init?: Omit<RequestOptions, "method" | "body">
  ) => request<T>(path, { ...init, method: "PUT", body }),

  patch: <T>(
    path: string,
    body?: unknown,
    init?: Omit<RequestOptions, "method" | "body">
  ) => request<T>(path, { ...init, method: "PATCH", body }),

  delete: <T>(
    path: string,
    init?: Omit<RequestOptions, "method">
  ) => request<T>(path, { ...init, method: "DELETE" }),
};