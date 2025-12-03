// src/lib/api.ts
import { http, authToken } from "./client";

/** ===== Typy – Timesheets ===== */

export type TimesheetStatus = "Draft" | "Submitted" | "Approved" | "Returned";

export type TimesheetDto = {
  id: string;
  employeeId: string;
  periodStart: string;
  periodEnd: string;
  status: TimesheetStatus;
  totalHours: number;
  totalKm: number;
  totalPieces: number;
  totalPay: number;
  createdAt?: string;
  updatedAt?: string;
};

export type TimesheetEntryDto = {
  id: string;
  workDate: string;
  project?: string | null;
  task?: string | null;
  hours: number;
  km: number;
  pieces: number;
  comment?: string | null;
  entryPay: number;
};

export type CreateTimesheetRequest = {
  periodStart: string;
  periodEnd: string;
};

export type CreateTimesheetEntryRequest = {
  workDate: string;
  project?: string | null;
  task?: string | null;
  hours: number;
  km: number;
  pieces: number;
  comment?: string | null;
};

/** ===== Typy – Auth ===== */

export type UserInfo = {
  id: string;
  email: string;
  name: string;
  roles: string[];
};

export type LoginResponse = {
  token: string;
  user: UserInfo;
};

/** ===== Typy – Inventory ===== */

export type InventoryItem = {
  id: string;
  name: string;
  sku?: string | null;
  serialNumber?: string | null;
  minStock: number;
  isActive: boolean;
};

export type StockMovement = {
  id: string;
  itemId: string;
  type: number;
  quantity: number;
  at?: string;
};

export type CreateInventoryItemRequest = {
  name: string;
  sku?: string | null;
  serialNumber?: string | null;
  minStock?: number;
  isActive?: boolean;
};

export type CreateStockMovementRequest = {
  itemId: string;
  type: number;
  quantity: number;
};

/** ===== Utils (UI pomocníci) ===== */

export function fmtMoney(v: number) {
  return new Intl.NumberFormat("cs-CZ", {
    style: "currency",
    currency: "SEK",
  }).format(v);
}

export function todayYMD(): string {
  const d = new Date();
  const mm = `${d.getMonth() + 1}`.padStart(2, "0");
  const dd = `${d.getDate()}`.padStart(2, "0");
  return `${d.getFullYear()}-${mm}-${dd}`;
}

export function monthRange(d = new Date()): { start: string; end: string } {
  const start = new Date(d.getFullYear(), d.getMonth(), 1);
  const end = new Date(d.getFullYear(), d.getMonth() + 1, 0);
  const s = `${start.getFullYear()}-${String(start.getMonth() + 1).padStart(2, "0")}-${String(start.getDate()).padStart(2, "0")}`;
  const e = `${end.getFullYear()}-${String(end.getMonth() + 1).padStart(2, "0")}-${String(end.getDate()).padStart(2, "0")}`;
  return { start: s, end: e };
}

/** ===== Auth ===== */

export async function login(email: string, password: string): Promise<LoginResponse> {
  const res = await http.post<LoginResponse>("/auth/login", { email, password });
  if (res?.token) {
    authToken.set(res.token);
  }
  return res;
}

export async function me(): Promise<UserInfo> {
  const data = await http.get<{
    id: string;
    email: string;
    name: string;
    roles: string[];
  }>("/auth/me");
  return {
    id: data.id,
    email: data.email,
    name: data.name,
    roles: data.roles ?? [],
  };
}

export function logout() {
  authToken.set(null);
}

/** ===== Moje timesheety ===== */

export async function getTimesheets(): Promise<TimesheetDto[]> {
  return http.get<TimesheetDto[]>("/api/timesheets");
}

export async function getTimesheet(id: string): Promise<TimesheetDto> {
  return http.get<TimesheetDto>(`/api/timesheets/${id}`);
}

export async function createTimesheet(payload: CreateTimesheetRequest): Promise<TimesheetDto> {
  return http.post<TimesheetDto>("/api/timesheets", payload);
}

export async function submitTimesheet(id: string): Promise<void> {
  await http.post<void>(`/api/timesheets/${id}/submit`, {});
}

/** ===== Timesheet entries ===== */

export async function getTimesheetEntries(timesheetId: string): Promise<TimesheetEntryDto[]> {
  return http.get<TimesheetEntryDto[]>(`/api/timesheets/${timesheetId}/entries`);
}

export async function createTimesheetEntry(
  timesheetId: string,
  payload: CreateTimesheetEntryRequest
): Promise<TimesheetEntryDto> {
  return http.post<TimesheetEntryDto>(`/api/timesheets/${timesheetId}/entries`, payload);
}

export async function deleteTimesheetEntry(timesheetId: string, entryId: string): Promise<void> {
  await http.delete<void>(`/api/timesheets/${timesheetId}/entries/${entryId}`);
}

/** ===== Admin: přehled výkazů ===== */

export async function getAdminTimesheets(): Promise<TimesheetDto[]> {
  const response = await http.get<{ page: number; pageSize: number; total: number; items: TimesheetDto[] }>("/api/admin/timesheets");
  return response.items || [];
}

/** ===== Workflow (admin) ===== */

export async function approveTimesheet(id: string): Promise<void> {
  await http.post<void>(`/api/timesheets/${id}/approve`, {});
}

export async function returnTimesheet(id: string, reason?: string): Promise<void> {
  await http.post<void>(`/api/timesheets/${id}/return`, {
    reason: reason || "Vráceno k doplnění.",
  });
}

/** ===== Inventory ===== */

export async function listInventoryItems(q?: string): Promise<InventoryItem[]> {
  return http.get<InventoryItem[]>("/api/inventory/items", q ? { q } : undefined);
}

export async function createInventoryItem(payload: CreateInventoryItemRequest): Promise<InventoryItem> {
  return http.post<InventoryItem>("/api/inventory/items", payload);
}

export async function updateInventoryItem(id: string, body: Partial<InventoryItem>): Promise<InventoryItem> {
  return http.put<InventoryItem>(`/api/inventory/items/${id}`, body);
}

export async function deleteInventoryItem(id: string): Promise<void> {
  await http.delete<void>(`/api/inventory/items/${id}`);
}

export async function listStockMovements(itemId: string): Promise<StockMovement[]> {
  return http.get<StockMovement[]>("/api/inventory/movements", { itemId });
}

export async function createStockMovement(payload: CreateStockMovementRequest): Promise<StockMovement> {
  return http.post<StockMovement>("/api/inventory/movements", payload);
}

export async function listLowStock(): Promise<{ id: string; name: string; sku?: string | null; current: number; min: number }[]> {
  return http.get("/api/inventory/low-stock");
}