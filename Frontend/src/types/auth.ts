export type UserRole = "Aluno" | "Professor" | "Administrador" | "Secretaria";

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  institutionId?: string;
  institutionName?: string;
  twoFactorEnabled: boolean;
}

export interface LoginCredentials {
  email: string;
  password: string;
  twoFactorCode?: string;
}

export interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginCredentials) => Promise<LoginResponse>;
  logout: () => Promise<void>;
  syncAuth: () => Promise<void>;
}
export interface LoginResponse {
  requiresPasswordChange?: boolean;
  requiresTwoFactor?: boolean;
  message?: string;
  user?: User;
}
