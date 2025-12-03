import { Link, useNavigate } from "react-router-dom";
import { getToken, logout } from "@/lib/auth";

export default function Nav() {
  const isAuth = !!getToken();
  const navigate = useNavigate();

  return (
    <header className="border-b bg-white">
      <div className="container-page flex h-14 items-center justify-between">
        <Link to="/" className="font-semibold text-[--color-brand]">
          SunSkog
        </Link>

        {isAuth ? (
          <div className="flex items-center gap-2">
            <Link to="/" className="btn-outline">Moje výkazy</Link>
            <button
              className="btn"
              onClick={() => {
                logout();
                navigate("/login");
              }}
            >
              Odhlásit
            </button>
          </div>
        ) : (
          <Link to="/login" className="btn">Přihlásit</Link>
        )}
      </div>
    </header>
  );
}