"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { apiFetch } from "../../services/api";
import { clearToken, getToken } from "../../services/auth";

type Me = { userId: string; email: string; name: string };

export default function HomePage() {
  const router = useRouter();
  const [me, setMe] = useState<Me | null>(null);

  useEffect(() => {
    const token = getToken();
    if (!token) {
      router.replace("/login");
      return;
    }
    apiFetch<Me>("/api/auth/me", { headers: { Authorization: `Bearer ${token}` } })
      .then(setMe)
      .catch(() => {
        clearToken(); // token vencido o inválido
        router.replace("/login");
      });
  }, [router]);

  function logout() {
    clearToken();
    router.replace("/login");
  }

  if (!me) return <p className="p-8 text-slate-600">Cargando…</p>;

  return (
    <main className="mx-auto w-full max-w-3xl p-8">
      <h1 className="text-2xl font-bold">Hola, {me.name}</h1>
      <p className="mt-2 text-slate-600">{me.email}</p>
      <button
        onClick={logout}
        className="mt-6 rounded-md border border-slate-300 px-4 py-2 text-sm hover:bg-slate-50"
      >
        Cerrar sesión
      </button>
    </main>
  );
}