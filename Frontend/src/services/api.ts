import { publicRoutes } from "@/types/auth";
import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "localhost:7168/api",
  headers: {
    "Content-Type": "application/json",
    Accept: "application/json",
  },
  withCredentials: true,
});

api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  },
);

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      const isPublicRoute = publicRoutes.some((route) =>
        window.location.pathname.startsWith(route),
      );

      if (!isPublicRoute) {
        window.location.href = "/login";
      }
    }

    return Promise.reject(error);
  },
);

export default api;
