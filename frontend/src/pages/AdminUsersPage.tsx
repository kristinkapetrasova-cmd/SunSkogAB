// src/pages/AdminUsersPage.tsx
import React, { useEffect, useState } from "react";
import {
  getUsers,
  createUser,
  updateUser,
  resetUserPassword,
  UserDto,
  UserRole,
} from "../api/usersApi";

const ALL_ROLES: UserRole[] = [
  "Employee",
  "TeamLead",
  "Accountant",
  "Management",
  "Warehouse",
  "Admin",
];

type StatusMessage = { type: "success" | "error"; text: string } | null;

const AdminUsersPage: React.FC = () => {
  const [users, setUsers] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [status, setStatus] = useState<StatusMessage>(null);

  // nový uživatel (form)
  const [newUser, setNewUser] = useState({
    email: "",
    password: "",
    name: "",
    roles: ["Employee"] as UserRole[],
  });

  // editace existujícího uživatele
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<{
    name: string;
    roles: UserRole[];
    lockout: boolean;
  }>({
    name: "",
    roles: [],
    lockout: false,
  });

  const showStatus = (msg: StatusMessage) => {
    setStatus(msg);
    if (msg) {
      setTimeout(() => setStatus(null), 4000);
    }
  };

  const loadUsers = async () => {
    setLoading(true);
    try {
      const data = await getUsers();
      setUsers(data);
    } catch (e: any) {
      console.error(e);
      showStatus({
        type: "error",
        text:
          e?.response?.data?.error ??
          e?.response?.data?.message ??
          "Nepodařilo se načíst uživatele.",
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadUsers();
  }, []);

  const handleCreateUser = async (ev: React.FormEvent) => {
    ev.preventDefault();
    showStatus(null);
    try {
      await createUser({
        email: newUser.email.trim(),
        password: newUser.password,
        name: newUser.name.trim() || undefined,
        roles: newUser.roles,
      });
      showStatus({ type: "success", text: "Uživatel byl vytvořen." });
      setNewUser({ email: "", password: "", name: "", roles: ["Employee"] });
      await loadUsers();
    } catch (e: any) {
      console.error(e);
      showStatus({
        type: "error",
        text:
          e?.response?.data?.error ??
          e?.response?.data?.message ??
          "Vytvoření uživatele se nezdařilo.",
      });
    }
  };

  const beginEdit = (u: UserDto) => {
    setEditingUserId(u.id);
    setEditForm({
      name: u.name ?? "",
      roles: (u.roles as UserRole[]) ?? [],
      lockout: u.isLockedOut,
    });
  };

  const cancelEdit = () => {
    setEditingUserId(null);
  };

  const handleSaveEdit = async (id: string) => {
    showStatus(null);
    try {
      await updateUser(id, {
        name: editForm.name.trim() || undefined,
        roles: editForm.roles,
        lockout: editForm.lockout,
        // teamId a teamRole zatím neřešíme
      });
      showStatus({ type: "success", text: "Uživatel byl upraven." });
      setEditingUserId(null);
      await loadUsers();
    } catch (e: any) {
      console.error(e);
      showStatus({
        type: "error",
        text:
          e?.response?.data?.error ??
          e?.response?.data?.message ??
          "Úprava uživatele se nezdařila.",
      });
    }
  };

  const toggleNewUserRole = (role: UserRole) => {
    setNewUser((prev) => {
      const has = prev.roles.includes(role);
      return {
        ...prev,
        roles: has ? prev.roles.filter((r) => r !== role) : [...prev.roles, role],
      };
    });
  };

  const toggleEditRole = (role: UserRole) => {
    setEditForm((prev) => {
      const has = prev.roles.includes(role);
      return {
        ...prev,
        roles: has ? prev.roles.filter((r) => r !== role) : [...prev.roles, role],
      };
    });
  };

  const handleResetPassword = async (id: string) => {
    const newPassword = window.prompt(
      "Zadej nové heslo pro tohoto uživatele (min. 6 znaků):"
    );
    if (!newPassword) return;
    if (newPassword.length < 6) {
      showStatus({ type: "error", text: "Heslo musí mít alespoň 6 znaků." });
      return;
    }

    try {
      await resetUserPassword(id, newPassword);
      showStatus({ type: "success", text: "Heslo bylo resetováno." });
    } catch (e: any) {
      console.error(e);
      showStatus({
        type: "error",
        text:
          e?.response?.data?.error ??
          e?.response?.data?.message ??
          "Reset hesla se nezdařil.",
      });
    }
  };

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-semibold mb-2">Správa uživatelů</h1>
      <p className="text-sm text-gray-600 mb-4">
        Vytvářej nové účty, upravuj role a zamykej / odemykej přístup do systému.
      </p>

      {status && (
        <div
          className={`border px-3 py-2 rounded text-sm ${
            status.type === "success"
              ? "bg-green-50 border-green-400 text-green-800"
              : "bg-red-50 border-red-400 text-red-800"
          }`}
        >
          {status.text}
        </div>
      )}

      {/* Formulář pro vytvoření nového uživatele */}
      <section className="border rounded-xl p-4 shadow-sm bg-white max-w-xl">
        <h2 className="text-lg font-medium mb-3">Nový uživatel</h2>
        <form onSubmit={handleCreateUser} className="space-y-3">
          <div>
            <label className="block text-sm font-medium mb-1">Email</label>
            <input
              type="email"
              required
              className="border rounded px-2 py-1 w-full"
              value={newUser.email}
              onChange={(e) =>
                setNewUser((prev) => ({ ...prev, email: e.target.value }))
              }
            />
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Heslo</label>
            <input
              type="password"
              required
              minLength={6}
              className="border rounded px-2 py-1 w-full"
              value={newUser.password}
              onChange={(e) =>
                setNewUser((prev) => ({ ...prev, password: e.target.value }))
              }
            />
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Jméno (zobrazované)</label>
            <input
              type="text"
              className="border rounded px-2 py-1 w-full"
              value={newUser.name}
              onChange={(e) =>
                setNewUser((prev) => ({ ...prev, name: e.target.value }))
              }
            />
          </div>

          <div>
            <span className="block text-sm font-medium mb-1">Role</span>
            <div className="flex flex-wrap gap-3 text-sm">
              {ALL_ROLES.map((role) => (
                <label key={role} className="inline-flex items-center gap-1">
                  <input
                    type="checkbox"
                    className="rounded border-gray-300"
                    checked={newUser.roles.includes(role)}
                    onChange={() => toggleNewUserRole(role)}
                  />
                  <span>{role}</span>
                </label>
              ))}
            </div>
          </div>

          <div className="pt-2">
            <button
              type="submit"
              className="px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-medium disabled:opacity-60"
              disabled={loading}
            >
              Vytvořit uživatele
            </button>
          </div>
        </form>
      </section>

      {/* Tabulka existujících uživatelů */}
      <section className="border rounded-xl p-4 shadow-sm bg-white">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-lg font-medium">Existující uživatelé</h2>
          {loading && <span className="text-xs text-gray-500">Načítám…</span>}
        </div>

        {users.length === 0 ? (
          <div className="text-sm text-gray-500">Zatím žádní uživatelé.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm border-collapse">
              <thead>
                <tr className="border-b bg-gray-50">
                  <th className="text-left py-2 px-2">Email</th>
                  <th className="text-left py-2 px-2">Jméno</th>
                  <th className="text-left py-2 px-2">Role</th>
                  <th className="text-left py-2 px-2">Tým</th>
                  <th className="text-left py-2 px-2">Stav</th>
                  <th className="text-right py-2 px-2">Akce</th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => {
                  const isEditing = editingUserId === u.id;

                  if (isEditing) {
                    return (
                      <tr key={u.id} className="border-b align-top">
                        <td className="py-2 px-2 align-middle">
                          <div className="font-mono text-xs">{u.email}</div>
                        </td>
                        <td className="py-2 px-2">
                          <input
                            type="text"
                            className="border rounded px-2 py-1 w-full text-sm"
                            value={editForm.name}
                            onChange={(e) =>
                              setEditForm((prev) => ({
                                ...prev,
                                name: e.target.value,
                              }))
                            }
                          />
                        </td>
                        <td className="py-2 px-2">
                          <div className="flex flex-wrap gap-2 text-xs">
                            {ALL_ROLES.map((role) => (
                              <label
                                key={role}
                                className="inline-flex items-center gap-1 bg-gray-50 border rounded px-2 py-1"
                              >
                                <input
                                  type="checkbox"
                                  className="rounded border-gray-300"
                                  checked={editForm.roles.includes(role)}
                                  onChange={() => toggleEditRole(role)}
                                />
                                <span>{role}</span>
                              </label>
                            ))}
                          </div>
                        </td>
                        <td className="py-2 px-2 text-xs text-gray-500">
                          {/* Týmy zatím neřešíme; zobrazíme jen existující info */}
                          {u.teamName ? (
                            <>
                              <div>{u.teamName}</div>
                              {u.teamRole && (
                                <div className="text-[11px] text-gray-500">
                                  role v týmu: {u.teamRole}
                                </div>
                              )}
                            </>
                          ) : (
                            <span className="italic text-gray-400">
                              žádný tým
                            </span>
                          )}
                        </td>
                        <td className="py-2 px-2 align-middle">
                          <label className="inline-flex items-center gap-1 text-xs">
                            <input
                              type="checkbox"
                              className="rounded border-gray-300"
                              checked={editForm.lockout}
                              onChange={(e) =>
                                setEditForm((prev) => ({
                                  ...prev,
                                  lockout: e.target.checked,
                                }))
                              }
                            />
                            <span>{editForm.lockout ? "Uzamčený" : "Aktivní"}</span>
                          </label>
                        </td>
                        <td className="py-2 px-2 text-right align-middle space-x-2">
                          <button
                            onClick={() => handleSaveEdit(u.id)}
                            className="px-3 py-1 rounded bg-emerald-600 text-white text-xs"
                          >
                            Uložit
                          </button>
                          <button
                            onClick={cancelEdit}
                            className="px-3 py-1 rounded bg-gray-200 text-gray-700 text-xs"
                          >
                            Zrušit
                          </button>
                        </td>
                      </tr>
                    );
                  }

                  return (
                    <tr key={u.id} className="border-b hover:bg-gray-50/70">
                      <td className="py-2 px-2">
                        <div className="font-mono text-xs">{u.email}</div>
                      </td>
                      <td className="py-2 px-2">
                        <div className="text-sm">{u.name}</div>
                      </td>
                      <td className="py-2 px-2">
                        <div className="flex flex-wrap gap-1 text-xs">
                          {u.roles && u.roles.length > 0 ? (
                            u.roles.map((r) => (
                              <span
                                key={r}
                                className="inline-flex items-center px-2 py-0.5 rounded-full border border-gray-300 bg-gray-50"
                              >
                                {r}
                              </span>
                            ))
                          ) : (
                            <span className="italic text-gray-400">bez role</span>
                          )}
                        </div>
                      </td>
                      <td className="py-2 px-2 text-xs text-gray-600">
                        {u.teamName ? (
                          <>
                            <div>{u.teamName}</div>
                            {u.teamRole && (
                              <div className="text-[11px] text-gray-500">
                                role v týmu: {u.teamRole}
                              </div>
                            )}
                          </>
                        ) : (
                          <span className="italic text-gray-400">žádný tým</span>
                        )}
                      </td>
                      <td className="py-2 px-2 text-xs">
                        {u.isLockedOut ? (
                          <span className="inline-flex items-center px-2 py-0.5 rounded-full bg-red-100 text-red-700">
                            Uzamčený
                          </span>
                        ) : (
                          <span className="inline-flex items-center px-2 py-0.5 rounded-full bg-green-100 text-green-700">
                            Aktivní
                          </span>
                        )}
                      </td>
                      <td className="py-2 px-2 text-right text-xs space-x-2">
                        <button
                          onClick={() => beginEdit(u)}
                          className="px-3 py-1 rounded bg-blue-600 text-white"
                        >
                          Upravit
                        </button>
                        <button
                          onClick={() => handleResetPassword(u.id)}
                          className="px-3 py-1 rounded bg-orange-500 text-white"
                        >
                          Reset hesla
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
};

export default AdminUsersPage;