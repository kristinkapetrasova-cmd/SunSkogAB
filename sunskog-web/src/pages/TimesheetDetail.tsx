// src/pages/TimesheetDetail.tsx
import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { getTimesheetDetail } from "@/lib/api";

type Entry = {
  id: string;
  workDate: string;
  project?: string | null;
  task?: string | null;
  hours: number;
  km: number;
  pieces: number;
  hourRate: number;
  kmRate: number;
  pieceRate: number;
  entryPay: number;
  comment?: string | null;
};

type Detail = {
  id: string;
  employeeId: string;
  periodStart: string;
  periodEnd: string;
  status: number | string;
  notes?: string | null;
  totalHours: number;
  totalKm: number;
  totalPieces: number;
  totalPay: number;
  submittedAt?: string | null;
  approvedAt?: string | null;
  entries: Entry[];
};

export default function TimesheetDetail() {
  const { id } = useParams();
  const [data, setData] = useState<Detail | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        if (!id) return;
        const d = await getTimesheetDetail(id);
        setData(d);
      } catch (e: any) {
        setErr(e?.message || "Nepodařilo se načíst detail");
      }
    })();
  }, [id]);

  if (err) {
    return (
      <main className="container-page">
        <div className="rounded-md border border-red-300 bg-red-50 p-3 text-sm text-red-700">{err}</div>
      </main>
    );
  }

  if (!data) return <main className="container-page">Načítám…</main>;

  return (
    <main className="container-page">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-semibold">
          Výkaz: {data.periodStart} – {data.periodEnd}
        </h1>
        <span className="badge">{String(data.status)}</span>
      </div>

      <div className="card p-4">
        <div className="mb-3 text-sm text-gray-600">{data.notes || "—"}</div>
        <div className="mb-4 grid grid-cols-2 gap-3 text-sm">
          <div>Hodiny: <strong>{data.totalHours}</strong></div>
          <div>Kilometry: <strong>{data.totalKm}</strong></div>
          <div>Kusy: <strong>{data.totalPieces}</strong></div>
          <div>Odměna: <strong>{data.totalPay} Kč</strong></div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="text-left text-gray-600">
              <tr>
                <th className="py-2">Datum</th>
                <th>Projekt</th>
                <th>Úkol</th>
                <th className="text-right">Hodiny</th>
                <th className="text-right">Km</th>
                <th className="text-right">Kusy</th>
                <th className="text-right">Odměna</th>
              </tr>
            </thead>
            <tbody>
              {data.entries.map((e) => (
                <tr key={e.id} className="border-t">
                  <td className="py-2">{e.workDate}</td>
                  <td>{e.project || "—"}</td>
                  <td>{e.task || "—"}</td>
                  <td className="text-right">{e.hours}</td>
                  <td className="text-right">{e.km}</td>
                  <td className="text-right">{e.pieces}</td>
                  <td className="text-right">{e.entryPay}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </main>
  );
}