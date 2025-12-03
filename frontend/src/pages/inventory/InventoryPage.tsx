import React, { useEffect, useState } from "react";
import {
  listInventoryItems,
  createInventoryItem,
  updateInventoryItem,
  deleteInventoryItem,
  listStockMovements,
  createStockMovement,
  listLowStock,
  type InventoryItem,
  type StockMovement,
} from "../../lib/api";
import { useLanguage } from "../../LanguageContext";
import QRCodeDisplay from "../../components/QRCodeDisplay";

function numberFmt(v: number) {
  return new Intl.NumberFormat("cs-CZ", { maximumFractionDigits: 2 }).format(v);
}

export default function InventoryPage() {
  const { t, language } = useLanguage();
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [q, setQ] = useState("");
  const [loading, setLoading] = useState(false);
  const [selected, setSelected] = useState<InventoryItem | null>(null);
  const [movements, setMovements] = useState<StockMovement[]>([]);
  const [low, setLow] = useState<{ id: string; name: string; sku?: string | null; current: number; min: number }[]>([]);
  const [showQrModal, setShowQrModal] = useState(false);

  async function load() {
    setLoading(true);
    try {
      const [list, lows] = await Promise.all([
        listInventoryItems(q || undefined),
        listLowStock(),
      ]);
      setItems(list);
      setLow(lows);
      if (list.length && !selected) setSelected(list[0]);
      if (selected) {
        const m = await listStockMovements(selected.id);
        setMovements(m);
      }
    } catch (e: any) {
      alert(`${language === "cs" ? "Sklad se nepodařilo načíst" : "Failed to load inventory"}: ${e.message}`);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); /* eslint-disable-next-line */ }, [q]);

  useEffect(() => {
    (async () => {
      if (selected) {
        const m = await listStockMovements(selected.id);
        setMovements(m);
      } else {
        setMovements([]);
      }
    })();
  }, [selected?.id]);

  async function handleCreate() {
    const name = prompt(t.itemName + ":");
    if (!name) return;
    await createInventoryItem({ name, minStock: 0, isActive: true });
    await load();
  }

  async function handleEdit(item: InventoryItem) {
    const name = prompt(language === "cs" ? "Upravit název položky:" : "Edit item name:", item.name);
    if (!name) return;
    await updateInventoryItem(item.id, { ...item, name });
    await load();
  }

  async function handleDelete(item: InventoryItem) {
    if (!confirm(`${t.delete} "${item.name}"?`)) return;
    await deleteInventoryItem(item.id);
    if (selected?.id === item.id) setSelected(null);
    await load();
  }

  async function handleMovement(type: "in" | "out") {
    if (!selected) return;
    const raw = prompt(
      type === "in" 
        ? `${t.quantity} (${t.typeIn}):`
        : `${t.quantity} (${t.typeOut}):`
    );
    if (!raw) return;
    const qty = Number(raw);
    if (!Number.isFinite(qty) || qty <= 0) {
      alert(language === "cs" ? "Zadej kladné číslo." : "Enter a positive number.");
      return;
    }
    await createStockMovement({
      itemId: selected.id,
      type: type === "in" ? 0 : 1,
      quantity: qty,
    });
    await load();
  }

  function handleShowQr() {
    if (!selected) return;
    setShowQrModal(true);
  }

  return (
    <>
      <div className="grid grid-cols-12 gap-4">
        {/* LEVÝ PANEL */}
        <div className="col-span-12 md:col-span-4">
          <div className="rounded-2xl border bg-white p-3 space-y-3">
            <div className="flex gap-2">
              <input
                value={q}
                onChange={(e) => setQ(e.target.value)}
                className="flex-1 rounded-xl border px-3 py-2"
                placeholder={t.searchPlaceholder}
              />
              <button
                onClick={handleCreate}
                className="px-3 py-2 rounded-xl bg-black text-white"
              >
                {t.addItem}
              </button>
            </div>

            {loading && <div className="text-sm text-gray-500">{t.loading}</div>}
            {!loading && items.length === 0 && (
              <div className="text-sm text-gray-500">{t.noItems}</div>
            )}

            <ul className="divide-y">
              {items.map((it) => (
                <li
                  key={it.id}
                  onClick={() => setSelected(it)}
                  className={`p-3 cursor-pointer rounded-xl ${
                    selected?.id === it.id ? "bg-gray-100" : "hover:bg-gray-50"
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <div>
                      <div className="font-medium">{it.name}</div>
                      <div className="text-xs text-gray-500">
                        {it.sku || "-"} · {t.min}: {numberFmt(it.minStock)} · {it.isActive ? t.active : t.inactive}
                      </div>
                    </div>
                    <div className="text-right">
                      <button
                        onClick={(e) => { e.stopPropagation(); handleEdit(it); }}
                        className="text-xs text-blue-600 hover:underline mr-2"
                      >
                        {t.edit}
                      </button>
                      <button
                        onClick={(e) => { e.stopPropagation(); handleDelete(it); }}
                        className="text-xs text-red-600 hover:underline"
                      >
                        {t.delete}
                      </button>
                    </div>
                  </div>
                </li>
              ))}
            </ul>

            {low.length > 0 && (
              <div className="mt-3 rounded-xl border p-3">
                <div className="text-sm font-medium mb-1">{t.lowStock}</div>
                <ul className="text-sm text-red-700">
                  {low.map(l => (
                    <li key={l.id}>
                      {l.name} ({l.sku || "—"}): {numberFmt(l.current)} / {t.min} {numberFmt(l.min)}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        </div>

        {/* PRAVÝ PANEL */}
        <div className="col-span-12 md:col-span-8">
          <div className="rounded-2xl border bg-white p-4">
            {!selected ? (
              <div className="text-sm text-gray-500">{t.selectItem}</div>
            ) : (
              <>
                <div className="flex items-center justify-between mb-3">
                  <div>
                    <div className="text-lg font-semibold">{selected.name}</div>
                    <div className="text-xs text-gray-500">
                      {t.sku}: {selected.sku || "—"} · {t.serialNumber}: {selected.serialNumber || "—"}
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={() => handleMovement("in")}
                      className="px-3 py-2 rounded-xl bg-green-600 text-white"
                    >
                      + {t.receive}
                    </button>
                    <button
                      onClick={() => handleMovement("out")}
                      className="px-3 py-2 rounded-xl bg-amber-600 text-white"
                    >
                      − {t.issue}
                    </button>
                    <button
                      onClick={handleShowQr}
                      className="px-3 py-2 rounded-xl bg-blue-600 text-white"
                    >
                      {t.showQr}
                    </button>
                  </div>
                </div>

                <div className="overflow-x-auto">
                  <table className="min-w-full text-sm">
                    <thead>
                      <tr className="text-left text-gray-500">
                        <th className="py-2 pr-3">{t.date}</th>
                        <th className="py-2 pr-3">{t.type}</th>
                        <th className="py-2 pr-3">{t.quantity}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {movements.length === 0 ? (
                        <tr><td colSpan={3} className="py-3 text-gray-500">{t.noMovements}</td></tr>
                      ) : movements.map(m => (
                        <tr key={m.id} className="border-t">
                          <td className="py-2 pr-3">{m.at?.replace("T", " ").substring(0, 16)}</td>
                          <td className="py-2 pr-3">{m.type === 0 ? t.typeIn : t.typeOut}</td>
                          <td className="py-2 pr-3">{numberFmt(m.quantity)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </div>
        </div>
      </div>

      {/* QR Modal */}
      {showQrModal && selected && (
        <div
          className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
          onClick={() => setShowQrModal(false)}
        >
          <div
            className="bg-white rounded-2xl p-6 max-w-md w-full mx-4"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold">{t.qrCode}</h2>
              <button
                onClick={() => setShowQrModal(false)}
                className="text-gray-500 hover:text-gray-700"
              >
                ✕
              </button>
            </div>
            <QRCodeDisplay
              data={`inv:${selected.id}|${selected.name}|${selected.sku || ""}`}
              itemName={selected.name}
              sku={selected.sku}
              serialNumber={selected.serialNumber}
            />
          </div>
        </div>
      )}
    </>
  );
}