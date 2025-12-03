import { Link, NavLink, Outlet } from "react-router-dom";

export default function Layout() {
  return (
    <div className="min-h-dvh bg-gray-50 text-gray-900">
      <header className="border-b bg-white">
        <div className="container-page flex items-center justify-between">
          <Link to="/timesheets" className="flex items-center gap-2">
            <div className="size-8 rounded-lg bg-[--color-brand]" aria-hidden />
            <span className="font-semibold">SunSkog</span>
          </Link>
          <nav className="flex items-center gap-4">
            <NavLink
              to="/timesheets"
              className={({ isActive }) =>
                `text-sm ${isActive ? "text-[--color-brand] font-medium" : "text-gray-600 hover:text-gray-900"}`
              }
            >
              Výkazy
            </NavLink>
            <button
              className="btn-outline"
              onClick={() => {
                localStorage.removeItem("token");
                location.href = "/login";
              }}
            >
              Odhlásit
            </button>
          </nav>
        </div>
      </header>

      <main className="container-page">
        <Outlet />
      </main>
    </div>
  );
}