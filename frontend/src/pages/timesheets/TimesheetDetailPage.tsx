// src/pages/timesheets/TimesheetDetailPage.tsx
import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  getTimesheet,
  getTimesheetEntries,
  createTimesheetEntry,
  deleteTimesheetEntry,
  submitTimesheet,
  approveTimesheet,
  returnTimesheet,
  type TimesheetDto,
  type TimesheetEntryDto,
  fmtMoney,
  todayYMD,
} from "../../lib/api";
import { useAuth } from "../../AuthContext";
import { useLanguage } from "../../LanguageContext";

const TimesheetDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { t, language } = useLanguage();

  const [item, setItem] = useState<TimesheetDto | null>(null);
  const [entries, setEntries] = useState<TimesheetEntryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const roles = user?.roles || [];
  const isApprover = roles.includes("TeamLead") || roles.includes("Management") || roles.includes("Admin");

  const canEdit =
    item?.status === "Draft" ||
    item?.status === "Returned";

  const canApprove =
    isApprover &&
    item?.status === "Submitted";

  async function loadAll() {
    if (!id) return;
    setLoading(true);
    try {
      const ts = await getTimesheet(id);
      const raw = await getTimesheetEntries(id);

      let es: TimesheetEntryDto[] = [];
      if (Array.isArray(raw)) {
        es = raw;
      } else if (raw && typeof raw === "object" && Array.isArray((raw as any).items)) {
        es = (raw as any).items;
      }

      setItem(ts);
      setEntries(es);
    } catch (e: any) {
      console.error(e);
      alert(`${t.error}: ${e.message || e}`);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (!id) return;
    loadAll();
  }, [id]);

  async function handleAddEntry(form: FormData) {
    if (!id || !item) return;

    const payload = {
      workDate: String(form.get("workDate") || todayYMD()),
      project: ((form.get("project") as string) || "").trim() || null,
      task: ((form.get("task") as string) || "").trim() || null,
      hours: Number(form.get("hours") || 0),
      km: Number(form.get("km") || 0),
      pieces: Number(form.get("pieces") || 0),
      comment: ((form.get("comment") as string) || "").trim() || null,
    };

    if (!payload.hours && !payload.km && !payload.pieces) {
      alert(language === "cs" 
        ? "Zadej alespoň hodiny, km nebo ks." 
        : "Enter at least hours, km or pieces.");
      return;
    }

    setBusy(true);
    try {
      await createTimesheetEntry(id, payload);
      await loadAll();
      (document.getElementById("entry-form") as HTMLFormElement | null)?.reset();
    } catch (e: any) {
      console.error(e);
      alert(`${t.error}: ${e.message || e}`);
    } finally {
      setBusy(false);
    }
  }

  async function handleDelete(entryId: string) {
    if (!id) return;
    
    const confirmMsg = language === "cs" 
      ? "Opravdu smazat tuto položku?" 
      : "Really delete this entry?";
    if (!confirm(confirmMsg)) return;

    setBusy(true);
    try {
      await deleteTimesheetEntry(id, entryId);
      await loadAll();
    } catch (e: any) {
      console.error(e);
      alert(`${t.error}: ${e.message || e}`);
    } finally {
      setBusy(false);
    }
  }

  async function handleSubmit() {
    if (!id || !item) return;

    if (!entries || entries.length === 0) {
      alert(language === "cs" 
        ? "Výkaz bez položek nelze odeslat." 
        : "Cannot submit timesheet without entries.");
      return;
    }

    const confirmMsg = language === "cs"
      ? "Odeslat výkaz ke schválení? Po odeslání už nepůjde upravovat."
      : "Submit timesheet for approval? After submission it cannot be edited.";
    if (!confirm(confirmMsg)) return;

    setBusy(true);
    try {
      await submitTimesheet(id);
      await loadAll();
    } catch (e: any) {
      console.error(e);
      alert(`${t.error}: ${e.message || e}`);
    } finally {
      setBusy(false);
    }
  }

  async function handleApprove() {
    if (!id || !item) return;
    if (!confirm(t.approveConfirm)) return;

    setBusy(true);
    try {
      await approveTimesheet(id);
      await loadAll();
    } catch (e: any) {
      console.error(e);
      alert(`${t.error}: ${e.message || e}`);
    } finally {
      setBusy(false);
    }
  }

  async function handleReturn() {
    if (!id || !item) return;

    const reason = prompt(t.returnReason + ":", "");
    if (reason === null) return; // zrušeno

    setBusy(true);
    try {
      await returnTimesheet(id, reason || (language === "cs" ? "Vráceno ke kontrole." : "Returned for review."));
      await loadAll();
    } catch (e: any) {
      console.error(e);
      alert(`${t.error}: ${e.message || e}`);
    } finally {
      setBusy(false);
    }
  }

  if (!id) {
    return (
      <div className="space-y-3">
        <p className="text-sm text-gray-600">
          {language === "cs" ? "Chybí ID výkazu v URL." : "Missing timesheet ID in URL."}
        </p>
        <button
          onClick={() => navigate("/app/timesheets")}
          className="text-sm text-blue-600 underline"
        >
          {t.backToList}
        </button>
      </div>
    );
  }

  if (loading && !item) {
    return <div className="text-sm text-gray-600">{t.loading}</div>;
  }

  if (!item) {
    return (
      <div className="space-y-3">
        <p className="text-sm text-red-600">
          {language === "cs" ? "Výkaz nenalezen." : "Timesheet not found."}
        </p>
        <button
          onClick={() => navigate("/app/timesheets")}
          className="text-sm text-blue-600 underline"
        >
          {t.backToList}
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Hlavička */}
      <div className="flex items-center justify-between gap-3">
        <div>
          <button
            onClick={() => navigate("/app/timesheets")}
            className="text-xs text-gray-500 hover:text-gray-800"
          >
            ← {t.backToList}
          </button>
          <h1 className="text-xl font-semibold mt-1">
            {language === "cs" ? "Výkaz" : "Timesheet"} {item.periodStart} – {item.periodEnd}
          </h1>
          <div className="text-xs text-gray-500">
            {t.status}: <strong>{
              item.status === "Draft" ? t.statusDraft :
              item.status === "Submitted" ? t.statusSubmitted :
              item.status === "Approved" ? t.statusApproved :
              item.status === "Returned" ? t.statusReturned :
              item.status
            }</strong>
          </div>
        </div>

        <div className="text-right text-sm">
          <div>{item.totalHours} h</div>
          <div className="text-xs text-gray-500">
            {item.totalKm} km · {item.totalPieces} {language === "cs" ? "ks" : "pcs"}
          </div>
          <div className="font-semibold">{fmtMoney(item.totalPay)}</div>

          {canEdit && (
            <button
              onClick={handleSubmit}
              disabled={busy || !entries || !entries.length}
              className="mt-2 px-3 py-1.5 rounded-xl bg-blue-600 text-white text-xs disabled:opacity-50"
            >
              {t.submit}
            </button>
          )}

          {canApprove && (
            <div className="mt-2 flex flex-col gap-1">
              <button
                onClick={handleApprove}
                disabled={busy}
                className="px-3 py-1.5 rounded-xl bg-emerald-600 text-white text-xs disabled:opacity-50"
              >
                {t.approve}
              </button>
              <button
                onClick={handleReturn}
                disabled={busy}
                className="px-3 py-1.5 rounded-xl bg-red-600 text-white text-xs disabled:opacity-50"
              >
                {t.return}
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Položky */}
      <div className="rounded-2xl border bg-white p-4 space-y-4">
        <div className="flex items-center justify-between gap-2">
          <h2 className="font-medium text-sm">{t.entries}</h2>
          <div className="text-[10px] text-gray-500">
            {language === "cs" 
              ? "Přidávej pracovní dny, dohody, kilometry, kusy…" 
              : "Add work days, agreements, kilometers, pieces…"}
          </div>
        </div>

        {canEdit && (
          <EntryForm
            busy={busy}
            onSubmit={(fd) => handleAddEntry(fd)}
          />
        )}

        <div>
          {!entries || entries.length === 0 ? (
            <div className="text-sm text-gray-500">{t.noEntries}</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full text-xs">
                <thead>
                  <tr className="text-left text-gray-500 border-b">
                    <th className="py-1.5 pr-3">{t.date}</th>
                    <th className="py-1.5 pr-3">{t.project}</th>
                    <th className="py-1.5 pr-3">{t.task}</th>
                    <th className="py-1.5 pr-3">{language === "cs" ? "Hod" : "Hrs"}</th>
                    <th className="py-1.5 pr-3">Km</th>
                    <th className="py-1.5 pr-3">{language === "cs" ? "Ks" : "Pcs"}</th>
                    <th className="py-1.5 pr-3">{t.pay}</th>
                    <th className="py-1.5 pr-3 text-right"></th>
                  </tr>
                </thead>
                <tbody>
                  {entries.map((e) => (
                    <tr key={e.id} className="border-b last:border-b-0">
                      <td className="py-1.5 pr-3">{e.workDate}</td>
                      <td className="py-1.5 pr-3">{e.project || "-"}</td>
                      <td className="py-1.5 pr-3">{e.task || "-"}</td>
                      <td className="py-1.5 pr-3">{e.hours}</td>
                      <td className="py-1.5 pr-3">{e.km}</td>
                      <td className="py-1.5 pr-3">{e.pieces}</td>
                      <td className="py-1.5 pr-3">{fmtMoney(e.entryPay)}</td>
                      <td className="py-1.5 pr-3 text-right">
                        {canEdit && (
                          <button
                            onClick={() => handleDelete(e.id)}
                            disabled={busy}
                            className="text-[10px] text-red-600 hover:underline"
                          >
                            {t.delete}
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

function EntryForm({
  onSubmit,
  busy,
}: {
  onSubmit: (fd: FormData) => void;
  busy: boolean;
}) {
  const { t, language } = useLanguage();
  
  return (
    <form
      id="entry-form"
      className="grid grid-cols-12 gap-2 text-xs"
      onSubmit={(e) => {
        e.preventDefault();
        const fd = new FormData(e.currentTarget);
        onSubmit(fd);
      }}
    >
      <input
        name="workDate"
        type="date"
        defaultValue={todayYMD()}
        required
        className="col-span-12 sm:col-span-3 rounded-xl border px-2 py-1.5"
      />
      <input
        name="project"
        type="text"
        placeholder={t.project}
        className="col-span-12 sm:col-span-2 rounded-xl border px-2 py-1.5"
      />
      <input
        name="task"
        type="text"
        placeholder={t.task}
        className="col-span-12 sm:col-span-3 rounded-xl border px-2 py-1.5"
      />
      <input
        name="hours"
        type="number"
        step="0.5"
        min="0"
        placeholder={language === "cs" ? "Hod" : "Hrs"}
        className="col-span-4 sm:col-span-1 rounded-xl border px-2 py-1.5"
      />
      <input
        name="km"
        type="number"
        min="0"
        placeholder="Km"
        className="col-span-4 sm:col-span-1 rounded-xl border px-2 py-1.5"
      />
      <input
        name="pieces"
        type="number"
        min="0"
        placeholder={language === "cs" ? "Ks" : "Pcs"}
        className="col-span-4 sm:col-span-1 rounded-xl border px-2 py-1.5"
      />
      <input
        name="comment"
        type="text"
        placeholder={t.comment}
        className="col-span-12 rounded-xl border px-2 py-1.5"
      />
      <div className="col-span-12 flex justify-end">
        <button
          type="submit"
          disabled={busy}
          className="px-3 py-1.5 rounded-xl bg-black text-white disabled:opacity-50"
        >
          {t.addEntry}
        </button>
      </div>
    </form>
  );
}

export default TimesheetDetailPage;