// src/AuthContext.tsx
import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  ReactNode,
} from "react";
import { authToken } from "./lib/client";
import { login as apiLogin, me as apiMe } from "./lib/api";

export type AuthUser = {
  id: string;
  email: string;
  name?: string;
  roles: string[];
};

type AuthContextValue = {
  user: AuthUser | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshMe: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  const refreshMe = async () => {
    const token = authToken.get();
    if (!token) {
      setUser(null);
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      const me = await apiMe();
      setUser(me);
    } catch (err) {
      console.error("refreshMe() failed, clearing token", err);
      authToken.set(null);
      setUser(null);
    } finally {
      setLoading(false);
    }
  };

  const login = async (email: string, password: string) => {
    setLoading(true);
    try {
      // zavolá /auth/login, uloží token přes authToken.set(...)
      const res = await apiLogin(email, password);

      // použijeme user z login response, nebo si ho dotáhneme přes /auth/me
      if (res && res.user) {
        setUser({
          id: res.user.id,
          email: res.user.email,
          name: res.user.name,
          roles: res.user.roles ?? [],
        });
      } else {
        const me = await apiMe();
        setUser(me);
      }
    } catch (err: any) {
      console.error("AuthContext.login error:", err);
      // když login selže, pro jistotu smažeme token
      authToken.set(null);
      setUser(null);
      // vyhodíme rozumnou zprávu dál, aby ji LoginPage zobrazila
      throw new Error(
        err?.message || "Přihlášení se nezdařilo. Zkontroluj email a heslo."
      );
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    authToken.set(null);
    setUser(null);
  };

  useEffect(() => {
    // při načtení appky se zkusíme dotáhnout /auth/me
    refreshMe().catch((err) => {
      console.error("Initial refreshMe failed:", err);
    });
  }, []);

  const value: AuthContextValue = {
    user,
    loading,
    login,
    logout,
    refreshMe,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return ctx;
}