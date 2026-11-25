import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../../services/apiError";
import { setNotice } from "../../services/notice";
import ClientsPage from "./page";

const replace = vi.fn();
const router = { replace };
const apiFetch = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => router }));
vi.mock("../../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

const ACME = {
  id: "c1",
  name: "Acme",
  industry: "Diseño",
  primaryContact: { id: "p1", name: "Ana", email: "ana@acme.com", phone: "099123456" },
};
const BARE = { id: "c2", name: "Sin datos", industry: null, primaryContact: null };

describe("ClientsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    localStorage.setItem("kioskitos_token", "jwt");
  });

  it("goes to login when there is no token", () => {
    localStorage.clear();
    render(<ClientsPage />);

    expect(replace).toHaveBeenCalledWith("/login");
    expect(apiFetch).not.toHaveBeenCalled();
  });

  it("shows a loading state while the clients load", () => {
    apiFetch.mockReturnValueOnce(new Promise(() => {}));
    render(<ClientsPage />);

    expect(screen.getByRole("status")).toHaveTextContent("Cargando clientes…");
  });

  it("lists the clients with their basic data and links to the detail", async () => {
    apiFetch.mockResolvedValueOnce([ACME]);
    render(<ClientsPage />);

    const link = await screen.findByRole("link", { name: /Acme/ });
    expect(link).toHaveAttribute("href", "/clients/c1");
    const card = within(link);
    expect(card.getByText("Ana")).toBeInTheDocument();
    expect(card.getByText("ana@acme.com")).toBeInTheDocument();
    expect(card.getByText("099123456")).toBeInTheDocument();
    expect(card.getByText("Diseño")).toBeInTheDocument();
    expect(apiFetch).toHaveBeenCalledWith("/api/clients", expect.anything());
  });

  it("shows a placeholder for missing optional fields", async () => {
    apiFetch.mockResolvedValueOnce([BARE]);
    render(<ClientsPage />);

    const card = within(await screen.findByRole("link", { name: /Sin datos/ }));
    expect(card.getAllByText("—")).toHaveLength(4);
  });

  it("shows an empty state with a link to register a client", async () => {
    apiFetch.mockResolvedValueOnce([]);
    render(<ClientsPage />);

    expect(await screen.findByText("Todavía no registraste clientes.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Registrá tu primer cliente" })).toHaveAttribute(
      "href",
      "/clients/new",
    );
  });

  it("shows an error and retries loading", async () => {
    apiFetch.mockRejectedValueOnce(new ApiError(500, null)).mockResolvedValueOnce([ACME]);
    render(<ClientsPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos cargar los clientes.");

    await userEvent.click(screen.getByRole("button", { name: "Reintentar" }));

    expect(await screen.findByRole("link", { name: /Acme/ })).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("shows the notice left by a deletion once, with the refreshed list", async () => {
    setNotice("Eliminamos a Estudio Sur.");
    apiFetch.mockResolvedValue([ACME]);
    const { unmount } = render(<ClientsPage />);

    expect(await screen.findByText("Eliminamos a Estudio Sur.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Acme/ })).toBeInTheDocument();

    unmount();
    render(<ClientsPage />);
    await screen.findByRole("link", { name: /Acme/ });
    expect(screen.queryByText("Eliminamos a Estudio Sur.")).not.toBeInTheDocument();
  });
});
