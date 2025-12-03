// src/pages/Timesheets.tsx
import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

type TimesheetSummaryDto = {
  id: string;
  employeeId: string;
  periodStart: string; // DateOnly přichází jako ISO string
  periodEnd: string;
  status: number;
  totalHours: number;
  totalKm: number;
  totalPieces: number;
  totalPay: number;
  submittedAt?: string | null;
  approvedAt?: string | null;
};

const API = import.meta.env.VITE_API_URL;

function fmtDate(d?: string | null) {
  if (!d) return "—";
  // DateOnly jde bezpečně vykreslit jako toLocaleDateString, ale
  // některé prohlížeče vyžadují konstrukci přes Date
  try {
    return new Date(d).toLocaleDateString();
  } catch {
    return d;
  }
}

function statusBadge(status: number) {
  const map: Record<number, { label: string; cls: string }> = {
    0: { label: "Draft", cls: "badge border-gray-300 text-gray-700" },
    1: { label: "In Review", cls: "badge border-blue-300 text-blue-700" },
    2: { label: "Approved", cls: "badge border-green-300 text-green-700" },
    3: { label: "Returned", cls: "badge border-amber-300 text-amber-700" },
  };
  return map[status] ?? { label: `#${status}`, cls: "badge" };
}

export default function Timesheets() {
  const [items, setItems] = useState<TimesheetSummaryDto[] | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem("token");
    fetch(`${API}/api/timesheets`, {
      headers: { Authorization: `Bearer ${token}` },
    })
      .then(async (r) => {
        if (!r.ok) throw new Error(`${r.status} ${r.statusText}`);
        return r.json();
      })
      .then(setItems)
      .catch((e) => setErr(e.message ?? "Chyba načtení"))
      .finally(() => setLoading(false));
  }, []);

  const total = useMemo(
    () => ({
      hours: items?.reduce((s, x) => s + (x.totalHours ?? 0), 0) ?? 0,
      km: items?.reduce((s, x) => s + (x.totalKm ?? 0), 0) ?? 0,
      pay: items?.reduce((s, x) => s + (x.totalPay ?? 0), 0) ?? 0,
    }),
    [items]
  );

  return (
    <div className="space-y-6">
      <div className="flex items-end justify-between">
        <div>
          <h1 className="text-xl font-semibold">Moje výkazy</h1>
          <p className="text-sm text-gray-600">Přehled všech období a součtů.</p>
        </div>
      </div>

      <div className="card overflow-hidden">
        <table className="min-w-full text-sm">
          <thead className="bg-gray-50">
            <tr className="text-left">
              <th className="px-4 py-3">Období</th>
              <th className="px-4 py-3">Hodin</th>
              <th className="px-4 py-3">Km</th>
              <th className="px-4 py-3">Odměna</th>
              <th className="px-4 py-3">Stav</th>
              <th className="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr>
                <td className="px-4 py-5 text-gray-500" colSpan={6}>
                  Načítám…
                </td>
              </tr>
            )}

            {err && !loading && (
              <tr>
                <td className="px-4 py-5 text-red-600" colSpan={6}>
                  {err}
                </td>
              </tr>
            )}

            {!loading && !err && items?.length === 0 && (
              <tr>
                <td className="px-4 py-5 text-gray-500" colSpan={6}>
                  Zatím žádné výkazy.
                </td>
              </tr>
            )}

            {items?.map((t) => {
              const s = statusBadge(t.status);
              return (
                <tr key={t.id} className="border-t">
                  <td className="px-4 py-3">
                    {fmtDate(t.periodStart)} – {fmtDate(t.periodEnd)}
                  </td>
                  <td className="px-4 py-3">{t.totalHours.toFixed(2)}</td>
                  <td className="px-4 py-3">{t.totalKm.toFixed(2)}</td>
                  <td className="px-4 py-3">{t.totalPay.toFixed(2)}&nbsp;SEK</td>
                  <td className="px-4 py-3">
                    <span className={s.cls}>{s.label}</span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link to={`/timesheets/${t.id}`} className="btn-outline">
                      Detail
                    </Link>
                  </td>
                </tr>
              );
            })}
          </tbody>

          {items && items.length > 0 && (
            <tfoot className="border-t bg-gray-50">
              <tr>
                <td className="px-4 py-3 font-medium">Součty</td>
                <td className="px-4 py-3 font-medium">{total.hours.toFixed(2)}</td>
                <td className="px-4 py-3 font-medium">{total.km.toFixed(2)}</td>
                <td className="px-4 py-3 font-medium">{total.pay.toFixed(2)}&nbsp;SEK</td>
                <td colSpan={2} />
              </tr>
            </tfoot>
          )}
        </table>
      </div>
    </div>
  );
}