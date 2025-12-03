import React, { useEffect, useMemo, useState } from "react";

const API_BASE =
  (import.meta as any).env?.VITE_API_BASE?.toString() || "http://localhost:5250";

type UserAgg = {
  team: string;        // očekáváme zatím "N/A"
  userId: string;
  email: string;
  name: string;
  hours: number;
  km: number;
  pieces: number;
  pay: number;
  currency: string;    // "SEK"
};

type UsersResponse = {
  range: { from: string; to: string };
  items: UserAgg[];
};

function getToken() {
  return localStorage.getItem("auth/token") || "";
}
const fmtNum = (n: number, d = 2) =>
  new Intl.NumberFormat("cs-CZ", { minimumFractionDigits: d, maximumFractionDigits: d }).format(n);

export default function ReportsUsersPage() {
  const todayIso = useMemo(() => new Date().toISOString().slice(0, 10), []);
  const firstOfMonthIso = useMemo(() => {
    const d = new Date(); d.setDate(1); return d.toISOString().slice(0, 10);
  }, []);

  const [from, setFrom] = useState(firstOfMonthIso);
  const [to, setTo] = useState(todayIso);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [data, setData] = useState<UsersResponse | null>(null);
  const [q, setQ] = useState("");

  const filtered = useMemo(() => {
    if (!data) return [];
    const needle = q.trim().toLowerCase();
    if (!needle) return data.items;
    return data.items.filter(x =>
      (x.name || "").toLowerCase().includes(needle) ||
      (x.email || "").toLowerCase().includes(needle)
    );
  }, [data, q]);

  const totals = useMemo(() => {
    const base = { hours: 0, km: 0, pieces: 0, pay: 0, currency: data?.items[0]?.currency ?? "SEK" };
    return filtered.reduce((acc, x) => ({
      hours: acc.hours + x.hours,
      km: acc.km + x.km,
      pieces: acc.pieces + x.pieces,
      pay: acc.pay + x.pay,
      currency: acc.currency
    }), base);
  }, [filtered, data]);

  async function load() {
    setErr(null); setLoading(true);
    try {
      const res = await fetch(`${API_BASE}/api/admin/reports/users?from=${from}&to=${to}`, {
        headers: { Authorization: `Bearer ${getToken()}` },
      });
      if (!res.ok) {
        let d = ""; try { const j = await res.json(); d = j.detail || j.title || ""; } catch {}
        throw new Error(d || `Chyba (HTTP ${res.status})`);
      }
      const j = (await res.json()) as UsersResponse;
      setData(j);
    } catch (e: any) {
      setErr(e.message || "Nepodařilo se načíst data.");
    } finally { setLoading(false); }
  }

  useEffect(() => { void load(); /* eslint-disable-next-line */ }, []);

  return (
    <div className="space-y-4">
      <div className="rounded-xl bg-white p-5 shadow">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h2 className="mb-1 text-lg font-semibold">Reporty – Uživatelé</h2>
            <p className="text-sm text-gray-600">
              Součty za období podle uživatele. (Team zatím „N/A“ – napojíme později.)
            </p>
          </div>
        </div>

        <div className="mt-4 flex flex-wrap items-end gap-3">
          <label className="text-sm">
            Od
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)}
                   className="ml-2 rounded border border-gray-300 px-2 py-1" />
          </label>

          <label className="text-sm">
            Do
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)}
                   className="ml-2 rounded border border-gray-300 px-2 py-1" />
          </label>

          <button onClick={load} disabled={loading}
                  className="rounded bg-blue-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50">
            {loading ? "Načítám…" : "Načíst"}
          </button>

          <div className="ml-auto flex items-center gap-2">
            <input
              placeholder="Hledat jméno / email…"
              value={q}
              onChange={(e) => setQ(e.target.value)}
              className="rounded border border-gray-300 px-3 py-1.5 text-sm w-60"
            />
          </div>
        </div>
      </div>

      {err && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 border border-red-200">{err}</div>
      )}

      {data && (
        <>
          {/* Totals */}
          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <Stat title="Hodiny" value={fmtNum(totals.hours, 2)} />
            <Stat title="Km" value={fmtNum(totals.km, 1)} />
            <Stat title="Kusy" value={fmtNum(totals.pieces, 0)} />
            <Stat title={`Odměny (${totals.currency})`} value={fmtNum(totals.pay, 2)} />
          </div>

          {/* Table */}
          <div className="rounded-xl bg-white p-5 shadow overflow-x-auto">
            <table className="min-w-[720px] w-full text-sm">
              <thead>
                <tr className="text-left text-gray-500 border-b">
                  <th className="py-2 pr-4">Jméno</th>
                  <th className="py-2 pr-4">Email</th>
                  <th className="py-2 pr-4">Team</th>
                  <th className="py-2 text-right">Hodiny</th>
                  <th className="py-2 text-right">Km</th>
                  <th className="py-2 text-right">Kusy</th>
                  <th className="py-2 text-right">Odměny</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((u) => (
                  <tr key={u.userId} className="border-b last:border-none">
                    <td className="py-2 pr-4">{u.name || "—"}</td>
                    <td className="py-2 pr-4 text-gray-600">{u.email || "—"}</td>
                    <td className="py-2 pr-4">{u.team || "N/A"}</td>
                    <td className="py-2 text-right">{fmtNum(u.hours, 2)}</td>
                    <td className="py-2 text-right">{fmtNum(u.km, 1)}</td>
                    <td className="py-2 text-right">{fmtNum(u.pieces, 0)}</td>
                    <td className="py-2 text-right">{fmtNum(u.pay, 2)} {u.currency}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="font-medium">
                  <td className="py-2 pr-4" colSpan={3}>Celkem zobrazeno</td>
                  <td className="py-2 text-right">{fmtNum(totals.hours, 2)}</td>
                  <td className="py-2 text-right">{fmtNum(totals.km, 1)}</td>
                  <td className="py-2 text-right">{fmtNum(totals.pieces, 0)}</td>
                  <td className="py-2 text-right">{fmtNum(totals.pay, 2)} {totals.currency}</td>
                </tr>
              </tfoot>
            </table>
          </div>
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