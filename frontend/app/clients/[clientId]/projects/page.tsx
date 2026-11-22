"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { Suspense, useEffect } from "react";
import { ClientProjectsList } from "../../../../components/ClientProjectsList";
import { getToken } from "../../../../services/auth";

function ClientProjects() {
  const router = useRouter();
  const { clientId } = useParams<{ clientId: string }>();

  useEffect(() => {
    if (!getToken()) router.replace("/login");
  }, [router]);

  return (
    <main className="mx-auto w-full max-w-3xl p-8">
      <Link href="/home" className="text-sm text-slate-600 hover:underline">
        ← Volver al inicio
      </Link>
      <div className="mt-4 flex items-center justify-between">
        <h1 className="text-2xl font-bold">Proyectos del cliente</h1>
        <Link
          href={`/clients/${clientId}/projects/new`}
          className="rounded-md bg-slate-900 px-4 py-2 text-sm text-white hover:bg-slate-700"
        >
          Nuevo proyecto
        </Link>
      </div>
      <div className="mt-6">
        <ClientProjectsList clientId={clientId} />
      </div>
    </main>
  );
}

export default function ClientProjectsPage() {
  // useParams needs a Suspense boundary because cacheComponents is enabled.
  return (
    <Suspense fallback={<p className="p-8 text-slate-600">Cargando…</p>}>
      <ClientProjects />
    </Suspense>
  );
}
