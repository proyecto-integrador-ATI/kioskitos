import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../../../../../services/apiError";
import NewProjectPage from "./page";

const replace = vi.fn();
const push = vi.fn();
const router = { replace, push }; // stable reference, like Next's real router
const apiFetch = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => router,
  useParams: () => ({ clientId: "c1" }),
}));
vi.mock("../../../../../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

/** Answers each endpoint by path; `create` decides what POST /api/projects does. */
function mockApi(create: () => Promise<unknown> = () => Promise.resolve({ id: "p1" })) {
  apiFetch.mockImplementation((path: string) => {
    if (path === "/api/tags") return Promise.resolve([{ id: "t1", name: "Branding" }]);
    if (path === "/api/stage-names") return Promise.resolve([{ id: "s1", name: "Diseño" }]);
    if (path === "/api/projects") return create();
    return Promise.reject(new Error(`Unexpected call to ${path}`));
  });
}

const sentBody = () =>
  JSON.parse(apiFetch.mock.calls.find(([path]) => path === "/api/projects")![1].body);

describe("NewProjectPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it("redirects to login when there is no session", () => {
    render(<NewProjectPage />);
    expect(replace).toHaveBeenCalledWith("/login");
  });

  it("creates a project without stages and goes to its detail", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    mockApi();
    render(<NewProjectPage />);

    await userEvent.type(screen.getByLabelText("Nombre *"), "  Website ");
    await userEvent.type(screen.getByLabelText("Objetivos"), "Launch");
    await userEvent.selectOptions(screen.getByLabelText("Estado"), "En curso");
    await userEvent.click(await screen.findByLabelText("Branding"));
    await userEvent.type(screen.getByLabelText("Nueva etiqueta"), "Web{Enter}");
    await userEvent.click(screen.getByRole("button", { name: "Crear proyecto" }));

    expect(apiFetch).toHaveBeenCalledWith(
      "/api/projects",
      expect.objectContaining({ method: "POST", headers: { Authorization: "Bearer jwt" } }),
    );
    expect(sentBody()).toEqual({
      clientId: "c1",
      name: "Website",
      objectives: "Launch",
      hasStages: false,
      status: "InProgress",
      tags: ["Branding", "Web"],
      stages: [],
    });
    expect(push).toHaveBeenCalledWith("/projects/p1");
  });

  it("creates a project with an existing and a new stage, in order", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    mockApi();
    render(<NewProjectPage />);

    await userEvent.type(screen.getByLabelText("Nombre *"), "App");
    await userEvent.click(screen.getByLabelText("El proyecto tiene etapas"));
    await userEvent.selectOptions(await screen.findByLabelText("Etapa existente"), "Diseño");
    await userEvent.click(screen.getByRole("button", { name: "Agregar etapa" }));
    await userEvent.type(screen.getByLabelText("Nueva etapa"), "Desarrollo");
    await userEvent.click(screen.getByRole("button", { name: "Crear etapa" }));
    await userEvent.click(screen.getByRole("button", { name: "Crear proyecto" }));

    expect(sentBody()).toMatchObject({ hasStages: true, stages: ["Diseño", "Desarrollo"] });
    expect(push).toHaveBeenCalledWith("/projects/p1");
  });

  it("leaves the form empty after creating, since Next keeps the page alive", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    mockApi();
    render(<NewProjectPage />);

    await userEvent.type(screen.getByLabelText("Nombre *"), "App");
    await userEvent.selectOptions(screen.getByLabelText("Estado"), "En curso");
    await userEvent.click(await screen.findByLabelText("Branding"));
    await userEvent.click(screen.getByLabelText("El proyecto tiene etapas"));
    await userEvent.type(screen.getByLabelText("Nueva etapa"), "Diseño{Enter}");
    await userEvent.click(screen.getByRole("button", { name: "Crear proyecto" }));

    expect(push).toHaveBeenCalledWith("/projects/p1");
    expect(screen.getByLabelText("Nombre *")).toHaveValue("");
    expect(screen.getByLabelText("Estado")).toHaveValue("Planning");
    expect(screen.getByLabelText("Branding")).not.toBeChecked();
    expect(screen.getByLabelText("El proyecto tiene etapas")).not.toBeChecked();
    expect(screen.queryByLabelText("Nueva etapa")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Crear proyecto" })).toBeEnabled();
  });

  it("asks for at least one stage when the project has stages", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    mockApi();
    render(<NewProjectPage />);

    await userEvent.type(screen.getByLabelText("Nombre *"), "App");
    await userEvent.click(screen.getByLabelText("El proyecto tiene etapas"));
    await userEvent.click(screen.getByRole("button", { name: "Crear proyecto" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Agregá al menos una etapa.");
    expect(apiFetch).not.toHaveBeenCalledWith("/api/projects", expect.anything());
  });

  it("requires a name", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    mockApi();
    render(<NewProjectPage />);

    await userEvent.type(screen.getByLabelText("Nombre *"), "   ");
    await userEvent.click(screen.getByRole("button", { name: "Crear proyecto" }));

    expect(await screen.findByText("El nombre es obligatorio.")).toBeInTheDocument();
    expect(screen.getByLabelText("Nombre *")).toHaveAttribute("aria-invalid", "true");
    expect(apiFetch).not.toHaveBeenCalledWith("/api/projects", expect.anything());
  });

  it("keeps the typed data when saving fails", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    mockApi(() => Promise.reject(new Error("network")));
    render(<NewProjectPage />);

    await userEvent.type(screen.getByLabelText("Nombre *"), "Website");
    await userEvent.type(screen.getByLabelText("Alcance"), "Landing");
    await userEvent.click(screen.getByRole("button", { name: "Crear proyecto" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos crear el proyecto.");
    expect(screen.getByLabelText("Nombre *")).toHaveValue("Website");
    expect(screen.getByLabelText("Alcance")).toHaveValue("Landing");
    expect(screen.getByRole("button", { name: "Crear proyecto" })).toBeEnabled();
  });

  it("shows a friendly message when the client does not exist", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    mockApi(() => Promise.reject(new ApiError(404, { errorCode: "client_not_found" })));
    render(<NewProjectPage />);

    await userEvent.type(screen.getByLabelText("Nombre *"), "Website");
    await userEvent.click(screen.getByRole("button", { name: "Crear proyecto" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("El cliente no existe.");
    expect(push).not.toHaveBeenCalled();
  });
});
