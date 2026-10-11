import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // Para probar desde un teléfono en la misma red
    host: true,
    // En desarrollo las llamadas a la API salen del mismo origen, igual que en producción,
    // así la cookie de sesión funciona sin configurar CORS
    proxy: { "/api": "http://localhost:5270" },
  },
});
