// src/pages/Home.tsx
import { Link } from "react-router-dom";

export default function Home() {
  return (
    <div className="min-h-dvh flex items-center justify-center bg-gray-50 p-6">
      <div className="card w-full max-w-xl p-6 space-y-4 text-center">
        <h1 className="text-2xl font-semibold text-gray-900">SunSkog – přehled</h1>
        <p className="text-gray-600">Zatím prázdná domácí stránka.</p>
        <div className="flex items-center justify-center gap-3">
          <Link to="/login" className="btn">Přejít na přihlášení</Link>
          <a
            href={`${import.meta.env.VITE_API_URL || "http://localhost:5250"}/swagger`}
            className="btn-outline"
            target="_blank"
          >
            Otevřít Swagger
          </a>
        </div>
      </div>
    </div>
  );
}