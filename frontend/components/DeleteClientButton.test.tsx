import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../services/apiError";
import { takeNotice } from "../services/notice";
import { DeleteClientButton } from "./DeleteClientButton";

const replace = vi.fn();
const router = { replace };
const apiFetch = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => router }));
vi.mock("../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

const open = () => userEvent.click(screen.getByRole("button", { name: "Eliminar" }));
const confirm = () => userEvent.click(screen.getByRole("button", { name: "Sí, eliminar" }));

describe("DeleteClientButton", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    takeNotice();
    localStorage.setItem("kioskitos_token", "jwt");
  });

  it("asks for confirmation naming the client", async () => {
    render(<DeleteClientButton clientId="c1" clientName="Estudio Sur" />);

    await open();

    expect(screen.getByRole("dialog", { name: "¿Eliminar a Estudio Sur?" })).toBeInTheDocument();
    expect(apiFetch).not.toHaveBeenCalled();
  });

  it("does nothing when the user cancels", async () => {
    render(<DeleteClientButton clientId="c1" clientName="Estudio Sur" />);

    await open();
    await userEvent.click(screen.getByRole("button", { name: "Cancelar" }));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(apiFetch).not.toHaveBeenCalled();
  });

  it("deletes the client, leaves a notice and goes to the list", async () => {
    apiFetch.mockResolvedValueOnce(undefined);
    render(<DeleteClientButton clientId="c1" clientName="Estudio Sur" />);

    await open();
    await confirm();

    expect(apiFetch).toHaveBeenCalledWith("/api/clients/c1", expect.objectContaining({ method: "DELETE" }));
    expect(replace).toHaveBeenCalledWith("/clients");
    expect(takeNotice()).toBe("Eliminamos a Estudio Sur.");
  });

  it("shows a loading state and sends a single request while deleting", async () => {
    apiFetch.mockReturnValueOnce(new Promise(() => {}));
    render(<DeleteClientButton clientId="c1" clientName="Estudio Sur" />);

    await open();
    await confirm();

    const deleting = screen.getByRole("button", { name: "Eliminando…" });
    expect(deleting).toBeDisabled();
    expect(screen.getByRole("button", { name: "Cancelar" })).toBeDisabled();
    await userEvent.click(deleting);
    expect(apiFetch).toHaveBeenCalledTimes(1);
  });

  it("explains why a client with projects cannot be deleted and keeps the dialog open", async () => {
    apiFetch.mockRejectedValueOnce(new ApiError(409, { errorCode: "client_has_projects" }));
    render(<DeleteClientButton clientId="c1" clientName="Estudio Sur" />);

    await open();
    await confirm();

    expect(await screen.findByRole("alert")).toHaveTextContent("tiene proyectos asociados");
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sí, eliminar" })).toBeEnabled();
    expect(replace).not.toHaveBeenCalled();
    expect(takeNotice()).toBeNull();
  });

  it("shows a generic error when the deletion fails", async () => {
    apiFetch.mockRejectedValueOnce(new ApiError(500, null));
    render(<DeleteClientButton clientId="c1" clientName="Estudio Sur" />);

    await open();
    await confirm();

    expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos eliminar el cliente.");
  });
});
