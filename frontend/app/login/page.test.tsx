import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import LoginPage from "./page";

const replace = vi.fn();
const apiFetch = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => ({ replace }) }));
vi.mock("../../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));
vi.mock("@react-oauth/google", () => ({
  GoogleOAuthProvider: ({ children }: { children: ReactNode }) => children,
  GoogleLogin: ({ onSuccess }: { onSuccess: (c: { credential?: string }) => void }) => (
    <button onClick={() => onSuccess({ credential: "google-id-token" })}>Google</button>
  ),
}));

describe("LoginPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it("saves the token and goes to home after a successful login", async () => {
    apiFetch.mockResolvedValue({ accessToken: "jwt", name: "Ana", isNewUser: false });
    render(<LoginPage />);

    await userEvent.click(screen.getByRole("button", { name: "Google" }));

    expect(apiFetch).toHaveBeenCalledWith("/api/auth/google", expect.objectContaining({ method: "POST" }));
    expect(localStorage.getItem("kioskitos_token")).toBe("jwt");
    expect(replace).toHaveBeenCalledWith("/home");
  });

  it("goes straight to home when a token is already saved", () => {
    localStorage.setItem("kioskitos_token", "jwt");
    render(<LoginPage />);

    expect(replace).toHaveBeenCalledWith("/home");
  });

  it("stays on login when there is no token", () => {
    render(<LoginPage />);

    expect(replace).not.toHaveBeenCalled();
  });

  it("shows an error when the backend rejects the login", async () => {
    apiFetch.mockRejectedValue(new Error("401"));
    render(<LoginPage />);

    await userEvent.click(screen.getByRole("button", { name: "Google" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos iniciar sesión");
    expect(replace).not.toHaveBeenCalled();
  });
});