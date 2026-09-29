import { useEffect, useState } from "react";
import { publicRoutes, type LoginCredentials, type LoginResponse, type User } from "../types/auth";
import api from "../services/api";
import { AuthContext } from "./AuthContext";

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const syncAuth = async () => {
    try {
      const response = await api.get<User>("/auth/me");
      setUser(response.data);
    } catch {
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (!publicRoutes.includes(window.location.pathname)) {
      syncAuth();
    }
    else{
      setIsLoading(false);
    }
  }, []);

  const login = async (
    credentials: LoginCredentials,
  ): Promise<LoginResponse> => {
    const response = await api.post<LoginResponse>("/auth/login", credentials);

    if (!response.data.requiresTwoFactor && response.data.user) {
      setUser(response.data.user);
    }

    return response.data;
  };

  const logout = async () => {
    try {
      await api.post("/auth/logout");
    } finally {
      setUser(null);
      window.location.href = "/login";
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        logout,
        syncAuth,
      }}>
      {children}
    </AuthContext.Provider>
  );
};
