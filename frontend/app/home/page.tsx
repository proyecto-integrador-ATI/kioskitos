"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AppHeader } from "../../components/AppHeader";
import { clearToken, getToken } from "../../services/auth";
import { getMyProfile, type UserProfile } from "../../services/users";

const SHORTCUTS = [
  {
    href: "/clients",
    title: "Mis clientes",
    description: "Consultá tus clientes y sus datos de contacto.",
  },
  {
    href: "/projects",
    title: "Mis proyectos",
    description: "Mirá todos tus proyectos, de todos tus clientes.",
  },
  {
    href: "/clients/new",
    title: "Registrar cliente",
    description: "Cargá un cliente nuevo con sus personas de contacto.",
  },
  {
    href: "/profile",
    title: "Mi perfil",
    description: "Revisá y editá tu nombre, tu rubro y tu tarifa por hora.",
  },
  {
    href: "/calendar",
    title: "Google Calendar",
    description: "Conectá tu cuenta para crear el calendario “Kioskitos”.",
  },
];

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

  if (!me) {
    return (
      <main className="flex min-h-screen flex-1 items-center justify-center bg-ash">
        <p className="text-normal text-green">Cargando…</p>
      </main>
    );
  }

  return (
    <div className="flex min-h-screen flex-1 flex-col bg-ash">
      <AppHeader />

      <main className="mx-auto w-full max-w-6xl flex-1 px-6 py-12 md:px-[72px]">
        <h1 className="text-header2 text-green">Hola, {me.name}</h1>
        <p className="mt-2 text-normal text-green">{me.email}</p>

        <section aria-labelledby="shortcuts" className="mt-10">
          <h2 id="shortcuts" className="text-special-header text-green">
            ¿Qué querés hacer hoy?
          </h2>
          <ul className="mt-6 grid gap-6 md:grid-cols-3">
            {SHORTCUTS.map((s) => (
              <li key={s.href}>
                <Link
                  href={s.href}
                  className="flex h-full flex-col gap-2 rounded-[20px] border border-green bg-cream p-6 text-green transition-colors hover:bg-white"
                >
                  <span className="text-normal font-bold">{s.title}</span>
                  <span className="text-small">{s.description}</span>
                </Link>
              </li>
            ))}
          </ul>
        </section>
      </main>
    </div>
  );
}
