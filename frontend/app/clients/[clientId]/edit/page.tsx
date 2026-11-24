"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { AppHeader } from "../../../../components/AppHeader";
import { ClientForm, toFormValues, type ClientFormData } from "../../../../components/ClientForm";
import { ApiError } from "../../../../services/apiError";
import { getToken } from "../../../../services/auth";
import { getClient, updateClient, type Client } from "../../../../services/clients";

// version changes on every load, so the form is remounted with the latest data.
type State =
  | { kind: "loading" }
  | { kind: "loaded"; clientId: string; client: Client; version: number }
  | { kind: "notFound"; clientId: string }
  | { kind: "error"; clientId: string };

function EditClient() {
  const router = useRouter();
  const { clientId } = useParams<{ clientId: string }>();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [attempt, setAttempt] = useState(0);

  // Runs again every time the hidden page becomes visible (cacheComponents),
  // so the form never shows stale data or unsaved edits from a previous visit.
  useEffect(() => {
    if (!getToken()) {
      router.replace("/login");
      return;
    }
    let current = true;
    getClient(clientId)
      .then((client) => {
        if (!current) return;
        setState((prev) => ({
          kind: "loaded",
          clientId,
          client,
          version: prev.kind === "loaded" ? prev.version + 1 : 0,
        }));
      })
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

  async function save(data: ClientFormData) {
    await updateClient(clientId, data);
    router.push(`/clients/${clientId}`);
  }

  const view = state.kind !== "loading" && state.clientId === clientId ? state : { kind: "loading" as const };

  return (
    <>
      <Link href={`/clients/${clientId}`} className="text-small text-green hover:underline">
        ← Volver al cliente
      </Link>
      <h1 className="mt-4 text-header2 text-green">Editar cliente</h1>

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

        {view.kind === "loaded" && (
          <ClientForm
            key={view.version}
            defaultValues={toFormValues(view.client)}
            submitLabel="Guardar cambios"
            submittingLabel="Guardando…"
            failureMessage="No pudimos guardar los cambios. Probá de nuevo."
            onSubmit={save}
          />
        )}
      </div>
    </>
  );
}

export default function EditClientPage() {
  return (
    <div className="flex min-h-screen flex-1 flex-col bg-ash">
      <AppHeader />
      <main className="mx-auto w-full max-w-3xl flex-1 px-6 py-12">
        {/* useParams needs a Suspense boundary because cacheComponents is enabled. */}
        <Suspense fallback={<p className="text-normal text-green">Cargando…</p>}>
          <EditClient />
        </Suspense>
      </main>
    </div>
  );
}
