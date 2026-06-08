import type { Config } from "tailwindcss";

export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        midnight: "#09111f",
        slatepanel: "#0f1a2d",
        steel: "#15243c",
        highlight: "#4ade80",
        warning: "#f59e0b",
        danger: "#f87171",
        cyanpulse: "#22d3ee",
      },
      fontFamily: {
        sans: ["IBM Plex Sans", "sans-serif"],
        display: ["Space Grotesk", "sans-serif"],
      },
      boxShadow: {
        panel: "0 24px 60px rgba(2, 12, 27, 0.35)",
      },
      backgroundImage: {
        "grid-fade":
          "linear-gradient(rgba(255,255,255,0.05) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.05) 1px, transparent 1px)",
      },
    },
  },
  plugins: [],
} satisfies Config;
