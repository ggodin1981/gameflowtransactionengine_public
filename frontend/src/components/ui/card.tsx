import * as React from "react";
import { cn } from "@/lib/utils";

export function Card({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "rounded-3xl border border-white/10 bg-slatepanel/80 p-5 shadow-panel backdrop-blur-sm",
        className,
      )}
      {...props}
    />
  );
}
