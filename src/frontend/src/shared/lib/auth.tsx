"use client";

import { createContext, useContext, useState, useEffect, ReactNode } from "react";

interface Branch {
  id: string;
  name: string;
}

interface User {
  id: string;
  username: string;
  role: string;
  tenantId: string;
  branches: Branch[];
}

interface AuthState {
  token: string | null;
  user: User | null;
}

interface AuthContextType extends AuthState {
  login: (token: string, user: User) => void;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<AuthState>({ token: null, user: null });

  useEffect(() => {
    const stored = localStorage.getItem("auth");
    if (stored) {
      try {
        setAuth(JSON.parse(stored));
      } catch {
        localStorage.removeItem("auth");
      }
    }
  }, []);

  const login = (token: string, user: User) => {
    const state = { token, user };
    setAuth(state);
    localStorage.setItem("auth", JSON.stringify(state));
  };

  const logout = () => {
    setAuth({ token: null, user: null });
    localStorage.removeItem("auth");
  };

  return (
    <AuthContext.Provider
      value={{ ...auth, login, logout, isAuthenticated: !!auth.token }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
