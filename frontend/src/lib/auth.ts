import { useSyncExternalStore } from "react";

function getSnapshot() {
  return !!localStorage.getItem("token");
}

export function useAuth() {
  const isAuthed = useSyncExternalStore(
    (cb) => {
      const handler = () => cb();
      window.addEventListener("storage", handler);
      return () => window.removeEventListener("storage", handler);
    },
    getSnapshot,
    getSnapshot
  );
  return { isAuthed };
}