"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { AppHeader } from "../../../components/AppHeader";
import { ClientProjectsList } from "../../../components/ClientProjectsList";
import { ApiError } from "../../../services/apiError";
import { getToken } from "../../../services/auth";
import { getClient, type Client } from "../../../services/clients";

// Each state remembers which client it belongs to, so a stale detail is never shown for another id.
type State =
  | { kind: "loading" }
  | { kind: "loaded"; clientId: string; client: Client }
  | { kind: "notFound"; clientId: string }
  | { kind: "error"; clientId: string };

const EMPTY_VALUE = "—";

function ClientDetail() {
  const router = useRouter();
  const { clientId } = useParams<{ clientId: string }>();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [attempt, setAttempt] = useState(0);

  // Runs again every time the hidden page becomes visible (cacheComponents),
  // so the detail reflects the latest changes.
  useEffect(() => {
    if (!getToken()) {
      router.replace("/login");
      return;
    }
    let current = true;
    getClient(clientId)
      .then((client) => current && setState({ kind: "loaded", clientId, client }))
      .catch((e) => {
        if (!current) return;
        setState(
          e instanceof ApiError && e.status === 404
            ? { kind: "notFound", clientId }
            : { kind: "error", clientId },
        );
      });
    return () => {
      current = false;
    };
  }, [router, clientId, attempt]);

  function retry() {
    setState({ kind: "loading" });
    setAttempt((n) => n + 1);
  }

  const view = state.kind !== "loading" && state.clientId === clientId ? state : { kind: "loading" as const };

  return (
    <>
      <Link href="/clients" className="text-small text-green hover:underline">
        ← Volver a clientes
      </Link>

      <div className="mt-4">
        {view.kind === "loading" && (
          <p role="status" className="text-normal text-green">
            Cargando cliente…
          </p>
        )}

        {view.kind === "notFound" && (
          <p role="alert" className="text-normal text-red">
            No encontramos este cliente.
          </p>
        )}

        {view.kind === "error" && (
          <div role="alert" className="flex flex-col items-start gap-3">
            <p className="text-normal text-red">No pudimos cargar el cliente.</p>
            <button
              type="button"
              onClick={retry}
              className="rounded-full border border-green px-4 py-2 text-small font-medium text-green hover:bg-white"
            >
              Reintentar
            </button>
          </div>
        )}

        {view.kind === "loaded" && <ClientInfo client={view.client} />}
      </div>
    </>
  );
}

function ClientInfo({ client }: { client: Client }) {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <h1 className="text-header2 text-green">{client.name}</h1>
        <Link
          href={`/clients/${client.id}/edit`}
          className="rounded-full border border-green px-4 py-2 text-small font-medium text-green hover:bg-white"
        >
          Editar
        </Link>
      </div>

      <section className="rounded-[20px] border border-green bg-cream p-6 text-green">
        <h2 className="text-normal font-bold">Datos del cliente</h2>
        <dl className="mt-3 grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-small">
          <dt>Nombre</dt>
          <dd>{client.name}</dd>
          <dt>Rubro</dt>
          <dd>{client.industry ?? EMPTY_VALUE}</dd>
          <dt>Observaciones</dt>
          <dd className="whitespace-pre-line">{client.notes ?? EMPTY_VALUE}</dd>
        </dl>
      </section>

      <section className="rounded-[20px] border border-green bg-cream p-6 text-green">
        <h2 className="text-normal font-bold">Personas de contacto</h2>
        <ul className="mt-3 flex flex-col gap-3">
          {client.contacts.map((contact) => (
            <li key={contact.id} className="text-small">
              <p className="font-bold">{contact.name}</p>
              <p>
                <a href={`mailto:${contact.email}`} className="break-all hover:underline">
                  {contact.email}
                </a>
              </p>
              <p>
                <a href={`tel:${contact.phone.replace(/[^\d+]/g, "")}`} className="hover:underline">
                  {contact.phone}
                </a>
              </p>
            </li>
          ))}
        </ul>
      </section>

      <section className="rounded-[20px] border border-green bg-cream p-6 text-green">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <h2 className="text-normal font-bold">Proyectos</h2>
          <Link
            href={`/clients/${client.id}/projects/new`}
            className="rounded-full bg-green px-4 py-2 text-small text-cream hover:opacity-90"
          >
            Nuevo proyecto
          </Link>
        </div>
        <div className="mt-3">
          <ClientProjectsList clientId={client.id} />
        </div>
      </section>
    </div>
  );
}

export default function ClientPage() {
  return (
    <div className="flex min-h-screen flex-1 flex-col bg-ash">
      <AppHeader />
      <main className="mx-auto w-full max-w-3xl flex-1 px-6 py-12">
        {/* useParams needs a Suspense boundary because cacheComponents is enabled. */}
        <Suspense fallback={<p className="text-normal text-green">Cargando…</p>}>
          <ClientDetail />
        </Suspense>
      </main>
    </div>
  );
}
