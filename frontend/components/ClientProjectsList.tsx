"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ApiError } from "../services/apiError";
import {
  formatDate,
  listClientProjects,
  STATUS_LABELS,
  type ProjectSummary,
} from "../services/projects";

type State =
  | { kind: "loading" }
  | { kind: "loaded"; projects: ProjectSummary[] }
  | { kind: "error"; message: string };

export function ClientProjectsList({ clientId }: { clientId: string }) {
  const [state, setState] = useState<State>({ kind: "loading" });

  useEffect(() => {
    listClientProjects(clientId)
      .then((projects) => setState({ kind: "loaded", projects }))
      .catch((e) =>
        setState({
          kind: "error",
          message:
            e instanceof ApiError && e.status === 404
              ? "El cliente no existe."
              : "No pudimos cargar los proyectos.",
        }),
      );
  }, [clientId]);

  if (state.kind === "loading") return <p className="text-slate-600">Cargando proyectos…</p>;
  if (state.kind === "error") return <p role="alert" className="text-red-700">{state.message}</p>;
  if (state.projects.length === 0) {
    return <p className="text-slate-600">Este cliente todavía no tiene proyectos.</p>;
  }

  return (
    <ul className="flex flex-col gap-2">
      {state.projects.map((p) => (
        <li key={p.id} className="rounded-md border border-slate-200 p-3">
          <Link href={`/projects/${p.id}`} className="font-medium hover:underline">
            {p.name}
          </Link>
          <p className="text-sm text-slate-600">
            {STATUS_LABELS[p.status]}
            {p.dueDate && ` · Entrega: ${formatDate(p.dueDate)}`}
          </p>
        </li>
      ))}
    </ul>
  );
}
