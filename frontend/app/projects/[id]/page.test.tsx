import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../../../services/apiError";
import ProjectPage from "./page";

const replace = vi.fn();
const router = { replace }; // stable reference, like Next's real router
const apiFetch = vi.fn();
let searchParams = new URLSearchParams();

vi.mock("next/navigation", () => ({
  useRouter: () => router,
  useParams: () => ({ id: "p1" }),
  useSearchParams: () => searchParams,
}));
vi.mock("../../../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

const PROJECT = {
  id: "p1",
  clientId: "c1",
  name: "Website",
  description: "Corporate site",
  objectives: null,
  scope: null,
  status: "Planning",
  hasStages: false,
  createdAt: "2026-11-21T18:00:00Z",
  dueDate: "2027-03-01",
  tags: [
    { id: "t1", name: "Branding" },
    { id: "t2", name: "Web" },
  ],
  stages: [],
};

describe("ProjectPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    searchParams = new URLSearchParams();
  });

  it("links back to all projects when opened from the projects list", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    searchParams = new URLSearchParams("from=projects");
    apiFetch.mockResolvedValueOnce(PROJECT);

    render(<ProjectPage />);

    expect(await screen.findByRole("link", { name: /Volver/ })).toHaveAttribute("href", "/projects");
    expect(screen.getByRole("link", { name: /Volver/ })).toHaveTextContent("Volver a todos los proyectos");
  });

  it("redirects to login when there is no session", () => {
    render(<ProjectPage />);
    expect(replace).toHaveBeenCalledWith("/login");
  });

  it("shows the project detail", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    apiFetch.mockResolvedValueOnce(PROJECT);

    render(<ProjectPage />);

    expect(await screen.findByRole("heading", { name: "Website" })).toBeInTheDocument();
    expect(screen.getByText("Planificación")).toBeInTheDocument();
    expect(screen.getByText("01/03/2027")).toBeInTheDocument();
    expect(screen.getByText("Branding, Web")).toBeInTheDocument();
    expect(screen.getByText("Corporate site")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Volver/ })).toHaveAttribute("href", "/clients/c1/projects");
    expect(apiFetch).toHaveBeenCalledWith(
      "/api/projects/p1",
      expect.objectContaining({ headers: { Authorization: "Bearer jwt" } }),
    );
  });

  it("shows the stages in order with their status", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    apiFetch.mockResolvedValueOnce({
      ...PROJECT,
      hasStages: true,
      stages: [
        { id: "s1", name: "Diseño", status: "Finished" },
        { id: "s2", name: "Desarrollo", status: "Planning" },
      ],
    });

    render(<ProjectPage />);

    const items = await screen.findAllByRole("listitem");
    expect(items.map((li) => li.textContent)).toEqual([
      "Diseño (Finalizado)",
      "Desarrollo (Planificación)",
    ]);
  });

  it("shows an error when the project does not exist", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    apiFetch.mockRejectedValueOnce(new ApiError(404, null));

    render(<ProjectPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent("El proyecto no existe.");
  });
});
