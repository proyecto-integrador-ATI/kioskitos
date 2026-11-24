"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useLayoutEffect, useState, useSyncExternalStore } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { z } from "zod";
import { AppHeader } from "../../../components/AppHeader";
import { ApiError } from "../../../services/apiError";
import { getToken } from "../../../services/auth";
import { registerClient } from "../../../services/clients";

const optional = (max: number, message: string) =>
  z
    .string()
    .trim()
    .max(max, message)
    .transform((v) => v || null);

const isValidPhone = (v: string) => {
  const digits = v.replace(/\D/g, "").length;
  return /^\+?[\d\s()-]+$/.test(v) && digits >= 8 && digits <= 15;
};

const contactSchema = z.object({
  name: z.string().trim().min(1, "El nombre es obligatorio.").max(200, "Máximo 200 caracteres."),
  email: z
    .string()
    .trim()
    .min(1, "El email es obligatorio.")
    .max(320, "Máximo 320 caracteres.")
    .pipe(z.email("Ingresá un email válido.")),
  phone: z
    .string()
    .trim()
    .min(1, "El teléfono es obligatorio.")
    .max(30, "Máximo 30 caracteres.")
    .refine(isValidPhone, "Ingresá un teléfono válido, por ejemplo 099 123 456 o +598 2 123 4567."),
});

const clientSchema = z.object({
  name: z.string().trim().min(1, "El nombre es obligatorio.").max(200, "Máximo 200 caracteres."),
  industry: optional(100, "Máximo 100 caracteres."),
  notes: optional(2000, "Máximo 2000 caracteres."),
  contacts: z.array(contactSchema).min(1, "Agregá al menos una persona de contacto."),
});

type FormInput = z.input<typeof clientSchema>;
type FormOutput = z.output<typeof clientSchema>;

type Result = { kind: "success"; id: string; name: string } | { kind: "error"; message: string } | null;

const EMPTY_CONTACT = { name: "", email: "", phone: "" };
const EMPTY_FORM: FormInput = { name: "", industry: "", notes: "", contacts: [EMPTY_CONTACT] };

const inputClass =
  "mt-1 w-full rounded-[10px] border border-green bg-white px-3 py-2 text-normal text-green";
const labelClass = "text-small font-medium text-green";
const errorClass = "mt-1 text-small text-red";

const noSubscribe = () => () => {};

export default function NewClientPage() {
  const router = useRouter();
  const isClient = useSyncExternalStore(noSubscribe, () => true, () => false);

  useEffect(() => {
    if (!getToken()) router.replace("/login");
  }, [router]);

  return (
    <div className="flex min-h-screen flex-1 flex-col bg-ash">
      <AppHeader />

      <main className="mx-auto w-full max-w-3xl flex-1 px-6 py-12">
        <Link href="/clients" className="text-small text-green hover:underline">
          ← Volver a clientes
        </Link>
        <h1 className="mt-4 text-header2 text-green">Registrar cliente</h1>
        {/* useFieldArray creates random ids, so the form must not be prerendered. */}
        {isClient && getToken() !== null && <ClientForm />}
      </main>
    </div>
  );
}

function ClientForm() {
  const [result, setResult] = useState<Result>(null);

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormInput, unknown, FormOutput>({
    resolver: zodResolver(clientSchema),
    defaultValues: EMPTY_FORM,
  });
  const contacts = useFieldArray({ control, name: "contacts" });

  // cacheComponents hides the page instead of unmounting it.
  useLayoutEffect(() => () => setResult(null), []);

  async function onSubmit(data: FormOutput) {
    setResult(null);
    try {
      const client = await registerClient(data);
      reset(EMPTY_FORM);
      setResult({ kind: "success", id: client.id, name: client.name });
    } catch (err) {
      const message =
        err instanceof ApiError && err.status === 400
          ? "Revisá los datos ingresados."
          : err instanceof ApiError && err.status === 401
            ? "Tu sesión venció. Volvé a iniciar sesión."
            : "No pudimos registrar el cliente. Probá de nuevo.";
      setResult({ kind: "error", message });
    }
  }

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      onChange={() => setResult(null)}
      noValidate
      className="mt-8 flex flex-col gap-6"
    >
      <fieldset className="flex flex-col gap-4 rounded-[20px] border border-green bg-cream p-6">
        <legend className="px-2 text-normal font-bold text-green">Datos del cliente</legend>

        <div>
          <label htmlFor="name" className={labelClass}>Nombre *</label>
          <input id="name" className={inputClass} aria-invalid={!!errors.name} {...register("name")} />
          {errors.name && <p className={errorClass}>{errors.name.message}</p>}
        </div>

        <div>
          <label htmlFor="industry" className={labelClass}>Rubro</label>
          <input id="industry" className={inputClass} aria-invalid={!!errors.industry} {...register("industry")} />
          {errors.industry && <p className={errorClass}>{errors.industry.message}</p>}
        </div>

        <div>
          <label htmlFor="notes" className={labelClass}>Notas</label>
          <textarea id="notes" rows={3} className={inputClass} aria-invalid={!!errors.notes} {...register("notes")} />
          {errors.notes && <p className={errorClass}>{errors.notes.message}</p>}
        </div>
      </fieldset>

      <fieldset className="flex flex-col gap-4 rounded-[20px] border border-green bg-cream p-6">
        <legend className="px-2 text-normal font-bold text-green">Personas de contacto</legend>

        {contacts.fields.map((field, i) => {
          const e = errors.contacts?.[i];
          return (
            <div key={field.id} className="flex flex-col gap-3 border-b border-green/30 pb-4 last:border-b-0 last:pb-0">
              <div className="flex items-center justify-between">
                <p className="text-small font-bold text-green">Contacto {i + 1}</p>
                {contacts.fields.length > 1 && (
                  <button
                    type="button"
                    onClick={() => contacts.remove(i)}
                    className="text-small text-red hover:underline"
                  >
                    Quitar
                  </button>
                )}
              </div>
              <div className="grid gap-3 md:grid-cols-3">
                <div>
                  <label htmlFor={`contact-${i}-name`} className={labelClass}>Nombre *</label>
                  <input id={`contact-${i}-name`} className={inputClass} aria-invalid={!!e?.name} {...register(`contacts.${i}.name`)} />
                  {e?.name && <p className={errorClass}>{e.name.message}</p>}
                </div>
                <div>
                  <label htmlFor={`contact-${i}-email`} className={labelClass}>Email *</label>
                  <input id={`contact-${i}-email`} type="email" className={inputClass} aria-invalid={!!e?.email} {...register(`contacts.${i}.email`)} />
                  {e?.email && <p className={errorClass}>{e.email.message}</p>}
                </div>
                <div>
                  <label htmlFor={`contact-${i}-phone`} className={labelClass}>Teléfono *</label>
                  <input id={`contact-${i}-phone`} type="tel" className={inputClass} aria-invalid={!!e?.phone} {...register(`contacts.${i}.phone`)} />
                  {e?.phone && <p className={errorClass}>{e.phone.message}</p>}
                </div>
              </div>
            </div>
          );
        })}

        <button
          type="button"
          onClick={() => contacts.append(EMPTY_CONTACT)}
          className="w-fit rounded-full border border-green px-4 py-2 text-small font-medium text-green hover:bg-white"
        >
          + Agregar contacto
        </button>
      </fieldset>

      <button
        type="submit"
        disabled={isSubmitting}
        className="w-fit rounded-full bg-green px-6 py-3 text-normal text-cream hover:opacity-90 disabled:opacity-50"
      >
        {isSubmitting ? "Registrando…" : "Registrar cliente"}
      </button>

      {result?.kind === "success" && (
        <p role="status" className="rounded-[10px] border border-green bg-white p-3 text-normal text-green">
          Registramos a {result.name}.{" "}
          <Link href={`/clients/${result.id}`} className="font-bold underline">
            Ver cliente
          </Link>
        </p>
      )}
      {result?.kind === "error" && (
        <p role="alert" className="rounded-[10px] border border-red bg-white p-3 text-normal text-red">
          {result.message}
        </p>
      )}
    </form>
  );
}
