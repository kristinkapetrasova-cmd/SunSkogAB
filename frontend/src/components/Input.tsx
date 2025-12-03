import { InputHTMLAttributes } from "react";
import clsx from "clsx";

export function Label({ htmlFor, children }: {htmlFor?: string; children: React.ReactNode}) {
  return <label htmlFor={htmlFor} className="block text-sm font-medium mb-1">{children}</label>;
}

export default function Input(props: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      {...props}
      className={clsx(
        "w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none",
        "focus:ring-2 focus:ring-brand-500/30 focus:border-brand-500",
        props.className
      )}
    />
  );
}