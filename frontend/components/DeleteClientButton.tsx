"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { ApiError } from "../services/apiError";
import { deleteClient } from "../services/clients";
import { setNotice } from "../services/notice";

type Props = { clientId: string; clientName: string };

function errorMessage(err: unknown) {
  if (err instanceof ApiError && err.status === 409) {
    return "No podés eliminar este cliente porque tiene proyectos asociados.";
  }
  if (err instanceof ApiError && err.status === 404) {
    return "Este cliente ya no existe.";
  }
  if (err instanceof ApiError && err.status === 401) {
    return "Tu sesión venció. Volvé a iniciar sesión.";
  }
  return "No pudimos eliminar el cliente. Probá de nuevo.";
}

export function DeleteClientButton({ clientId, clientName }: Props) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function close() {
    if (deleting) return;
    setOpen(false);
    setError(null);
  }

  async function confirm() {
    if (deleting) return;
    setDeleting(true);
    setError(null);
    try {
      await deleteClient(clientId);
      setNotice(`Eliminamos a ${clientName}.`);
      setOpen(false);
      // replace, so going back does not return to the deleted client.
      router.replace("/clients");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setDeleting(false);
    }
  }

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="rounded-full border border-red px-4 py-2 text-small font-medium text-red hover:bg-white"
      >
        Eliminar
      </button>

      {open && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-6"
          onKeyDown={(e) => e.key === "Escape" && close()}
        >
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="delete-client-title"
            className="w-full max-w-md rounded-[20px] border border-green bg-cream p-6 text-green"
          >
            <h2 id="delete-client-title" className="text-normal font-bold">
              ¿Eliminar a {clientName}?
            </h2>
            <p className="mt-2 text-small">
              Se eliminarán también sus personas de contacto. Esta acción no se puede deshacer.
            </p>

            {error && (
              <p role="alert" className="mt-3 rounded-[10px] border border-red bg-white p-3 text-small text-red">
                {error}
              </p>
            )}

            <div className="mt-6 flex justify-end gap-3">
              <button
                type="button"
                onClick={close}
                disabled={deleting}
                autoFocus
                className="rounded-full border border-green px-4 py-2 text-small font-medium text-green hover:bg-white disabled:opacity-50"
              >
                Cancelar
              </button>
              <button
                type="button"
                onClick={confirm}
                disabled={deleting}
                className="rounded-full bg-red px-4 py-2 text-small font-medium text-white hover:opacity-90 disabled:opacity-50"
              >
                {deleting ? "Eliminando…" : "Sí, eliminar"}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
