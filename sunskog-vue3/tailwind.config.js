/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        'sunskog-primary': '#870404',      // Tmavě červená - hlavní barva
        'sunskog-hover': '#ED3939',         // Světlejší červená pro hover
        'sunskog-dark': '#161616',          // Tmavě šedá
        'sunskog-yellow': '#FFF200',        // Žlutá (akcent)
        'sunskog-orange': '#F9B000',        // Oranžová (gradient)
        'sunskog-light': '#F4F4F4',         // Světle šedá
        // Aliasy pro zpětnou kompatibilitu
        'sunskog-green': '#870404',         // Mapuje na primární červenou
        'sunskog-blue': '#870404',          // Mapuje na primární červenou
      }
    },
  },
  plugins: [],
}
