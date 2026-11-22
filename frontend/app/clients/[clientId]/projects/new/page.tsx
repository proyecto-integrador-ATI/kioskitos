"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
import { z } from "zod";
import { StageSelector } from "../../../../../components/StageSelector";
import { TagSelector } from "../../../../../components/TagSelector";
import { ApiError } from "../../../../../services/apiError";
import { getToken } from "../../../../../services/auth";
import {
  createProject,
  listStageNames,
  listTags,
  STATUS_LABELS,
  type ProjectStatus,
} from "../../../../../services/projects";

const optionalText = (max: number, label: string) =>
  z
    .string()
    .trim()
    .max(max, `${label} no puede superar los ${max} caracteres.`)
    .transform((v) => v || undefined);

/**
 * Form rules. Text fields are edited as strings and the schema turns them into the
 * payload the backend expects (trimmed, empty optional fields omitted).
 */
const projectSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, "El nombre es obligatorio.")
      .max(200, "El nombre no puede superar los 200 caracteres."),
    description: optionalText(2000, "La descripción"),
    objectives: optionalText(2000, "Los objetivos"),
    scope: optionalText(2000, "El alcance"),
    status: z.enum(Object.keys(STATUS_LABELS) as [ProjectStatus, ...ProjectStatus[]]),
    dueDate: z.string().transform((v) => v || undefined),
    hasStages: z.boolean(),
    stages: z.array(z.string()),
    tags: z.array(z.string()),
  })
  .superRefine((data, ctx) => {
    if (data.hasStages && data.stages.length === 0) {
      ctx.addIssue({ code: "custom", path: ["stages"], message: "Agregá al menos una etapa." });
    }
  });

type FormInput = z.input<typeof projectSchema>;
type FormOutput = z.output<typeof projectSchema>;
type TextField = "name" | "description" | "objectives" | "scope" | "dueDate";

const DEFAULT_VALUES: FormInput = {
  name: "",
  description: "",
  objectives: "",
  scope: "",
  status: "Planning",
  dueDate: "",
  hasStages: false,
  stages: [],
  tags: [],
};

const ERROR_MESSAGES: Record<string, string> = {
  client_not_found: "El cliente no existe.",
  invalid_data: "Revisá los datos del proyecto.",
};

function toErrorMessage(err: unknown) {
  if (!(err instanceof ApiError)) return "No pudimos crear el proyecto. Probá de nuevo.";
  const errorCode = err.problem?.errorCode;
  if (errorCode && ERROR_MESSAGES[errorCode]) return ERROR_MESSAGES[errorCode];
  if (err.status === 401) return "Tu sesión venció. Volvé a iniciar sesión.";
  if (err.status === 400) return ERROR_MESSAGES.invalid_data;
  return "No pudimos crear el proyecto. Probá de nuevo.";
}

const inputClass = "rounded-md border border-slate-300 px-2 py-1";

function NewProjectForm() {
  const router = useRouter();
  const { clientId } = useParams<{ clientId: string }>();

  const [availableTags, setAvailableTags] = useState<string[]>([]);
  const [availableStageNames, setAvailableStageNames] = useState<string[]>([]);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors, isSubmitting },
  } = useForm<FormInput, unknown, FormOutput>({
    resolver: zodResolver(projectSchema),
    defaultValues: DEFAULT_VALUES,
  });

  const hasStages = useWatch({ control, name: "hasStages" });

  useEffect(() => {
    if (!getToken()) {
      router.replace("/login");
      return;
    }
    // Without the existing tags or stage names the user can still type new ones.
    listTags()
      .then((t) => setAvailableTags(t.map((tag) => tag.name)))
      .catch(() => setAvailableTags([]));
    listStageNames()
      .then((n) => setAvailableStageNames(n.map((stageName) => stageName.name)))
      .catch(() => setAvailableStageNames([]));
  }, [router]);

  async function onSubmit(data: FormOutput) {
    setSubmitError(null);
    try {
      const project = await createProject({
        clientId,
        ...data,
        stages: data.hasStages ? data.stages : [],
      });
      // With cacheComponents Next.js keeps this page alive (hidden) after navigating away,
      // so it is reset here; otherwise the next "Nuevo proyecto" would show this project's data.
      reset(DEFAULT_VALUES);
      router.push(`/projects/${project.id}`);
    } catch (err) {
      // The form keeps what the user typed so they can retry.
      setSubmitError(toErrorMessage(err));
    }
  }

  const fieldProps = (field: TextField) => ({
    id: field,
    "aria-invalid": !!errors[field],
    "aria-describedby": errors[field] ? `${field}-error` : undefined,
    className: inputClass,
    ...register(field),
  });

  const fieldError = (field: TextField) =>
    errors[field] && (
      <p id={`${field}-error`} className="text-sm text-red-700">
        {errors[field]?.message}
      </p>
    );

  return (
    <main className="mx-auto w-full max-w-3xl p-8">
      <Link href={`/clients/${clientId}/projects`} className="text-sm text-slate-600 hover:underline">
        ← Volver a los proyectos
      </Link>
      <h1 className="mt-4 text-2xl font-bold">Nuevo proyecto</h1>

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="mt-6 flex flex-col gap-4">
        <div className="flex flex-col gap-1 text-sm">
          <label htmlFor="name">Nombre *</label>
          <input {...fieldProps("name")} maxLength={200} />
          {fieldError("name")}
        </div>
        <div className="flex flex-col gap-1 text-sm">
          <label htmlFor="description">Descripción</label>
          <textarea {...fieldProps("description")} maxLength={2000} />
          {fieldError("description")}
        </div>
        <div className="flex flex-col gap-1 text-sm">
          <label htmlFor="objectives">Objetivos</label>
          <textarea {...fieldProps("objectives")} maxLength={2000} />
          {fieldError("objectives")}
        </div>
        <div className="flex flex-col gap-1 text-sm">
          <label htmlFor="scope">Alcance</label>
          <textarea {...fieldProps("scope")} maxLength={2000} />
          {fieldError("scope")}
        </div>
        <div className="flex flex-col gap-1 text-sm">
          <label htmlFor="status">Estado</label>
          <select id="status" {...register("status")} className={inputClass}>
            {Object.entries(STATUS_LABELS).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>
        <div className="flex flex-col gap-1 text-sm">
          <label htmlFor="dueDate">Fecha de entrega</label>
          <input {...fieldProps("dueDate")} type="date" />
          {fieldError("dueDate")}
        </div>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" {...register("hasStages")} />
          El proyecto tiene etapas
        </label>

        {hasStages && (
          <>
            <Controller
              control={control}
              name="stages"
              render={({ field }) => (
                <StageSelector
                  available={availableStageNames}
                  value={field.value}
                  onChange={field.onChange}
                />
              )}
            />
            {errors.stages && (
              <p role="alert" className="text-sm text-red-700">
                {errors.stages.message}
              </p>
            )}
          </>
        )}

        <Controller
          control={control}
          name="tags"
          render={({ field }) => (
            <TagSelector available={availableTags} value={field.value} onChange={field.onChange} />
          )}
        />

        {submitError && (
          <p role="alert" className="text-sm text-red-700">
            {submitError}
          </p>
        )}

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-fit rounded-md bg-slate-900 px-4 py-2 text-sm text-white hover:bg-slate-700 disabled:opacity-50"
        >
          {isSubmitting ? "Creando…" : "Crear proyecto"}
        </button>
      </form>
    </main>
  );
}

export default function NewProjectPage() {
  // useParams needs a Suspense boundary because cacheComponents is enabled.
  return (
    <Suspense fallback={<p className="p-8 text-slate-600">Cargando…</p>}>
      <NewProjectForm />
    </Suspense>
  );
}
