import { useEffect, useState } from "react";
import axios from "axios";

interface Timesheet {
  id: number;
  date: string;
  hours: number;
  description: string;
}

export default function DashboardPage() {
  const [timesheets, setTimesheets] = useState<Timesheet[]>([]);
  const [error, setError] = useState("");

  useEffect(() => {
    async function fetchData() {
      try {
        const token = localStorage.getItem("token");
        const res = await axios.get("http://localhost:5250/api/timesheets", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setTimesheets(res.data);
      } catch (err: any) {
        setError("Nepodařilo se načíst výkazy.");
      }
    }
    fetchData();
  }, []);

  return (
    <div className="container-page">
      <h1 className="text-2xl font-bold mb-4">Moje výkazy</h1>
      {error && <p className="text-red-600">{error}</p>}
      {timesheets.length === 0 ? (
        <p>Zatím žádné výkazy.</p>
      ) : (
        <table className="w-full border border-gray-200 rounded-lg">
          <thead className="bg-gray-100">
            <tr>
              <th className="px-4 py-2 text-left">Datum</th>
              <th className="px-4 py-2 text-left">Hodin</th>
              <th className="px-4 py-2 text-left">Popis</th>
            </tr>
          </thead>
          <tbody>
            {timesheets.map((t) => (
              <tr key={t.id} className="border-t">
                <td className="px-4 py-2">{t.date}</td>
                <td className="px-4 py-2">{t.hours}</td>
                <td className="px-4 py-2">{t.description}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}