import { PropsWithChildren } from "react";
import clsx from "clsx";

export function Card({ children, className }: PropsWithChildren<{className?: string}>) {
  return <div className={clsx("bg-white rounded-2xl shadow-card", className)}>{children}</div>;
}

export function CardBody({ children, className }: PropsWithChildren<{className?: string}>) {
  return <div className={clsx("p-6", className)}>{children}</div>;
}

export function CardHeader({ title, subtitle }: {title: string; subtitle?: string}) {
  return (
    <div className="px-6 pt-6">
      <h3 className="text-lg font-semibold">{title}</h3>
      {subtitle && <p className="text-sm text-slate-500 mt-1">{subtitle}</p>}
    </div>
  );
}