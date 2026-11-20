"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { clearToken, getToken } from "../../services/auth";
import { getMyProfile, type UserProfile } from "../../services/users";

export default function HomePage() {
  const router = useRouter();
  const [me, setMe] = useState<UserProfile | null>(null);

  useEffect(() => {
    const token = getToken();
    if (!token) {
      router.replace("/login");
      return;
    }
    getMyProfile()
      .then(setMe)
      .catch(() => {
        clearToken(); // expired or invalid token
        router.replace("/login");
      });
  }, [router]);

  function logout() {
    clearToken();
    window.location.replace("/login");
  }

  if (!me) return <p className="p-8 text-slate-600">Cargando…</p>;

  return (
    <main className="mx-auto w-full max-w-3xl p-8">
      <h1 className="text-2xl font-bold">Hola, {me.name}</h1>
      <p className="mt-2 text-slate-600">{me.email}</p>
      <Link
        href="/profile"
        className="mt-6 block w-fit rounded-md border border-slate-300 px-4 py-2 text-sm hover:bg-slate-50"
      >
        Mi perfil
      </Link>
      <Link
        href="/calendar"
        className="mt-6 block w-fit rounded-md bg-slate-900 px-4 py-2 text-sm text-white hover:bg-slate-700"
      >
        Conectar Google Calendar
      </Link>
      <button
        onClick={logout}
        className="mt-6 rounded-md border border-slate-300 px-4 py-2 text-sm hover:bg-slate-50"
      >
        Cerrar sesión
      </button>
    </main>
  );
}