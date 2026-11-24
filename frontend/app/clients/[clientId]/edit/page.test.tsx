import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../../../../services/apiError";
import EditClientPage from "./page";

const replace = vi.fn();
const router = { replace }; // stable reference, like Next's real router
const apiFetch = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => router,
  useParams: () => ({ clientId: "c1" }),
}));
vi.mock("../../../../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

const CLIENT = {
  id: "c1",
  name: "Estudio Sur",
  industry: "Diseño",
  notes: null,
  createdAt: "2026-11-22T12:00:00Z",
  contacts: [{ id: "p1", name: "Ana", email: "ana@sur.uy", phone: "099 123 456" }],
};

const save = () => userEvent.click(screen.getByRole("button", { name: "Guardar cambios" }));

describe("EditClientPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    localStorage.clear();
    localStorage.setItem("kioskitos_token", "jwt");
  });

  it("goes to login when there is no token", () => {
    localStorage.clear();
    render(<EditClientPage />);

    expect(replace).toHaveBeenCalledWith("/login");
    expect(apiFetch).not.toHaveBeenCalled();
  });

  it("prefills the form with the current client data", async () => {
    apiFetch.mockResolvedValueOnce(CLIENT);
    render(<EditClientPage />);

    expect(await screen.findByLabelText("Nombre *", { selector: "#name" })).toHaveValue("Estudio Sur");
    expect(screen.getByLabelText("Rubro")).toHaveValue("Diseño");
    expect(screen.getByLabelText("Notas")).toHaveValue("");
    expect(screen.getByLabelText("Email *")).toHaveValue("ana@sur.uy");
    expect(screen.getByRole("link", { name: "← Volver al cliente" })).toHaveAttribute("href", "/clients/c1");
  });

  it("sends the changes with PUT and confirms they were saved", async () => {
    apiFetch.mockResolvedValueOnce(CLIENT).mockResolvedValueOnce({ ...CLIENT, name: "Estudio Norte" });
    render(<EditClientPage />);

    const name = await screen.findByLabelText("Nombre *", { selector: "#name" });
    await userEvent.clear(name);
    await userEvent.type(name, "Estudio Norte");
    await userEvent.clear(screen.getByLabelText("Rubro"));
    await save();

    expect(apiFetch).toHaveBeenLastCalledWith(
      "/api/clients/c1",
      expect.objectContaining({ method: "PUT" }),
    );
    expect(JSON.parse(apiFetch.mock.lastCall![1].body)).toEqual({
      name: "Estudio Norte",
      industry: null,
      notes: null,
      contacts: [{ name: "Ana", email: "ana@sur.uy", phone: "099 123 456" }],
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Guardamos los cambios de Estudio Norte.");
    expect(screen.getByRole("link", { name: "Ver cliente" })).toHaveAttribute("href", "/clients/c1");
    // The form keeps the saved values instead of going back to the old ones.
    expect(name).toHaveValue("Estudio Norte");
  });

  it("does not call the API when the data is invalid", async () => {
    apiFetch.mockResolvedValueOnce(CLIENT);
    render(<EditClientPage />);

    await userEvent.clear(await screen.findByLabelText("Email *"));
    await save();

    expect(await screen.findByText("El email es obligatorio.")).toBeInTheDocument();
    expect(apiFetch).toHaveBeenCalledTimes(1);
  });

  it("keeps the changes and shows an error when saving fails", async () => {
    apiFetch.mockResolvedValueOnce(CLIENT).mockRejectedValueOnce(new ApiError(500, null));
    render(<EditClientPage />);

    const name = await screen.findByLabelText("Nombre *", { selector: "#name" });
    await userEvent.type(name, " SRL");
    await save();

    expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos guardar los cambios.");
    expect(name).toHaveValue("Estudio Sur SRL");
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("shows not found when the client does not exist or belongs to another user", async () => {
    apiFetch.mockRejectedValueOnce(new ApiError(404, null));
    render(<EditClientPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent("No encontramos este cliente.");
    expect(screen.queryByRole("button", { name: "Guardar cambios" })).not.toBeInTheDocument();
  });
});
