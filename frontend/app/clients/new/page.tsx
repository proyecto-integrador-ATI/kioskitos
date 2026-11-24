"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useSyncExternalStore } from "react";
import { AppHeader } from "../../../components/AppHeader";
import { ClientForm, EMPTY_CLIENT_FORM, type ClientFormData } from "../../../components/ClientForm";
import { getToken } from "../../../services/auth";
import { registerClient } from "../../../services/clients";

const noSubscribe = () => () => {};

export default function NewClientPage() {
  const router = useRouter();
  const isClient = useSyncExternalStore(noSubscribe, () => true, () => false);

  useEffect(() => {
    if (!getToken()) router.replace("/login");
  }, [router]);

  async function save(data: ClientFormData) {
    const client = await registerClient(data);
    return (
      <>
        Registramos a {client.name}.{" "}
        <Link href={`/clients/${client.id}`} className="font-bold underline">
          Ver cliente
        </Link>
      </>
    );
  }

  return (
    <div className="flex min-h-screen flex-1 flex-col bg-ash">
      <AppHeader />

      <main className="mx-auto w-full max-w-3xl flex-1 px-6 py-12">
        <Link href="/clients" className="text-small text-green hover:underline">
          ← Volver a clientes
        </Link>
        <h1 className="mt-4 text-header2 text-green">Registrar cliente</h1>
        {/* useFieldArray creates random ids, so the form must not be prerendered. */}
        {isClient && getToken() !== null && (
          <ClientForm
            defaultValues={EMPTY_CLIENT_FORM}
            submitLabel="Registrar cliente"
            submittingLabel="Registrando…"
            failureMessage="No pudimos registrar el cliente. Probá de nuevo."
            onSubmit={save}
          />
        )}
      </main>
    </div>
  );
}
