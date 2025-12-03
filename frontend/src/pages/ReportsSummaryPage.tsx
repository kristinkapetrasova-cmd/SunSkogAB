// src/pages/ReportsSummaryPage.tsx
import React, { useEffect, useMemo, useState } from "react";
import { useLanguage } from "../LanguageContext";

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

export default function ReportsSummaryPage() {
  const { t, language } = useLanguage();
  
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
      if (!res.ok) throw new Error(`${t.error} HTTP ${res.status}`);
      const j = (await res.json()) as SummaryDto;
      setData(j);
    } catch (e: any) {
      setErr(e.message || (language === "cs" ? "Nepodařilo se načíst data." : "Failed to load data."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, []);

  return (
    <div className="space-y-4">
      <div className="rounded-xl bg-white p-5 shadow">
        <h2 className="mb-4 text-lg font-semibold">{t.reports} – {t.reportsSummary}</h2>
        <div className="flex flex-wrap items-end gap-3">
          <label className="text-sm">
            {language === "cs" ? "Od" : "From"}
            <input 
              type="date" 
              value={from} 
              onChange={e => setFrom(e.target.value)}
              className="ml-2 rounded border border-gray-300 px-2 py-1" 
            />
          </label>
          <label className="text-sm">
            {language === "cs" ? "Do" : "To"}
            <input 
              type="date" 
              value={to} 
              onChange={e => setTo(e.target.value)}
              className="ml-2 rounded border border-gray-300 px-2 py-1" 
            />
          </label>
          <button
            onClick={load}
            disabled={loading}
            className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {loading ? t.loading : (language === "cs" ? "Načíst" : "Load")}
          </button>
        </div>
      </div>

      {err && <div className="rounded-md bg-red-50 p-3 text-sm text-red-700">{err}</div>}

      {data && (
        <>
          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <Stat title={t.hours} value={data.totals.hours.toFixed(2)} />
            <Stat title={t.km} value={data.totals.km.toFixed(1)} />
            <Stat title={t.pieces} value={data.totals.pieces.toString()} />
            <Stat 
              title={language === "cs" ? `Odměny (${data.currency})` : `Pay (${data.currency})`} 
              value={data.totals.pay.toFixed(2)} 
            />
          </div>

          {/* Volitelně: Zobrazení timesheetů podle statusu */}
          {data.timesheetsByStatus && data.timesheetsByStatus.length > 0 && (
            <div className="rounded-xl bg-white p-5 shadow">
              <h3 className="text-md font-semibold mb-3">
                {language === "cs" ? "Výkazy podle stavu" : "Timesheets by Status"}
              </h3>
              <div className="space-y-2">
                {data.timesheetsByStatus.map((item, idx) => (
                  <div key={idx} className="flex justify-between items-center text-sm">
                    <span className="text-gray-600">
                      {item.status === "Draft" && t.statusDraft}
                      {item.status === "Submitted" && t.statusSubmitted}
                      {item.status === "Approved" && t.statusApproved}
                      {item.status === "Returned" && t.statusReturned}
                      {item.status === "Paid" && t.statusPaid}
                      {!["Draft", "Submitted", "Approved", "Returned", "Paid"].includes(item.status) && item.status}
                    </span>
                    <span className="font-semibold">{item.count}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function Stat({ title, value }: { title: string; value: string }) {
  return (
    <div className="rounded-xl bg-white p-5 shadow">
      <div className="text-sm text-gray-500">{title}</div>
      <div className="mt-1 text-2xl font-semibold">{value}</div>
    </div>
  );
}