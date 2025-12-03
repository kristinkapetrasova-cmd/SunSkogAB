// src/pages/timesheets/TimesheetsListPage.tsx
import React, { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  getTimesheets,
  createTimesheet,
  type TimesheetDto,
  monthRange,
} from "../lib/api";
import { useLanguage } from "../LanguageContext";

function fmtMoney(v: number) {
  return new Intl.NumberFormat("cs-CZ", { style: "currency", currency: "SEK" }).format(v ?? 0);
}

export default function TimesheetsListPage() {
  const { t, language } = useLanguage();
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState<TimesheetDto[]>([]);

  async function load() {
    setLoading(true);
    try {
      const data = await getTimesheets();
      setItems(data);
    } catch (e: any) {
      alert(`${t.error}: ${e?.message ?? e}`);
    } finally {
      setLoading(false);
    }
  }

  async function handleCreateMonth() {
    const { start, end } = monthRange();
    try {
      await createTimesheet({ periodStart: start, periodEnd: end });
      await load();
    } catch (e: any) {
      alert(`${t.error}: ${e?.message ?? e}`);
    }
  }

  useEffect(() => {
    load();
  }, []);

  return (
    <div className="mx-auto max-w-7xl p-4 md:p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">{t.myTimesheets}</h1>
        <button
          onClick={handleCreateMonth}
          className="px-3 py-2 rounded-xl bg-black text-white hover:opacity-90"
        >
          + {t.createTimesheet}
        </button>
      </div>

      {loading && <div className="text-sm text-gray-500">{t.loading}</div>}
      
      {!loading && items.length === 0 && (
        <div className="rounded-2xl border p-6 text-sm text-gray-500">
          {t.noResults}
        </div>
      )}

      <div className="grid gap-4">
        {items.map((ts) => (
          <Link
            key={ts.id}
            to={`/app/timesheets/${ts.id}`}
            className="rounded-2xl border bg-white p-4 hover:bg-gray-50 block"
          >
            <div className="flex items-center justify-between">
              <div>
                <div className="font-medium">
                  {ts.periodStart} – {ts.periodEnd}
                </div>
                <div className="text-xs text-gray-500">
                  {t.status}: {
                    ts.status === "Draft" ? t.statusDraft :
                    ts.status === "Submitted" ? t.statusSubmitted :
                    ts.status === "Approved" ? t.statusApproved :
                    ts.status === "Returned" ? t.statusReturned :
                    ts.status
                  }
                </div>
              </div>
              <div className="text-right">
                <div className="text-sm">{ts.totalHours} h</div>
                <div className="text-xs text-gray-500">{fmtMoney(ts.totalPay)}</div>
              </div>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}