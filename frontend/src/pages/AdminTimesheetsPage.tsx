import React, { useEffect, useState } from "react";
import {
  TimesheetDto,
  TimesheetStatus,
  getAdminTimesheets,
  approveTimesheet,
  returnTimesheet,
  fmtMoney,
} from "../lib/api";
import { useLanguage } from "../LanguageContext";

type FilterStatus = "All" | TimesheetStatus;

export default function AdminTimesheetsPage() {
  const { t, language } = useLanguage();
  const [items, setItems] = useState<TimesheetDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [status, setStatus] = useState<FilterStatus>("Submitted");
  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const all = await getAdminTimesheets();
      let filtered = all;

      if (status !== "All") {
        filtered = filtered.filter((t) => t.status === status);
      }
      if (email.trim()) {
        const q = email.trim().toLowerCase();
        filtered = filtered.filter((t) =>
          (t.employeeId || "").toLowerCase().includes(q)
        );
      }

      setItems(filtered);
    } catch (e: any) {
      console.error(e);
      setError(e.message || (language === "cs" ? "Chyba při načtení výkazů." : "Error loading timesheets."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status]);

  async function handleApprove(id: string) {
    if (!confirm(t.approveConfirm)) return;
    try {
      await approveTimesheet(id);
      await load();
    } catch (e: any) {
      alert(e.message || (language === "cs" ? "Nelze schválit výkaz." : "Cannot approve timesheet."));
    }
  }

  async function handleReturn(id: string) {
    const reason = prompt(
      t.returnReason + ":",
      language === "cs" ? "Prosím doplň některé položky." : "Please add some items."
    );
    if (reason === null) return; // cancel
    try {
      await returnTimesheet(id, reason || undefined);
      await load();
    } catch (e: any) {
      alert(e.message || (language === "cs" ? "Nelze vrátit výkaz." : "Cannot return timesheet."));
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold">{t.adminPanel} – {t.timesheets}</h1>
          <p className="text-sm text-gray-600">
            {language === "cs" ? "Schvalování a kontrola výkazů zaměstnanců." : "Approve and review employee timesheets."}
          </p>
        </div>
        <button
          onClick={load}
          disabled={loading}
          className="px-3 py-1.5 rounded-lg border text-sm hover:bg-gray-50 disabled:opacity-50"
        >
          {language === "cs" ? "Obnovit" : "Refresh"}
        </button>
      </div>

      {/* Filtry */}
      <div className="flex flex-wrap gap-3 items-end bg-white p-3 rounded-xl shadow-sm border">
        <div className="flex flex-col">
          <label className="text-xs text-gray-500">{t.status}</label>
          <select
            value={status}
            onChange={(e) => setStatus(e.target.value as FilterStatus)}
            className="border rounded-lg px-2 py-1 text-sm"
          >
            <option value="All">{language === "cs" ? "Vše" : "All"}</option>
            <option value="Submitted">{language === "cs" ? "Jen čekající" : "Pending only"} ({t.statusSubmitted})</option>
            <option value="Approved">{t.statusApproved}</option>
            <option value="Returned">{t.statusReturned}</option>
            <option value="Draft">{t.statusDraft}</option>
          </select>
        </div>

        <div className="flex flex-col">
          <label className="text-xs text-gray-500">
            {language === "cs" ? "Hledat podle ID zaměstnance" : "Search by Employee ID"}
          </label>
          <input
            type="text"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder={language === "cs" ? "např. user1 nebo GUID" : "e.g. user1 or GUID"}
            className="border rounded-lg px-2 py-1 text-sm min-w-[180px]"
          />
        </div>

        <button
          onClick={load}
          disabled={loading}
          className="px-3 py-1.5 rounded-lg bg-black text-white text-sm hover:opacity-90 disabled:opacity-50"
        >
          {language === "cs" ? "Filtrovat" : "Filter"}
        </button>
      </div>

      {error && (
        <div className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">
          {error}
        </div>
      )}

      <div className="bg-white rounded-xl shadow-sm border overflow-x-auto">
        <table className="min-w-full text-sm">
          <thead className="bg-gray-50">
            <tr className="text-left text-gray-500">
              <th className="px-3 py-2">{t.employee} (ID)</th>
              <th className="px-3 py-2">{t.period}</th>
              <th className="px-3 py-2">{t.status}</th>
              <th className="px-3 py-2 text-right">{language === "cs" ? "Hod" : "Hrs"}</th>
              <th className="px-3 py-2 text-right">{t.km}</th>
              <th className="px-3 py-2 text-right">{language === "cs" ? "Ks" : "Pcs"}</th>
              <th className="px-3 py-2 text-right">{t.pay}</th>
              <th className="px-3 py-2 text-right">{t.actions}</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr>
                <td className="px-3 py-3 text-gray-500" colSpan={8}>
                  {t.loading}
                </td>
              </tr>
            )}

            {!loading && items.length === 0 && (
              <tr>
                <td className="px-3 py-3 text-gray-500" colSpan={8}>
                  {language === "cs" ? "Žádné výkazy pro zvolený filtr." : "No timesheets for selected filter."}
                </td>
              </tr>
            )}

            {!loading &&
              items.map((t) => (
                <tr key={t.id} className="border-t">
                  <td className="px-3 py-2">
                    <div className="text-xs text-gray-500">
                      {t.employeeId}
                    </div>
                  </td>
                  <td className="px-3 py-2">
                    {t.periodStart} – {t.periodEnd}
                  </td>
                  <td className="px-3 py-2">
                    <span
                      className={
                        t.status === "Submitted"
                          ? "text-amber-700"
                          : t.status === "Approved"
                          ? "text-green-700"
                          : t.status === "Returned"
                          ? "text-red-700"
                          : "text-gray-600"
                      }
                    >
                      {t.status}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-right">
                    {t.totalHours.toFixed(2)}
                  </td>
                  <td className="px-3 py-2 text-right">
                    {t.totalKm.toFixed(2)}
                  </td>
                  <td className="px-3 py-2 text-right">{t.totalPieces}</td>
                  <td className="px-3 py-2 text-right">
                    {fmtMoney(t.totalPay)}
                  </td>
                  <td className="px-3 py-2 text-right">
                    {t.status === "Submitted" ? (
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={() => handleApprove(t.id)}
                          className="px-2 py-1 rounded bg-green-600 text-white text-xs hover:bg-green-700"
                        >
                          {language === "cs" ? "Schválit" : "Approve"}
                        </button>
                        <button
                          onClick={() => handleReturn(t.id)}
                          className="px-2 py-1 rounded bg-red-600 text-white text-xs hover:bg-red-700"
                        >
                          {language === "cs" ? "Vrátit" : "Return"}
                        </button>
                      </div>
                    ) : (
                      <span className="text-xs text-gray-400">
                        –
                      </span>
                    )}
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}