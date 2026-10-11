import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, ErrorDeApi, resultado, type Esquemas } from "./api/cliente";

export type Usuario = Esquemas["UsuarioActualDto"];

const CLAVE = ["sesion"];

// null significa que no hay sesión; undefined, que todavía se está averiguando
export function useSesion() {
  return useQuery({
    queryKey: CLAVE,
    queryFn: async (): Promise<Usuario | null> => {
      try {
        return await resultado(api.GET("/api/auth/yo"));
      } catch (error) {
        if (error instanceof ErrorDeApi && error.estado === 401) return null;
        throw error;
      }
    },
    staleTime: 5 * 60_000,
    retry: false,
  });
}

export function useEntrar() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: (credenciales: { email: string; password: string }) =>
      resultado(api.POST("/api/auth/login", { body: credenciales })),
    onSuccess: (usuario) => cache.setQueryData(CLAVE, usuario),
  });
}

export function useSalir() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: () => resultado(api.POST("/api/auth/logout")),
    // Se borra todo lo que se había consultado con la sesión que termina
    onSettled: () => {
      cache.clear();
      cache.setQueryData(CLAVE, null);
    },
  });
}

export const NOMBRE_DE_ROL: Record<Usuario["rol"], string> = {
  admin: "Administrador",
  laboratorista: "Laboratorista",
  docente: "Docente",
};
