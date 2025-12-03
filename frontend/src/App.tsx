import React, { Component, PropsWithChildren, useEffect, useState } from "react";
import {
  BrowserRouter,
  Routes,
  Route,
  Navigate,
  Outlet,
  Link,
} from "react-router-dom";

import AppLayout from "./layouts/AppLayout";
import { AuthProvider, useAuth } from "./AuthContext";
import { useLanguage } from "./LanguageContext";

// Pages
import LoginPage from "./pages/LoginPage";
import ReportsSummaryPage from "./pages/ReportsSummaryPage";
import ReportsUsersPage from "./pages/ReportsUsersPage";
import ReportsTeamsPage from "./pages/ReportsTeamsPage";
import TimesheetsListPage from "./pages/timesheets/TimesheetsListPage";
import TimesheetDetailPage from "./pages/timesheets/TimesheetDetailPage";
import AdminTimesheetsPage from "./pages/AdminTimesheetsPage";
import InventoryPage from "./pages/inventory/InventoryPage";
import AdminUsersPage from "./pages/AdminUsersPage";

// API
import { getAdminTimesheets, getTimesheets, listLowStock } from "./lib/api";
import type { TimesheetDto } from "./lib/api";

// ====== ERROR BOUNDARY ======
class ErrorBoundary extends Component<PropsWithChildren, { error?: Error }> {
  state: { error?: Error } = { error: undefined };

  static getDerivedStateFromError(error: Error) {
    return { error };
  }

  render() {
    if (this.state.error) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-black text-white p-6">
          <div className="max-w-xl w-full rounded-xl border border-red-500 bg-zinc-900 p-6 shadow">
            <h2 className="text-lg font-semibold mb-2">Něco se pokazilo v UI</h2>
            <pre className="text-sm overflow-auto whitespace-pre-wrap">
              {this.state.error.message}
            </pre>
            <p className="text-xs text-gray-400 mt-2">
              Obnov stránku (Ctrl+R). Pokud chyba přetrvá, pošli mi prosím tuto hlášku.
            </p>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}

// ====== PROTECTED ROUTE ======
function ProtectedRoute() {
  const { user, loading } = useAuth();
  const { t } = useLanguage();

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-sm text-gray-500">{t.loading}</div>
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}

// ====== DASHBOARD - PRAKTICKÁ VERZE ======
function DashboardPage() {
  const { user } = useAuth();
  const { t, language } = useLanguage();
  const [loading, setLoading] = useState(true);
  const [pendingTimesheets, setPendingTimesheets] = useState<TimesheetDto[]>([]);
  const [lowStockItems, setLowStockItems] = useState<any[]>([]);
  const [myTimesheets, setMyTimesheets] = useState<TimesheetDto[]>([]);

  const roles = user?.roles ?? [];
  const isAdminLike = roles.some(r => r.toLowerCase() === "admin" || r.toLowerCase() === "management");
  const canUseInventory = isAdminLike || roles.some(r => r.toLowerCase() === "warehouse");

  useEffect(() => {
    async function loadDashboardData() {
      setLoading(true);
      try {
        // Načti data podle role
        const promises = [];

        // Moje výkazy (pro všechny)
        promises.push(
          getTimesheets().then(ts => setMyTimesheets(ts)).catch(() => setMyTimesheets([]))
        );

        // Výkazy ke schválení (jen pro admin)
        if (isAdminLike) {
          promises.push(
            getAdminTimesheets()
              .then(ts => {
                const pending = ts.filter(t => t.status === "Submitted");
                setPendingTimesheets(pending);
              })
              .catch(() => setPendingTimesheets([]))
          );
        }

        // Nízký stav skladu (pro admin a warehouse)
        if (canUseInventory) {
          promises.push(
            listLowStock().then(items => setLowStockItems(items)).catch(() => setLowStockItems([]))
          );
        }

        await Promise.all(promises);
      } finally {
        setLoading(false);
      }
    }

    loadDashboardData();
  }, [isAdminLike, canUseInventory]);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-gray-500">{t.loading}</div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Uvítání */}
      <div className="rounded-xl bg-white p-6 shadow">
        <h1 className="text-2xl font-semibold mb-1">{t.welcomeTitle}</h1>
        <p className="text-gray-600">{user?.name || user?.email}</p>
      </div>

      {/* Praktické karty */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Karta 1: Výkazy ke schválení (jen admin) */}
        {isAdminLike && (
          <Link
            to="/app/admin/timesheets"
            className="rounded-xl bg-white p-6 shadow hover:shadow-lg transition-all block"
          >
            <div className="flex items-center gap-3 mb-3">
              <div className={`w-12 h-12 rounded-lg flex items-center justify-center ${
                pendingTimesheets.length > 0 ? 'bg-orange-100' : 'bg-green-100'
              }`}>
                <span className="text-2xl">{pendingTimesheets.length > 0 ? '🔔' : '✓'}</span>
              </div>
              <div className="text-lg font-semibold text-gray-700">
                {language === "cs" ? "Ke schválení" : "Pending Approval"}
              </div>
            </div>
            <div className={`text-3xl font-bold ${
              pendingTimesheets.length > 0 ? 'text-orange-700' : 'text-green-700'
            }`}>
              {pendingTimesheets.length}
            </div>
            <div className="text-sm text-gray-500 mt-1">
              {pendingTimesheets.length === 0 
                ? (language === "cs" ? "Vše schváleno! 🎉" : "All approved! 🎉")
                : (language === "cs" ? "Výkazů čeká" : "Timesheets waiting")
              }
            </div>
          </Link>
        )}

        {/* Karta 2: Nízký stav skladu (admin + warehouse) */}
        {canUseInventory && (
          <Link
            to="/app/inventory"
            className="rounded-xl bg-white p-6 shadow hover:shadow-lg transition-all block"
          >
            <div className="flex items-center gap-3 mb-3">
              <div className={`w-12 h-12 rounded-lg flex items-center justify-center ${
                lowStockItems.length > 0 ? 'bg-red-100' : 'bg-green-100'
              }`}>
                <span className="text-2xl">{lowStockItems.length > 0 ? '⚠️' : '📦'}</span>
              </div>
              <div className="text-lg font-semibold text-gray-700">
                {language === "cs" ? "Nízký stav" : "Low Stock"}
              </div>
            </div>
            <div className={`text-3xl font-bold ${
              lowStockItems.length > 0 ? 'text-red-700' : 'text-green-700'
            }`}>
              {lowStockItems.length}
            </div>
            <div className="text-sm text-gray-500 mt-1">
              {lowStockItems.length === 0
                ? (language === "cs" ? "Vše v pořádku" : "All good")
                : (language === "cs" ? "Položek pod limitem" : "Items below limit")
              }
            </div>
            {lowStockItems.length > 0 && (
              <div className="mt-3 text-xs text-red-600">
                {lowStockItems.slice(0, 2).map(item => (
                  <div key={item.id}>• {item.name}</div>
                ))}
                {lowStockItems.length > 2 && (
                  <div>...{language === "cs" ? "a další" : "and more"}</div>
                )}
              </div>
            )}
          </Link>
        )}

        {/* Karta 3: Moje výkazy */}
        <Link
          to="/app/timesheets"
          className="rounded-xl bg-white p-6 shadow hover:shadow-lg transition-all block"
        >
          <div className="flex items-center gap-3 mb-3">
            <div className="w-12 h-12 rounded-lg bg-blue-100 flex items-center justify-center">
              <span className="text-2xl">📋</span>
            </div>
            <div className="text-lg font-semibold text-gray-700">
              {t.myTimesheets}
            </div>
          </div>
          <div className="text-3xl font-bold text-blue-700">
            {myTimesheets.length}
          </div>
          <div className="text-sm text-gray-500 mt-1">
            {language === "cs" ? "Celkem výkazů" : "Total timesheets"}
          </div>
          {myTimesheets.length > 0 && (
            <div className="mt-3 text-xs text-gray-600">
              {language === "cs" ? "Poslední: " : "Latest: "}
              <span className="font-medium">
                {myTimesheets[0].status === "Draft" && (language === "cs" ? "Koncept" : "Draft")}
                {myTimesheets[0].status === "Submitted" && (language === "cs" ? "Odesláno" : "Submitted")}
                {myTimesheets[0].status === "Approved" && (language === "cs" ? "Schváleno" : "Approved")}
                {myTimesheets[0].status === "Returned" && (language === "cs" ? "Vráceno" : "Returned")}
              </span>
            </div>
          )}
        </Link>
      </div>

      {/* Rychlé akce */}
      <div className="rounded-xl bg-white p-6 shadow">
        <h2 className="text-lg font-semibold mb-4">
          {language === "cs" ? "Rychlé akce" : "Quick Actions"}
        </h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          <Link
            to="/app/timesheets"
            className="p-4 rounded-lg border border-gray-200 hover:border-green-500 hover:bg-green-50 transition-all text-center"
          >
            <div className="text-2xl mb-2">📝</div>
            <div className="text-sm font-medium">{t.myTimesheets}</div>
          </Link>

          {canUseInventory && (
            <Link
              to="/app/inventory"
              className="p-4 rounded-lg border border-gray-200 hover:border-green-500 hover:bg-green-50 transition-all text-center"
            >
              <div className="text-2xl mb-2">📦</div>
              <div className="text-sm font-medium">{t.inventory}</div>
            </Link>
          )}

          <Link
            to="/app/reports/summary"
            className="p-4 rounded-lg border border-gray-200 hover:border-green-500 hover:bg-green-50 transition-all text-center"
          >
            <div className="text-2xl mb-2">📊</div>
            <div className="text-sm font-medium">{t.reports}</div>
          </Link>

          {isAdminLike && (
            <Link
              to="/app/admin/timesheets"
              className="p-4 rounded-lg border border-gray-200 hover:border-green-500 hover:bg-green-50 transition-all text-center"
            >
              <div className="text-2xl mb-2">✓</div>
              <div className="text-sm font-medium">{t.adminTimesheets}</div>
            </Link>
          )}
        </div>
      </div>
    </div>
  );
}

// ====== 404 ======
function NotFoundPage() {
  return (
    <div className="rounded-xl bg-white p-6 shadow">
      <h2 className="text-lg font-semibold">404 – Stránka nenalezena</h2>
      <p className="text-gray-600 mt-1">
        Zpět na{" "}
        <Link to="/app" className="text-blue-600 underline">
          Dashboard
        </Link>
        .
      </p>
    </div>
  );
}

// ====== ROOT APP ======
export default function App() {
  return (
    <ErrorBoundary>
      <BrowserRouter>
        <AuthProvider>
          <Routes>
            {/* root -> /app (ProtectedRoute se postará o redirect na login) */}
            <Route path="/" element={<Navigate to="/app" replace />} />

            {/* login */}
            <Route path="/login" element={<LoginPage />} />

            {/* chráněná část */}
            <Route element={<ProtectedRoute />}>
              <Route path="/app" element={<AppLayout />}>
                <Route index element={<DashboardPage />} />

                {/* výkazy */}
                <Route path="timesheets" element={<TimesheetsListPage />} />
                <Route path="timesheets/:id" element={<TimesheetDetailPage />} />

                {/* admin výkazy */}
                <Route path="admin/timesheets" element={<AdminTimesheetsPage />} />

                {/* admin uživatelé */}
                <Route path="admin/users" element={<AdminUsersPage />} />

                {/* sklad */}
                <Route path="inventory" element={<InventoryPage />} />

                {/* reporty */}
                <Route path="reports/summary" element={<ReportsSummaryPage />} />
                <Route path="reports/users" element={<ReportsUsersPage />} />
                <Route path="reports/teams" element={<ReportsTeamsPage />} />

                {/* 404 v rámci /app */}
                <Route path="*" element={<NotFoundPage />} />
              </Route>
            </Route>

            {/* 404 mimo /app */}
            <Route path="*" element={<NotFoundPage />} />
          </Routes>
        </AuthProvider>
      </BrowserRouter>
    </ErrorBoundary>
  );
}