import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import Button from "../components/Button";
import { logout } from "../lib/api";
import { useAuth } from "../AuthContext";
import { useLanguage } from "../LanguageContext";

export default function AppLayout() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { language, setLanguage, t } = useLanguage();

  // Bezpečná a robustní detekce rolí (case-insensitive)
  const roleSet = new Set((user?.roles ?? []).map(r => r.toLowerCase()));
  const isAdminLike = roleSet.has("admin") || roleSet.has("management");
  const canUseInventory = isAdminLike || roleSet.has("warehouse");

  return (
    <div className="min-h-dvh">
      <header className="bg-white border-b border-slate-200">
        <div className="container-app flex items-center justify-between h-14">
          <Link to="/app" className="font-semibold">SunSkog</Link>

          <nav className="flex items-center gap-4 text-sm">
            {/* Moje výkazy – pro všechny přihlášené */}
            <NavLink
              to="/app/timesheets"
              className={({ isActive }) => (isActive ? "text-brand-700 font-medium" : "text-slate-600")}
            >
              {t.myTimesheets}
            </NavLink>

            {/* Admin výkazy – jen Admin/Management */}
            {isAdminLike && (
              <NavLink
                to="/app/admin/timesheets"
                className={({ isActive }) => (isActive ? "text-brand-700 font-medium" : "text-slate-600")}
              >
                {t.adminTimesheets}
              </NavLink>
            )}

            {/* Správa uživatelů – jen Admin/Management */}
            {isAdminLike && (
              <NavLink
                to="/app/admin/users"
                className={({ isActive }) => (isActive ? "text-brand-700 font-medium" : "text-slate-600")}
              >
                {t.adminUsers}
              </NavLink>
            )}

            {/* Sklad – Admin/Management/Warehouse */}
            {canUseInventory && (
              <NavLink
                to="/app/inventory"
                className={({ isActive }) => (isActive ? "text-brand-700 font-medium" : "text-slate-600")}
              >
                {t.inventory}
              </NavLink>
            )}

            {/* Reporty – (případně lze omezit později) */}
            <NavLink
              to="/app/reports/summary"
              className={({ isActive }) => (isActive ? "text-brand-700 font-medium" : "text-slate-600")}
            >
              {t.reports}
            </NavLink>

            <div className="w-px h-4 bg-slate-300" />

            {/* Přepínač jazyků */}
            <div className="flex gap-1 items-center">
              <button
                onClick={() => setLanguage("cs")}
                className={`px-2 py-1 text-xs rounded ${
                  language === "cs" 
                    ? "bg-slate-800 text-white font-medium" 
                    : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                }`}
              >
                ČJ
              </button>
              <button
                onClick={() => setLanguage("en")}
                className={`px-2 py-1 text-xs rounded ${
                  language === "en" 
                    ? "bg-slate-800 text-white font-medium" 
                    : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                }`}
              >
                EN
              </button>
            </div>

            <div className="w-px h-4 bg-slate-300" />

            <span className="text-xs text-slate-500">{user?.email}</span>

            <Button
              variant="secondary"
              onClick={() => {
                logout();
                navigate("/login");
              }}
            >
              {t.logout}
            </Button>
          </nav>
        </div>
      </header>

      <main className="container-app py-6">
        <Outlet />
      </main>
    </div>
  );
}