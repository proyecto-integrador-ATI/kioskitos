"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useLayoutEffect, useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { z } from "zod";
import { ApiError } from "../../services/apiError";
import { clearToken, getToken } from "../../services/auth";
import { getMyProfile, updateMyProfile, type UserProfile } from "../../services/users";

/**
 * Form rules. Fields are edited as strings and the schema turns them into the
 * payload the backend expects (trimmed, empty optional fields as null).
 */
const profileSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "El nombre es obligatorio.")
    .max(200, "El nombre no puede superar los 200 caracteres."),
  industry: z
    .string()
    .trim()
    .max(100, "El rubro no puede superar los 100 caracteres.")
    .transform((v) => v || null),
  hourlyRate: z
    .string()
    .trim()
    .refine(
      (v) => v === "" || (Number.isFinite(Number(v)) && Number(v) >= 0),
      "La tarifa por hora debe ser un número mayor o igual a 0.",
    )
    .transform((v) => (v === "" ? null : Number(v))),
});

type FormInput = z.input<typeof profileSchema>;
type FormOutput = z.output<typeof profileSchema>;
type Field = keyof FormInput;

type Result = { kind: "success" } | { kind: "error"; message: string } | null;

const toFormValues = (p: UserProfile): FormInput => ({
  name: p.name,
  industry: p.industry ?? "",
  hourlyRate: p.hourlyRate === null ? "" : String(p.hourlyRate),
});

/**
 * True when the form differs from the saved profile once normalized
 */
function hasChanges(values: FormInput, profile: UserProfile): boolean {
  const parsed = profileSchema.safeParse(values);
  if (!parsed.success) return true;
  const { name, industry, hourlyRate } = parsed.data;
  return name !== profile.name || industry !== profile.industry || hourlyRate !== profile.hourlyRate;
}

const FIELDS: Field[] = ["name", "industry", "hourlyRate"];

/** Maps backend ValidationProblemDetails keys ("Name", "HourlyRate") to form fields. */
function toFieldErrors(errors: Record<string, string[]> | undefined) {
  return Object.entries(errors ?? {})
    .map(([key, messages]) => [key.charAt(0).toLowerCase() + key.slice(1), messages[0]] as const)
    .filter((entry): entry is [Field, string] => FIELDS.includes(entry[0] as Field));
}

export default function ProfilePage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [result, setResult] = useState<Result>(null);

  const {
    register,
    handleSubmit,
    reset,
    control,
    setError,
    clearErrors,
    formState: { errors, isSubmitting },
  } = useForm<FormInput, unknown, FormOutput>({
    resolver: zodResolver(profileSchema),
    defaultValues: { name: "", industry: "", hourlyRate: "" },
  });

  useEffect(() => {
    if (!getToken()) {
      router.replace("/login");
      return;
    }
    getMyProfile()
      .then((p) => {
        setProfile(p);
        reset(toFormValues(p));
      })
      .catch(() => {
        clearToken(); // expired or invalid token
        router.replace("/login");
      });
  }, [router, reset]);

  const values = useWatch({ control });

  // With cacheComponents, Next hides the page with <Activity> instead of unmounting it,
  // so state survives navigation. Clear transient feedback when the page is hidden.
  useLayoutEffect(() => {
    return () => {
      setResult(null);
      clearErrors();
    };
  }, [clearErrors]);

  async function onSubmit(data: FormOutput) {
    setResult(null);
    try {
      const updated = await updateMyProfile(data);
      setProfile(updated);
      reset(toFormValues(updated));
      setResult({ kind: "success" });
    } catch (err) {
      // The form keeps what the user typed so they can retry.
      if (err instanceof ApiError && err.status === 400) {
        for (const [field, message] of toFieldErrors(err.problem?.errors)) {
          setError(field, { type: "server", message });
        }
        setResult({ kind: "error", message: "Revisá los datos ingresados." });
      } else if (err instanceof ApiError && err.status === 401) {
        setResult({ kind: "error", message: "Tu sesión venció. Volvé a iniciar sesión." });
      } else {
        setResult({ kind: "error", message: "No pudimos guardar los cambios. Probá de nuevo." });
      }
    }
  }

  if (!profile) return <p className="p-8 text-slate-600">Cargando…</p>;

  const dirty = hasChanges(values as FormInput, profile);
  const inputClass = "mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm";

  const fieldProps = (field: Field) => ({
    id: field,
    "aria-invalid": !!errors[field],
    "aria-describedby": errors[field] ? `${field}-error` : undefined,
    className: inputClass,
    // Hide the previous result as soon as the user edits the form again.
    ...register(field, { onChange: () => setResult(null) }),
  });

  const fieldError = (field: Field) =>
    errors[field] && (
      <p id={`${field}-error`} className="mt-1 text-sm text-red-700">
        {errors[field]?.message}
      </p>
    );

  return (
    <main className="mx-auto w-full max-w-3xl p-8">
      <Link href="/home" className="text-sm text-slate-600 hover:underline">
        ← Volver al inicio
      </Link>
      <h1 className="mt-4 text-2xl font-bold">Mi perfil</h1>

      <form
        onSubmit={handleSubmit((data) => (dirty ? onSubmit(data) : undefined))}
        noValidate
        className="mt-6 flex max-w-md flex-col gap-4"
      >
        <div>
          <label htmlFor="email" className="text-sm font-medium">Email</label>
          <input id="email" value={profile.email} readOnly disabled className={`${inputClass} bg-slate-100`} />
        </div>

        <div>
          <label htmlFor="name" className="text-sm font-medium">Nombre *</label>
          <input {...fieldProps("name")} maxLength={200} />
          {fieldError("name")}
        </div>

        <div>
          <label htmlFor="industry" className="text-sm font-medium">Rubro</label>
          <input {...fieldProps("industry")} maxLength={100} />
          {fieldError("industry")}
        </div>

        <div>
          <label htmlFor="hourlyRate" className="text-sm font-medium">Tarifa por hora</label>
          <input {...fieldProps("hourlyRate")} type="number" min={0} step="0.01" />
          {fieldError("hourlyRate")}
        </div>

        <button
          type="submit"
          disabled={isSubmitting || !dirty}
          className="w-fit rounded-md bg-slate-900 px-4 py-2 text-sm text-white hover:bg-slate-700 disabled:opacity-50"
        >
          {isSubmitting ? "Guardando…" : "Guardar cambios"}
        </button>

        {result?.kind === "success" && (
          <p role="status" className="rounded-md border border-green-300 bg-green-50 p-3 text-sm text-green-800">
            Los cambios se guardaron correctamente.
          </p>
        )}
        {result?.kind === "error" && (
          <p role="alert" className="rounded-md border border-red-300 bg-red-50 p-3 text-sm text-red-800">
            {result.message}
          </p>
        )}
      </form>
    </main>
  );
}
