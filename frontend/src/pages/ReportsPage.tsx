import React, { useEffect, useMemo, useState } from "react";

const API_BASE =
  (import.meta as any).env?.VITE_API_BASE?.toString() || "http://localhost:5250";

type SummaryDto = {
  currency: string;
  range: { from: string; to: string };
  totals: { hours: number; km: number; pieces: number; pay: number };
  timesheetsByStatus: { status: string; count: number }[];
};

function getToken() {
  return localStorage.getItem("auth/token") || "";
}

function fmt(n: number, digits = 2) {
  return new Intl.NumberFormat("cs-CZ", { minimumFractionDigits: digits, maximumFractionDigits: digits }).format(n);
}

export default function ReportsPage() {
  const todayIso = useMemo(() => new Date().toISOString().slice(0, 10), []);
  const firstOfMonthIso = useMemo(() => {
    const d = new Date();
    d.setDate(1);
    return d.toISOString().slice(0, 10);
  }, []);

  const [from, setFrom] = useState(firstOfMonthIso);
  const [to, setTo] = useState(todayIso);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [data, setData] = useState<SummaryDto | null>(null);

  async function load() {
    setErr(null);
    setLoading(true);
    try {
      const res = await fetch(`${API_BASE}/api/admin/reports/summary?from=${from}&to=${to}`, {
        headers: { Authorization: `Bearer ${getToken()}` },
      });
      if (!res.ok) {
        let detail = "";
        try { const j = await res.json(); detail = j.detail || j.title || ""; } catch {}
        throw new Error(detail || `Chyba (HTTP ${res.status})`);
      }
      const j = (await res.json()) as SummaryDto;
      setData(j);
    } catch (e: any) {
      setErr(e.message || "Nepodařilo se načíst data.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="space-y-4">
      <div className="rounded-xl bg-white p-5 shadow">
        <h2 className="mb-4 text-lg font-semibold">Reporty – Souhrn za období</h2>

        <div className="flex flex-wrap items-end gap-3">
          <label className="text-sm">
            Od
            <input
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              className="ml-2 rounded border border-gray-300 px-2 py-1"
            />
          </label>
          <label className="text-sm">
            Do
            <input
              type="date"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              className="ml-2 rounded border border-gray-300 px-2 py-1"
            />
          </label>
          <button
            onClick={load}
            disabled={loading}
            className="rounded bg-blue-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {loading ? "Načítám…" : "Načíst"}
          </button>
        </div>
      </div>

      {err && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 border border-red-200">
          {err}
        </div>
      )}

      {data && (
        <>
          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <StatCard title="Hodiny" value={fmt(data.totals.hours, 2)} />
            <StatCard title="Km" value={fmt(data.totals.km, 1)} />
            <StatCard title="Kusy" value={fmt(data.totals.pieces, 0)} />
            <StatCard title={`Odměny (${data.currency})`} value={fmt(data.totals.pay, 2)} />
          </div>

          <div className="rounded-xl bg-white p-5 shadow">
            <h3 className="mb-3 font-medium">Počty výkazů podle stavu</h3>
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-gray-500 border-b">
                  <th className="py-2 pr-4">Status</th>
                  <th className="py-2">Počet</th>
                </tr>
              </thead>
              <tbody>
                {data.timesheetsByStatus.map((x) => (
                  <tr key={x.status} className="border-b last:border-none">
                    <td className="py-2 pr-4">{x.status}</td>
                    <td className="py-2">{x.count}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}

function StatCard({ title, value }: { title: string; value: string }) {
  return (
    <div className="rounded-xl bg-white p-5 shadow">
      <div className="text-sm text-gray-500">{title}</div>
      <div className="mt-1 text-2xl font-semibold">{value}</div>
    </div>
  );
}