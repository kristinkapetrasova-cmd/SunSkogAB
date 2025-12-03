import { Outlet } from "react-router-dom";

export default function AuthLayout() {
  return (
    <div className="min-h-dvh grid place-items-center p-6">
      <div className="w-full max-w-md">
        <div className="text-center mb-6">
          <div className="text-2xl font-bold">SunSkog</div>
          <div className="text-slate-500 text-sm">Interní systém</div>
        </div>
        <Outlet />
      </div>
    </div>
  );
}