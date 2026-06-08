import * as React from "react";
import { cn } from "@/lib/utils";

const variants = {
  neutral: "border-white/10 bg-white/5 text-slate-200",
  success: "border-emerald-400/30 bg-emerald-500/10 text-emerald-300",
  warning: "border-amber-400/30 bg-amber-500/10 text-amber-300",
  danger: "border-rose-400/30 bg-rose-500/10 text-rose-300",
  info: "border-cyan-400/30 bg-cyan-500/10 text-cyan-300",
} as const;

export function Badge({
  className,
  variant = "neutral",
  ...props
}: React.HTMLAttributes<HTMLSpanElement> & { variant?: keyof typeof variants }) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full border px-3 py-1 text-xs font-medium uppercase tracking-[0.18em]",
        variants[variant],
        className,
      )}
      {...props}
    />
  );
}
