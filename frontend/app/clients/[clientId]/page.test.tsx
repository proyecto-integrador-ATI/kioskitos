import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../../../services/apiError";
import ClientPage from "./page";

const replace = vi.fn();
const router = { replace }; // stable reference, like Next's real router
const apiFetch = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => router,
  useParams: () => ({ clientId: "c1" }),
}));
vi.mock("../../../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

const CLIENT = {
  id: "c1",
  name: "Estudio Sur",
  industry: "Diseño",
  notes: "Paga a 30 días.\nPrefiere mail.",
  createdAt: "2026-11-22T12:00:00Z",
  contacts: [
    { id: "p1", name: "Ana", email: "ana@sur.uy", phone: "099 123 456" },
    { id: "p2", name: "Luis", email: "luis@sur.uy", phone: "+598 2 123 4567" },
  ],
};

// The client detail is requested first; ClientProjectsList asks for the projects after it renders.
function mockApi(client: unknown, projects: unknown[] = []) {
  apiFetch.mockImplementation((path: string) =>
    path.endsWith("/projects") ? Promise.resolve(projects) : Promise.resolve(client),
  );
}

describe("ClientPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    localStorage.clear();
    localStorage.setItem("kioskitos_token", "jwt");
  });

  it("goes to login when there is no token", () => {
    localStorage.clear();
    render(<ClientPage />);

    expect(replace).toHaveBeenCalledWith("/login");
    expect(apiFetch).not.toHaveBeenCalled();
  });

  it("shows a loading state while the client loads", () => {
    apiFetch.mockReturnValueOnce(new Promise(() => {}));
    render(<ClientPage />);

    expect(screen.getByRole("status")).toHaveTextContent("Cargando cliente…");
  });

  it("shows every field of the client, including notes and all contacts", async () => {
    mockApi(CLIENT);
    render(<ClientPage />);

    expect(await screen.findByRole("heading", { name: "Estudio Sur" })).toBeInTheDocument();
    expect(apiFetch).toHaveBeenCalledWith("/api/clients/c1", expect.anything());
    expect(screen.getByText("Diseño")).toBeInTheDocument();
    expect(screen.getByText(/Paga a 30 días\./)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "ana@sur.uy" })).toHaveAttribute("href", "mailto:ana@sur.uy");
    expect(screen.getByRole("link", { name: "099 123 456" })).toHaveAttribute("href", "tel:099123456");
    expect(screen.getByRole("link", { name: "+598 2 123 4567" })).toHaveAttribute("href", "tel:+59821234567");
    expect(screen.getByRole("link", { name: "← Volver a clientes" })).toHaveAttribute("href", "/clients");
  });

  it("shows a placeholder for missing optional fields", async () => {
    mockApi({ ...CLIENT, industry: null, notes: null });
    render(<ClientPage />);

    await screen.findByRole("heading", { name: "Estudio Sur" });
    const data = within(screen.getByRole("heading", { name: "Datos del cliente" }).parentElement!);
    expect(data.getAllByText("—")).toHaveLength(2);
  });

  it("links to the edit page", async () => {
    mockApi(CLIENT);
    render(<ClientPage />);

    expect(await screen.findByRole("link", { name: "Editar" })).toHaveAttribute("href", "/clients/c1/edit");
  });

  it("shows the client's projects and a link to create one", async () => {
    mockApi(CLIENT, [
      { id: "pr1", name: "Website", status: "InProgress", createdAt: "", dueDate: null, tags: [] },
    ]);
    render(<ClientPage />);

    expect(await screen.findByRole("link", { name: "Website" })).toHaveAttribute("href", "/projects/pr1");
    expect(screen.getByRole("link", { name: "Nuevo proyecto" })).toHaveAttribute(
      "href",
      "/clients/c1/projects/new",
    );
  });

  it("shows not found when the client does not exist or belongs to another user", async () => {
    apiFetch.mockRejectedValueOnce(new ApiError(404, null));
    render(<ClientPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent("No encontramos este cliente.");
    expect(screen.queryByRole("button", { name: "Reintentar" })).not.toBeInTheDocument();
  });

  it("shows an error and retries loading", async () => {
    apiFetch.mockRejectedValueOnce(new ApiError(500, null));
    render(<ClientPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos cargar el cliente.");

    mockApi(CLIENT);
    await userEvent.click(screen.getByRole("button", { name: "Reintentar" }));

    expect(await screen.findByRole("heading", { name: "Estudio Sur" })).toBeInTheDocument();
  });
});
