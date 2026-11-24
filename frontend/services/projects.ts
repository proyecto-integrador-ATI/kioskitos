import { apiFetch } from "./api";
import { getToken } from "./auth";

export type ProjectStatus =
  | "Planning"
  | "InProgress"
  | "Blocked"
  | "InReview"
  | "Finished"
  | "Discarded";

export const STATUS_LABELS: Record<ProjectStatus, string> = {
  Planning: "Planificación",
  InProgress: "En curso",
  Blocked: "Bloqueado",
  InReview: "En revisión",
  Finished: "Finalizado",
  Discarded: "Descartado",
};

export type Tag = { id: string; name: string };

export type StageName = { id: string; name: string };

export type Stage = { id: string; name: string; status: ProjectStatus };

export type Project = {
  id: string;
  clientId: string;
  name: string;
  description: string | null;
  objectives: string | null;
  scope: string | null;
  status: ProjectStatus;
  hasStages: boolean;
  createdAt: string;
  dueDate: string | null; // "YYYY-MM-DD"
  tags: Tag[];
  stages: Stage[]; // in order; empty for projects without stages
};

export type ProjectSummary = Pick<Project, "id" | "name" | "status" | "createdAt" | "dueDate" | "tags">;

/** Row of the list of all projects; carries the client because it mixes several. */
export type ProjectListItem = ProjectSummary & { clientId: string; clientName: string };

export type CreateProjectInput = {
  clientId: string;
  name: string;
  description?: string;
  objectives?: string;
  scope?: string;
  hasStages: boolean;
  dueDate?: string;
  status: ProjectStatus;
  tags: string[];
  stages: string[]; // stage names in order; at least one when hasStages is true
};

const authHeaders = () => ({ Authorization: `Bearer ${getToken()}` });

export const createProject = (input: CreateProjectInput) =>
  apiFetch<Project>("/api/projects", {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify(input),
  });

export const getProject = (id: string) =>
  apiFetch<Project>(`/api/projects/${id}`, { headers: authHeaders() });

export const listProjects = () =>
  apiFetch<ProjectListItem[]>("/api/projects", { headers: authHeaders() });

export const listClientProjects = (clientId: string) =>
  apiFetch<ProjectSummary[]>(`/api/clients/${clientId}/projects`, { headers: authHeaders() });

export const listTags = () => apiFetch<Tag[]>("/api/tags", { headers: authHeaders() });

export const listStageNames = () =>
  apiFetch<StageName[]>("/api/stage-names", { headers: authHeaders() });

/** "2027-03-01" → "01/03/2027". Done by hand to avoid time zone shifts. */
export function formatDate(date: string): string {
  const [year, month, day] = date.split("-");
  return `${day}/${month}/${year}`;
}
