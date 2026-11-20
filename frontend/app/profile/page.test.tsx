import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Activity } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../../services/apiError";
import ProfilePage from "./page";

const PROFILE = {
  id: "1",
  email: "ana@example.com",
  name: "Ana",
  industry: "Diseño",
  hourlyRate: 20,
  createdAt: "2026-01-01T00:00:00Z",
};
const replace = vi.fn();
const router = { replace }; // stable reference, like Next's real router
const apiFetch = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => router }));
vi.mock("../../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

async function renderLoaded() {
  localStorage.setItem("kioskitos_token", "jwt");
  apiFetch.mockResolvedValueOnce(PROFILE);
  render(<ProfilePage />);
  await screen.findByRole("heading", { name: "Mi perfil" });
}

const saveButton = () => screen.getByRole("button", { name: "Guardar cambios" });

async function changeName(name: string) {
  await userEvent.clear(screen.getByLabelText("Nombre *"));
  await userEvent.type(screen.getByLabelText("Nombre *"), name);
}

describe("ProfilePage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it("redirects to login when there is no session", () => {
    render(<ProfilePage />);
    expect(replace).toHaveBeenCalledWith("/login");
  });

  it("shows the current profile values", async () => {
    await renderLoaded();

    expect(apiFetch).toHaveBeenCalledWith(
      "/api/users/me",
      expect.objectContaining({ headers: { Authorization: "Bearer jwt" } }),
    );
    expect(screen.getByLabelText("Email")).toHaveValue("ana@example.com");
    expect(screen.getByLabelText("Email")).toBeDisabled();
    expect(screen.getByLabelText("Nombre *")).toHaveValue("Ana");
    expect(screen.getByLabelText("Rubro")).toHaveValue("Diseño");
    expect(screen.getByLabelText("Tarifa por hora")).toHaveValue(20);
  });

  it("saves only the editable fields and shows the updated values", async () => {
    await renderLoaded();
    apiFetch.mockResolvedValueOnce({ ...PROFILE, name: "Ana María", industry: null, hourlyRate: 35.5 });

    await userEvent.clear(screen.getByLabelText("Nombre *"));
    await userEvent.type(screen.getByLabelText("Nombre *"), "  Ana María ");
    await userEvent.clear(screen.getByLabelText("Rubro"));
    await userEvent.clear(screen.getByLabelText("Tarifa por hora"));
    await userEvent.type(screen.getByLabelText("Tarifa por hora"), "35.5");
    await userEvent.click(saveButton());

    expect(apiFetch).toHaveBeenLastCalledWith(
      "/api/users/me",
      expect.objectContaining({
        method: "PUT",
        headers: { Authorization: "Bearer jwt" },
        body: JSON.stringify({ name: "Ana María", industry: null, hourlyRate: 35.5 }),
      }),
    );
    expect(await screen.findByRole("status")).toHaveTextContent("Los cambios se guardaron");
    expect(screen.getByLabelText("Nombre *")).toHaveValue("Ana María");
    expect(screen.getByLabelText("Rubro")).toHaveValue("");
  });

  it("sends null when the hourly rate is empty", async () => {
    await renderLoaded();
    apiFetch.mockResolvedValueOnce({ ...PROFILE, hourlyRate: null });

    await userEvent.clear(screen.getByLabelText("Tarifa por hora"));
    await userEvent.click(saveButton());

    expect(apiFetch).toHaveBeenLastCalledWith(
      "/api/users/me",
      expect.objectContaining({
        body: JSON.stringify({ name: "Ana", industry: "Diseño", hourlyRate: null }),
      }),
    );
  });

  it("does not submit when the name is empty", async () => {
    await renderLoaded();

    await userEvent.clear(screen.getByLabelText("Nombre *"));
    await userEvent.type(screen.getByLabelText("Nombre *"), "   ");
    await userEvent.click(saveButton());

    expect(screen.getByText("El nombre es obligatorio.")).toBeInTheDocument();
    expect(screen.getByLabelText("Nombre *")).toHaveAttribute("aria-invalid", "true");
    expect(apiFetch).toHaveBeenCalledTimes(1); // only the initial load
  });

  it("does not submit when the hourly rate is negative", async () => {
    await renderLoaded();

    await userEvent.clear(screen.getByLabelText("Tarifa por hora"));
    await userEvent.type(screen.getByLabelText("Tarifa por hora"), "-5");
    await userEvent.click(saveButton());

    expect(screen.getByText(/debe ser un número mayor o igual a 0/)).toBeInTheDocument();
    expect(apiFetch).toHaveBeenCalledTimes(1);
  });

  it("shows an error and keeps the typed values when saving fails", async () => {
    await renderLoaded();
    apiFetch.mockRejectedValueOnce(new ApiError(500, null));

    await userEvent.clear(screen.getByLabelText("Nombre *"));
    await userEvent.type(screen.getByLabelText("Nombre *"), "Nuevo nombre");
    await userEvent.click(saveButton());

    expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos guardar los cambios");
    expect(screen.getByLabelText("Nombre *")).toHaveValue("Nuevo nombre");
    expect(saveButton()).toBeEnabled();
  });

  it("shows backend validation errors next to each field", async () => {
    await renderLoaded();
    apiFetch.mockRejectedValueOnce(
      new ApiError(400, { errors: { Industry: ["The field Industry must be a string with a maximum length of '100'."] } }),
    );

    await userEvent.type(screen.getByLabelText("Rubro"), " gráfico");
    await userEvent.click(saveButton());

    expect(await screen.findByRole("alert")).toHaveTextContent("Revisá los datos ingresados");
    expect(screen.getByText(/maximum length of '100'/)).toBeInTheDocument();
    expect(screen.getByLabelText("Rubro")).toHaveAttribute("aria-invalid", "true");
  });

  it("clears the success message when the page is hidden by Activity", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    apiFetch.mockResolvedValueOnce(PROFILE).mockResolvedValueOnce({ ...PROFILE, name: "Otra" });
    const { rerender } = render(
      <Activity mode="visible">
        <ProfilePage />
      </Activity>,
    );
    await screen.findByRole("heading", { name: "Mi perfil" });
    await changeName("Otra");
    await userEvent.click(saveButton());
    expect(await screen.findByRole("status")).toBeInTheDocument();

    // Simulates navigating away and coming back (Next hides the page instead of unmounting it).
    rerender(
      <Activity mode="hidden">
        <ProfilePage />
      </Activity>,
    );
    apiFetch.mockResolvedValueOnce(PROFILE);
    rerender(
      <Activity mode="visible">
        <ProfilePage />
      </Activity>,
    );

    await screen.findByRole("heading", { name: "Mi perfil" });
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("disables the save button until something changes", async () => {
    await renderLoaded();
    expect(saveButton()).toBeDisabled();

    await changeName("Ana María");
    expect(saveButton()).toBeEnabled();

    // Back to the saved value (extra spaces do not count as a change).
    await changeName(" Ana ");
    expect(saveButton()).toBeDisabled();
  });

  it("does not count an equivalent hourly rate as a change", async () => {
    await renderLoaded();

    await userEvent.clear(screen.getByLabelText("Tarifa por hora"));
    await userEvent.type(screen.getByLabelText("Tarifa por hora"), "20.00");

    expect(saveButton()).toBeDisabled();
  });

  it("disables the save button again after a successful save", async () => {
    await renderLoaded();
    apiFetch.mockResolvedValueOnce({ ...PROFILE, name: "Ana María" });

    await changeName("Ana María");
    await userEvent.click(saveButton());

    await screen.findByRole("status");
    expect(saveButton()).toBeDisabled();
  });

  it("disables the button while saving", async () => {
    await renderLoaded();
    apiFetch.mockReturnValueOnce(new Promise(() => {})); // never resolves

    await changeName("Otra");
    await userEvent.click(saveButton());

    expect(screen.getByRole("button", { name: "Guardando…" })).toBeDisabled();
  });
});
