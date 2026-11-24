"use client";

import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { ApiError } from "../../../services/apiError";
import { getToken } from "../../../services/auth";
import { formatDate, getProject, STATUS_LABELS, type Project } from "../../../services/projects";

type State =
  | { kind: "loading" }
  | { kind: "loaded"; project: Project }
  | { kind: "error"; message: string };

function ProjectDetail() {
  const router = useRouter();
  const { id } = useParams<{ id: string }>();
  // The detail is reached from two lists; "from" says which one the back link returns to.
  const fromAllProjects = useSearchParams().get("from") === "projects";
  const [state, setState] = useState<State>({ kind: "loading" });

  useEffect(() => {
    if (!getToken()) {
      router.replace("/login");
      return;
    }
    getProject(id)
      .then((project) => setState({ kind: "loaded", project }))
      .catch((e) =>
        setState({
          kind: "error",
          message:
            e instanceof ApiError && e.status === 404
              ? "El proyecto no existe."
              : "No pudimos cargar el proyecto.",
        }),
      );
  }, [id, router]);

  if (state.kind === "loading") return <p className="p-8 text-slate-600">Cargando…</p>;
  if (state.kind === "error") {
    return <p role="alert" className="p-8 text-red-700">{state.message}</p>;
  }

  const { project } = state;

  return (
    <main className="mx-auto w-full max-w-3xl p-8">
      {fromAllProjects ? (
        <Link href="/projects" className="text-sm text-slate-600 hover:underline">
          ← Volver a todos los proyectos
        </Link>
      ) : (
        <Link
          href={`/clients/${project.clientId}/projects`}
          className="text-sm text-slate-600 hover:underline"
        >
          ← Volver a los proyectos del cliente
        </Link>
      )}
      <h1 className="mt-4 text-2xl font-bold">{project.name}</h1>

      <dl className="mt-6 grid grid-cols-[max-content_1fr] gap-x-6 gap-y-2 text-sm">
        <dt className="font-medium">Estado</dt>
        <dd>{STATUS_LABELS[project.status]}</dd>
        <dt className="font-medium">Fecha de entrega</dt>
        <dd>{project.dueDate ? formatDate(project.dueDate) : "Sin fecha"}</dd>
        <dt className="font-medium">Etapas</dt>
        <dd>
          {project.stages.length > 0 ? (
            <ol className="list-decimal pl-5">
              {project.stages.map((s) => (
                <li key={s.id}>
                  {s.name} <span className="text-slate-500">({STATUS_LABELS[s.status]})</span>
                </li>
              ))}
            </ol>
          ) : (
            "Sin etapas"
          )}
        </dd>
        <dt className="font-medium">Etiquetas</dt>
        <dd>{project.tags.length > 0 ? project.tags.map((t) => t.name).join(", ") : "Sin etiquetas"}</dd>
        <dt className="font-medium">Descripción</dt>
        <dd className="whitespace-pre-line">{project.description ?? "—"}</dd>
        <dt className="font-medium">Objetivos</dt>
        <dd className="whitespace-pre-line">{project.objectives ?? "—"}</dd>
        <dt className="font-medium">Alcance</dt>
        <dd className="whitespace-pre-line">{project.scope ?? "—"}</dd>
      </dl>
    </main>
  );
}

export default function ProjectPage() {
  // useParams needs a Suspense boundary because cacheComponents is enabled.
  return (
    <Suspense fallback={<p className="p-8 text-slate-600">Cargando…</p>}>
      <ProjectDetail />
    </Suspense>
  );
}
