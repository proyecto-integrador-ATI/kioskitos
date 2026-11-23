import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../../../services/apiError";
import NewClientPage from "./page";

const replace = vi.fn();
const router = { replace };
const apiFetch = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => router }));
vi.mock("../../../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

const submit = () => userEvent.click(screen.getByRole("button", { name: "Registrar cliente" }));

async function fillValidClient() {
  await userEvent.type(screen.getByLabelText("Nombre *", { selector: "#name" }), "  Estudio Sur  ");
  await userEvent.type(screen.getByLabelText("Nombre *", { selector: "#contact-0-name" }), "Ana");
  await userEvent.type(screen.getByLabelText("Email *"), "ana@sur.uy");
  await userEvent.type(screen.getByLabelText("Teléfono *"), "099123456");
}

describe("NewClientPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    localStorage.setItem("kioskitos_token", "jwt");
  });

  it("goes to login when there is no token", () => {
    localStorage.clear();
    render(<NewClientPage />);

    expect(replace).toHaveBeenCalledWith("/login");
  });

  it("sends the trimmed client with empty optional fields as null", async () => {
    apiFetch.mockResolvedValueOnce({ id: "c1", name: "Estudio Sur", contacts: [] });
    render(<NewClientPage />);

    await fillValidClient();
    await submit();

    expect(apiFetch).toHaveBeenCalledWith(
      "/api/clients",
      expect.objectContaining({ method: "POST" }),
    );
    expect(JSON.parse(apiFetch.mock.calls[0][1].body)).toEqual({
      name: "Estudio Sur",
      industry: null,
      notes: null,
      contacts: [{ name: "Ana", email: "ana@sur.uy", phone: "099123456" }],
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Registramos a Estudio Sur.");
    expect(screen.getByRole("link", { name: "Ver cliente" })).toHaveAttribute("href", "/clients/c1");
  });

  it("links back to the clients list", () => {
    render(<NewClientPage />);

    expect(screen.getByRole("link", { name: "← Volver a clientes" })).toHaveAttribute("href", "/clients");
  });

  it("does not call the API when required fields are missing", async () => {
    render(<NewClientPage />);

    await submit();

    expect(await screen.findAllByText("El nombre es obligatorio.")).toHaveLength(2);
    expect(screen.getByText("El email es obligatorio.")).toBeInTheDocument();
    expect(screen.getByText("El teléfono es obligatorio.")).toBeInTheDocument();
    expect(apiFetch).not.toHaveBeenCalled();
  });

  it("rejects an invalid contact email", async () => {
    render(<NewClientPage />);

    await fillValidClient();
    await userEvent.clear(screen.getByLabelText("Email *"));
    await userEvent.type(screen.getByLabelText("Email *"), "no-es-un-email");
    await submit();

    expect(await screen.findByText("Ingresá un email válido.")).toBeInTheDocument();
    expect(apiFetch).not.toHaveBeenCalled();
  });

  it.each(["ss", "099-abc-456", "1234567", "++598 99 123 456", "1234567890123456"])(
    "rejects the invalid phone %s",
    async (phone) => {
      render(<NewClientPage />);

      await fillValidClient();
      await userEvent.clear(screen.getByLabelText("Teléfono *"));
      await userEvent.type(screen.getByLabelText("Teléfono *"), phone);
      await submit();

      expect(await screen.findByText(/Ingresá un teléfono válido/)).toBeInTheDocument();
      expect(apiFetch).not.toHaveBeenCalled();
    },
  );

  it.each(["099123456", "099 123 456", "2123-4567", "+598 (2) 123 4567"])(
    "accepts the phone %s",
    async (phone) => {
      apiFetch.mockResolvedValueOnce({ id: "c1", name: "Estudio Sur", contacts: [] });
      render(<NewClientPage />);

      await fillValidClient();
      await userEvent.clear(screen.getByLabelText("Teléfono *"));
      await userEvent.type(screen.getByLabelText("Teléfono *"), phone);
      await submit();

      expect(await screen.findByRole("status")).toBeInTheDocument();
    },
  );

  it("adds and removes contacts, always keeping at least one", async () => {
    render(<NewClientPage />);
    expect(screen.queryByRole("button", { name: "Quitar" })).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "+ Agregar contacto" }));
    expect(screen.getByText("Contacto 2")).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: "Quitar" })).toHaveLength(2);

    await userEvent.click(screen.getAllByRole("button", { name: "Quitar" })[1]);
    expect(screen.queryByText("Contacto 2")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Quitar" })).not.toBeInTheDocument();
  });

  it("keeps the data and shows an error when the backend rejects it", async () => {
    apiFetch.mockRejectedValueOnce(new ApiError(400, { errorCode: "invalid_data" }));
    render(<NewClientPage />);

    await fillValidClient();
    await submit();

    expect(await screen.findByRole("alert")).toHaveTextContent("Revisá los datos ingresados.");
    expect(screen.getByLabelText("Email *")).toHaveValue("ana@sur.uy");
  });

  it("tells the user when the session expired", async () => {
    apiFetch.mockRejectedValueOnce(new ApiError(401, null));
    render(<NewClientPage />);

    await fillValidClient();
    await submit();

    expect(await screen.findByRole("alert")).toHaveTextContent("Tu sesión venció");
  });
});
