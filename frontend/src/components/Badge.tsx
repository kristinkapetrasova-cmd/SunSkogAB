import clsx from "clsx";

export default function Badge({ children, color="slate" }: {children: React.ReactNode; color?: "slate"|"green"|"amber"|"red"|"blue"}) {
  const colorMap = {
    slate: "bg-slate-100 text-slate-700",
    green: "bg-green-100 text-green-700",
    amber: "bg-amber-100 text-amber-800",
    red: "bg-red-100 text-red-700",
    blue: "bg-blue-100 text-blue-700"
  }[color];
  return <span className={clsx("inline-flex items-center px-2 py-0.5 rounded-lg text-xs font-medium", colorMap)}>{children}</span>;
}