// src/lib/auth.ts
const KEY = "auth_token";

export function saveToken(token: string) {
  localStorage.setItem(KEY, token);
}

export function getToken(): string | null {
  return localStorage.getItem(KEY);
}

export function logout() {
  localStorage.removeItem(KEY);
}