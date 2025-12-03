import React, { useEffect, useMemo, useState } from "react";

const API_BASE =
  (import.meta as any).env?.VITE_API_BASE?.toString() || "http://localhost:5250";

type TeamRow = {
  team: string;
  userId: string;
  email: string;
  name: string;
  hours: number;
  km: number;
  pieces: number;
  pay: number;
  currency: string;
};

type TeamsDto = {
  range: { from: string; to: string };
  items: TeamRow[];
};

function getToken() {
  return localStorage.getItem("auth/token") || "";
}

export default function ReportsTeamsPage() {
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
  const [data, setData] = useState<TeamsDto | null>(null);
  const [q, setQ] = useState("");

  async function load() {
    setErr(null);
    setLoading(true);
    try {
      const url = `${API_BASE}/api/admin/reports/teams?from=${from}&to=${to}`;
      const res = await fetch(url, {
        headers: { Authorization: `Bearer ${getToken()}` },
      });
      if (!res.ok) throw new Error(`Chyba HTTP ${res.status}`);
      const j = (await res.json()) as TeamsDto;
      setData(j);
    } catch (e: any) {
      setErr(e.message || "Nepodařilo se načíst data.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const filtered = useMemo(() => {
    if (!data) return [];
    const term = q.trim().toLowerCase();
    const rows = data.items ?? [];
    const result = term
      ? rows.filter(
          (r) =>
            r.team.toLowerCase().includes(term) ||
            r.name.toLowerCase().includes(term) ||
            r.email.toLowerCase().includes(term)
        )
      : rows;

    // defaultně řadíme podle výše odměny (pay) sestupně
    return [...result].sort((a, b) => b.pay - a.pay);
  }, [data, q]);

  const totals = useMemo(() => {
    const cur = data?.items?.[0]?.currency ?? "SEK";
    const agg = (filtered || []).reduce(
      (acc, r) => {
        acc.hours += r.hours;
        acc.km += r.km;
        acc.pieces += r.pieces;
        acc.pay += r.pay;
        return acc;
      },
      { hours: 0, km: 0, pieces: 0, pay: 0, currency: cur }
    );
    return agg;
  }, [filtered, data]);

  return (
    <div className="space-y-4">
      <div className="rounded-xl bg-white p-5 shadow">
        <h2 className="mb-4 text-lg font-semibold">Reporty – Týmy</h2>

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
            className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {loading ? "Načítám…" : "Načíst"}
          </button>

          <div className="ml-auto flex-1 sm:flex-none" />

          <input
            placeholder="Hledat (tým, jméno, email)…"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            className="w-full sm:w-64 rounded border border-gray-300 px-3 py-1.5 text-sm"
          />
        </div>
      </div>

      {err && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700">{err}</div>
      )}

      {/* Cards s agregovanými čísly */}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Stat title="Hodiny" value={totals.hours.toFixed(2)} />
        <Stat title="Km" value={totals.km.toFixed(1)} />
        <Stat title="Kusy" value={totals.pieces.toString()} />
        <Stat
          title={`Odměny (${totals.currency})`}
          value={totals.pay.toFixed(2)}
        />
      </div>

      {/* Tabulka lidí v týmech */}
      <div className="overflow-auto rounded-xl bg-white shadow">
        <table className="min-w-[720px] w-full text-sm">
          <thead className="bg-gray-50 text-left">
            <tr className="text-gray-600">
              <Th>Team</Th>
              <Th>Jméno</Th>
              <Th>Email</Th>
              <Th align="right">Hodiny</Th>
              <Th align="right">Km</Th>
              <Th align="right">Kusy</Th>
              <Th align="right">Odměny</Th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((r) => (
              <tr key={`${r.userId}-${r.team}`} className="border-t">
                <Td>{r.team}</Td>
                <Td>{r.name || "—"}</Td>
                <Td className="text-gray-600">{r.email}</Td>
                <Td align="right">{r.hours.toFixed(2)}</Td>
                <Td align="right">{r.km.toFixed(1)}</Td>
                <Td align="right">{r.pieces}</Td>
                <Td align="right">
                  {r.pay.toFixed(2)}{" "}
                  <span className="text-gray-400">{r.currency}</span>
                </Td>
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr className="border-t">
                <Td colSpan={7} className="text-center text-gray-500 py-8">
                  Žádná data pro zadané období nebo filtr.
                </Td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
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

function Th({
  children,
  align,
}: {
  children: React.ReactNode;
  align?: "left" | "right";
}) {
  return (
    <th className={`px-4 py-3 ${align === "right" ? "text-right" : ""}`}>
      {children}
    </th>
  );
}

function Td({
  children,
  align,
  colSpan,
  className = "",
}: {
  children: React.ReactNode;
  align?: "left" | "right";
  colSpan?: number;
  className?: string;
}) {
  return (
    <td
      colSpan={colSpan}
      className={`px-4 py-2 ${align === "right" ? "text-right" : ""} ${className}`}
    >
      {children}
    </td>
  );
}