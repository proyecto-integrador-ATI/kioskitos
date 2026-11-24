"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AppHeader } from "../../components/AppHeader";
import { getToken } from "../../services/auth";
import {
  formatDate,
  listProjects,
  STATUS_LABELS,
  type ProjectListItem,
} from "../../services/projects";

type State =
  | { kind: "loading" }
  | { kind: "loaded"; projects: ProjectListItem[] }
  | { kind: "error" };

export default function ProjectsPage() {
  const router = useRouter();
  const [state, setState] = useState<State>({ kind: "loading" });

  const [attempt, setAttempt] = useState(0);

  // cacheComponents hides this page instead of unmounting it, and the effect runs again
  // every time it becomes visible: that refetch is what shows projects created meanwhile.
  // The previous list stays on screen while it reloads.
  useEffect(() => {
    if (!getToken()) {
      router.replace("/login");
      return;
    }
    let current = true;
    listProjects()
      .then((projects) => current && setState({ kind: "loaded", projects }))
      .catch(() => current && setState({ kind: "error" }));
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
        <h1 className="text-header2 text-green">Proyectos</h1>

        <div className="mt-8">
          {state.kind === "loading" && (
            <p role="status" className="text-normal text-green">
              Cargando proyectos…
            </p>
          )}

          {state.kind === "error" && (
            <div role="alert" className="flex flex-col items-start gap-3">
              <p className="text-normal text-red">No pudimos cargar los proyectos.</p>
              <button
                type="button"
                onClick={retry}
                className="rounded-full border border-green px-4 py-2 text-small font-medium text-green hover:bg-white"
              >
                Reintentar
              </button>
            </div>
          )}

          {state.kind === "loaded" && state.projects.length === 0 && (
            <div className="flex flex-col items-start gap-3 rounded-[20px] border border-green bg-cream p-6">
              <p className="text-normal text-green">Todavía no tenés proyectos.</p>
              {/* Projects are always created from a client, so the empty state points there. */}
              <Link href="/clients" className="text-normal font-bold text-green hover:underline">
                Elegí un cliente para crear tu primer proyecto
              </Link>
            </div>
          )}

          {state.kind === "loaded" && state.projects.length > 0 && (
            <ul className="grid gap-4 md:grid-cols-2">
              {state.projects.map((project) => (
                // Not a single card-wide link: the project and its client link to different pages.
                <li
                  key={project.id}
                  className="flex h-full flex-col gap-2 rounded-[20px] border border-green bg-cream p-6 text-green"
                >
                  <Link href={`/projects/${project.id}?from=projects`} className="text-normal font-bold hover:underline">
                    {project.name}
                  </Link>
                  <dl className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-small">
                    <dt>Cliente</dt>
                    <dd>
                      <Link href={`/clients/${project.clientId}`} className="hover:underline">
                        {project.clientName}
                      </Link>
                    </dd>
                    <dt>Estado</dt>
                    <dd>{STATUS_LABELS[project.status]}</dd>
                    <dt>Entrega</dt>
                    <dd>{project.dueDate ? formatDate(project.dueDate) : "—"}</dd>
                  </dl>
                  {project.tags.length > 0 && (
                    <ul aria-label="Etiquetas" className="flex flex-wrap gap-2">
                      {project.tags.map((tag) => (
                        <li
                          key={tag.id}
                          className="rounded-full border border-green bg-white px-3 py-1 text-small"
                        >
                          {tag.name}
                        </li>
                      ))}
                    </ul>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>
      </main>
    </div>
  );
}
