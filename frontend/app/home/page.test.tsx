import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import HomePage from "./page";

const PROFILE = {
  id: "1",
  email: "ana@example.com",
  name: "Ana",
  industry: null,
  hourlyRate: null,
  createdAt: "2026-01-01T00:00:00Z",
};
const replace = vi.fn();
const router = { replace };
const apiFetch = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => router }));
vi.mock("../../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

describe("HomePage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it("goes to login when there is no token", () => {
    render(<HomePage />);

    expect(replace).toHaveBeenCalledWith("/login");
    expect(apiFetch).not.toHaveBeenCalled();
  });

  it("greets the user and shows the shortcuts", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    apiFetch.mockResolvedValueOnce(PROFILE);
    render(<HomePage />);

    expect(await screen.findByRole("heading", { name: "Hola, Ana" })).toBeInTheDocument();
    expect(screen.getByText("ana@example.com")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Google Calendar/ })).toHaveAttribute("href", "/calendar");
    expect(screen.getByRole("link", { name: /Mis clientes/ })).toHaveAttribute("href", "/clients");
    expect(screen.getByRole("link", { name: /Registrar cliente/ })).toHaveAttribute("href", "/clients/new");
    expect(screen.getAllByRole("link", { name: /Mi perfil/ })[0]).toHaveAttribute("href", "/profile");
  });

  it("clears the token and goes to login when the profile request fails", async () => {
    localStorage.setItem("kioskitos_token", "expired");
    apiFetch.mockRejectedValueOnce(new Error("401"));
    render(<HomePage />);

    await vi.waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(localStorage.getItem("kioskitos_token")).toBeNull();
  });
});
