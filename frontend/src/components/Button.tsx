import { ButtonHTMLAttributes } from "react";
import clsx from "clsx";

type Props = ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary"|"secondary"|"ghost", loading?: boolean };

export default function Button({ variant="primary", loading=false, className, children, ...rest }: Props) {
  const base = "inline-flex items-center justify-center rounded-xl px-4 py-2 text-sm font-medium transition shadow-sm";
  const styles = {
    primary: "bg-brand-600 hover:bg-brand-700 text-white",
    secondary: "bg-white hover:bg-slate-50 text-slate-900 ring-1 ring-slate-200",
    ghost: "bg-transparent hover:bg-slate-100 text-slate-700"
  }[variant];
  return (
    <button className={clsx(base, styles, className)} disabled={loading || rest.disabled} {...rest}>
      {loading ? "…" : children}
    </button>
  );
}