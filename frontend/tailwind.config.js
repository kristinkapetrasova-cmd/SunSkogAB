/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        brand: {
          50:  "#eef8ff",
          100: "#d9efff",
          200: "#bce3ff",
          300: "#8fd1ff",
          400: "#5ab7ff",
          500: "#2b9aff",
          600: "#1580e8",
          700: "#0f66bd",
          800: "#0f5296",
          900: "#0f457a"
        }
      },
      borderRadius: {
        xl: "0.75rem",
        "2xl": "1rem"
      },
      boxShadow: {
        card: "0 6px 24px rgba(0,0,0,0.06)"
      }
    }
  },
  plugins: []
};