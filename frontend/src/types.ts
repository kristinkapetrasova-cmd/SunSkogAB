export type TimesheetStatus = "Draft" | "Submitted" | "Approved" | string;

export interface Timesheet {
  id: string;
  employeeId: string;
  periodStart: string; // yyyy-MM-dd
  periodEnd: string;   // yyyy-MM-dd
  status: TimesheetStatus;
  totalHours: number;
  totalKm: number;
  totalPieces: number;
  totalPay: number;
  submittedAt?: string | null;
  approvedAt?: string | null;
}

export interface TimesheetEntry {
  id: string;
  workDate: string; // yyyy-MM-dd
  project: string;
  task: string;
  hours: number;
  km: number;
  pieces: number;
  comment?: string | null;
}

export interface Paged<T> {
  items: T[];
}

export interface ApiError {
  error?: string;
  title?: string;
  detail?: string;
}
