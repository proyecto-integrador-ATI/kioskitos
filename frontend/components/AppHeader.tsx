"use client";

import Link from "next/link";
import { clearToken } from "../services/auth";

export function AppHeader() {
  function logout() {
    clearToken();
    window.location.replace("/login");
  }

  return (
    <header className="bg-green">
      <div className="mx-auto flex w-full max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-4 md:px-[72px]">
        <Link href="/home" className="border-[3px] border-red bg-cream px-2">
          <span className="text-header2 text-blue">KIOSKITOS</span>
        </Link>
        <nav className="flex items-center gap-3">
          <Link href="/profile" className="text-normal text-cream hover:underline">
            Mi perfil
          </Link>
          <button
            type="button"
            onClick={logout}
            className="rounded-full border border-cream px-4 py-2 text-normal text-cream hover:bg-cream hover:text-green"
          >
            Cerrar sesión
          </button>
        </nav>
      </div>
    </header>
  );
}
