import { useEffect, useState } from "react";
import type { LoginCredentials, LoginResponse, User } from "../types/auth";
import api from "../services/api";
import { AuthContext } from "./AuthContext";

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        let isMounted = true;

        const checkAuthStatus = async () => {
            try {
                const response = await api.get<User>('/auth/me');
                if (isMounted) {
                    setUser(response.data);
                }
            } catch {
                if (isMounted) {
                    setUser(null);
                }
            } finally {
                if (isMounted) {
                    setIsLoading(false);
                }
            }
        };

        checkAuthStatus();

        return () => {
            isMounted = false; // Cancela a atualização de estado se o componente desmontar
        };
    }, []);

    const login = async (credentials: LoginCredentials): Promise<LoginResponse> => {
        const response = await api.post<LoginResponse>('/auth/login', credentials);

        if (!response.data.requiresTwoFactor && response.data.user) {
            setUser(response.data.user);
        }

        return response.data;
    };

    const logout = async () => {
        try {
            await api.post('/auth/logout');
        } finally {
            setUser(null);
            window.location.href = '/login';
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
            }}
        >
            {children}
        </AuthContext.Provider>
    );
};