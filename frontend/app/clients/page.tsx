"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AppHeader } from "../../components/AppHeader";
import { getToken } from "../../services/auth";
import { listClients, type ClientSummary } from "../../services/clients";
import { takeNotice } from "../../services/notice";

type State =
  | { kind: "loading" }
  | { kind: "loaded"; clients: ClientSummary[] }
  | { kind: "error" };

const EMPTY_VALUE = "—";

export default function ClientsPage() {
  const router = useRouter();
  const [state, setState] = useState<State>({ kind: "loading" });

  const [attempt, setAttempt] = useState(0);
  const [notice, setNoticeMessage] = useState<string | null>(null);

  // cacheComponents hides this page instead of unmounting it, and the effect runs again
  // every time it becomes visible: that refetch is what shows clients registered or deleted meanwhile.
  // The previous list stays on screen while it reloads.
  useEffect(() => {
    if (!getToken()) {
      router.replace("/login");
      return;
    }
    let current = true;
    const message = takeNotice();
    listClients()
      .then((clients) => {
        if (!current) return;
        setState({ kind: "loaded", clients });
        setNoticeMessage(message);
      })
      .catch(() => {
        if (!current) return;
        setState({ kind: "error" });
        setNoticeMessage(message);
      });
    return () => {
      current = false;
    };
  }, [router, attempt]);

  function retry() {
    setState({ kind: "loading" });
    setAttempt((n) => n + 1);
  }

  return (
    <div className="flex min-h-screen flex-1 flex-col bg-ash">
      <AppHeader />

      <main className="mx-auto w-full max-w-6xl flex-1 px-6 py-12 md:px-[72px]">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <h1 className="text-header2 text-green">Clientes</h1>
          <Link
            href="/clients/new"
            className="rounded-full bg-green px-6 py-3 text-normal text-cream hover:opacity-90"
          >
            Registrar cliente
          </Link>
        </div>

        {notice && (
          <p role="status" className="mt-6 rounded-[10px] border border-green bg-white p-3 text-normal text-green">
            {notice}
          </p>
        )}

        <div className="mt-8">
          {state.kind === "loading" && (
            <p role="status" className="text-normal text-green">
              Cargando clientes…
            </p>
          )}

          {state.kind === "error" && (
            <div role="alert" className="flex flex-col items-start gap-3">
              <p className="text-normal text-red">No pudimos cargar los clientes.</p>
              <button
                type="button"
                onClick={retry}
                className="rounded-full border border-green px-4 py-2 text-small font-medium text-green hover:bg-white"
              >
                Reintentar
              </button>
            </div>
          )}

          {state.kind === "loaded" && state.clients.length === 0 && (
            <div className="flex flex-col items-start gap-3 rounded-[20px] border border-green bg-cream p-6">
              <p className="text-normal text-green">Todavía no registraste clientes.</p>
              <Link href="/clients/new" className="text-normal font-bold text-green hover:underline">
                Registrá tu primer cliente
              </Link>
            </div>
          )}

          {state.kind === "loaded" && state.clients.length > 0 && (
            <ul className="grid gap-4 md:grid-cols-2">
              {state.clients.map((client) => (
                <li key={client.id}>
                  <Link
                    href={`/clients/${client.id}`}
                    className="flex h-full flex-col gap-2 rounded-[20px] border border-green bg-cream p-6 text-green hover:bg-white"
                  >
                    <span className="text-normal font-bold">{client.name}</span>
                    <dl className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-small">
                      <dt>Contacto</dt>
                      <dd>{client.primaryContact?.name ?? EMPTY_VALUE}</dd>
                      <dt>Email</dt>
                      <dd className="break-all">{client.primaryContact?.email ?? EMPTY_VALUE}</dd>
                      <dt>Teléfono</dt>
                      <dd>{client.primaryContact?.phone ?? EMPTY_VALUE}</dd>
                      <dt>Rubro</dt>
                      <dd>{client.industry ?? EMPTY_VALUE}</dd>
                    </dl>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </div>
      </main>
    </div>
  );
}
