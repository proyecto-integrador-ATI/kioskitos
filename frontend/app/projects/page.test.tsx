import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../../services/apiError";
import ProjectsPage from "./page";

const replace = vi.fn();
const router = { replace };
const apiFetch = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => router }));
vi.mock("../../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

const WEBSITE = {
  id: "p1",
  clientId: "c1",
  clientName: "Acme",
  name: "Website",
  status: "InProgress",
  createdAt: "2026-11-01T10:00:00Z",
  dueDate: "2027-03-01",
  tags: [
    { id: "t1", name: "Branding" },
    { id: "t2", name: "web" },
  ],
};
const BARE = {
  id: "p2",
  clientId: "c2",
  clientName: "Globex",
  name: "Logo",
  status: "Planning",
  createdAt: "2026-10-01T10:00:00Z",
  dueDate: null,
  tags: [],
};

describe("ProjectsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    localStorage.setItem("kioskitos_token", "jwt");
  });

  it("goes to login when there is no token", () => {
    localStorage.clear();
    render(<ProjectsPage />);

    expect(replace).toHaveBeenCalledWith("/login");
    expect(apiFetch).not.toHaveBeenCalled();
  });

  it("shows a loading state while the projects load", () => {
    apiFetch.mockReturnValueOnce(new Promise(() => {}));
    render(<ProjectsPage />);

    expect(screen.getByRole("status")).toHaveTextContent("Cargando proyectos…");
  });

  it("lists the projects with their client, status, due date and tags", async () => {
    apiFetch.mockResolvedValueOnce([WEBSITE]);
    render(<ProjectsPage />);

    expect(await screen.findByRole("link", { name: "Website" })).toHaveAttribute(
      "href",
      "/projects/p1?from=projects",
    );
    expect(screen.getByRole("link", { name: "Acme" })).toHaveAttribute("href", "/clients/c1");
    expect(screen.getByText("En curso")).toBeInTheDocument();
    expect(screen.getByText("01/03/2027")).toBeInTheDocument();
    const tags = within(screen.getByRole("list", { name: "Etiquetas" }));
    expect(tags.getAllByRole("listitem").map((li) => li.textContent)).toEqual(["Branding", "web"]);
    expect(apiFetch).toHaveBeenCalledWith("/api/projects", expect.anything());
  });

  it("shows a placeholder without due date and no tag list without tags", async () => {
    apiFetch.mockResolvedValueOnce([BARE]);
    render(<ProjectsPage />);

    await screen.findByRole("link", { name: "Logo" });
    expect(screen.getByText("—")).toBeInTheDocument();
    expect(screen.queryByRole("list", { name: "Etiquetas" })).not.toBeInTheDocument();
  });

  it("shows an empty state pointing to the clients", async () => {
    apiFetch.mockResolvedValueOnce([]);
    render(<ProjectsPage />);

    expect(await screen.findByText("Todavía no tenés proyectos.")).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: "Elegí un cliente para crear tu primer proyecto" }),
    ).toHaveAttribute("href", "/clients");
  });

  it("shows an error and retries loading", async () => {
    apiFetch.mockRejectedValueOnce(new ApiError(500, null)).mockResolvedValueOnce([WEBSITE]);
    render(<ProjectsPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos cargar los proyectos.");

    await userEvent.click(screen.getByRole("button", { name: "Reintentar" }));

    expect(await screen.findByRole("link", { name: "Website" })).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
});
