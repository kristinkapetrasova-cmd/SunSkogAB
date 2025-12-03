export type UserRole =
  | "Employee"
  | "TeamLead"
  | "Accountant"
  | "Management"
  | "Warehouse"
  | "Admin";

export interface UserDto {
  id: string;
  email: string;
  name: string;
  roles: string[];
  isLockedOut: boolean;
  teamId?: string | null;
  teamName?: string | null;
  teamRole?: string | null;
}

export interface CreateUserPayload {
  email: string;
  password: string;
  name?: string;
  roles: UserRole[];
}

export interface UpdateUserPayload {
  name?: string;
  roles?: UserRole[];
  lockout?: boolean;
  teamId?: string | null;
  teamRole?: string | null;
}

// ⬇️ stejný backend jako v LoginPage
const API_BASE = "http://localhost:5250";

async function apiRequest<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const url = `${API_BASE}${path}`;

  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(options.headers || {}),
  };

  const token =
    localStorage.getItem("authToken") ||
    localStorage.getItem("token") ||
    localStorage.getItem("jwt");

  if (token) {
    (headers as any).Authorization = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!response.ok) {
    let message = `HTTP ${response.status}`;
    try {
      const data = await response.json();
      if (typeof data === "object" && data && "error" in data) {
        message = (data as any).error;
      } else if (typeof data === "object" && data && "message" in data) {
        message = (data as any).message;
      }
    } catch {
      // ignore
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as unknown as T;
  }

  return (await response.json()) as T;
}

// veřejné funkce

export async function getUsers(): Promise<UserDto[]> {
  return apiRequest<UserDto[]>("/api/users", { method: "GET" });
}

export async function createUser(payload: CreateUserPayload): Promise<void> {
  await apiRequest<unknown>("/api/users", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function updateUser(
  id: string,
  payload: UpdateUserPayload
): Promise<void> {
  await apiRequest<unknown>(`/api/users/${id}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function resetUserPassword(
  id: string,
  newPassword: string
): Promise<void> {
  await apiRequest<unknown>(`/api/users/${id}/reset-password`, {
    method: "POST",
    body: JSON.stringify({ newPassword }),
  });
}