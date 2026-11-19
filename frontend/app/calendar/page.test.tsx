import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../../services/apiError";
import CalendarPage from "./page";

const ME = { userId: "1", email: "ana@example.com", name: "Ana" };
const replace = vi.fn();
const router = { replace }; // stable reference, like Next's real router
const apiFetch = vi.fn();
const googleLoginConfig = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => router }));
vi.mock("../../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));
vi.mock("@react-oauth/google", () => ({
  GoogleOAuthProvider: ({ children }: { children: ReactNode }) => children,
  // Simulates Google returning an authorization code on click.
  useGoogleLogin: (config: { onSuccess: (r: { code: string }) => void }) => {
    googleLoginConfig(config);
    return () => config.onSuccess({ code: "google-code" });
  },
}));

describe("CalendarPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it("redirects to login when there is no session", () => {
    render(<CalendarPage />);
    expect(replace).toHaveBeenCalledWith("/login");
  });

  it("sends the code to the backend and shows the calendar id", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    apiFetch
      .mockResolvedValueOnce(ME)
      .mockResolvedValueOnce({ calendarId: "abc@group.calendar.google.com", connected: true });
    render(<CalendarPage />);

    await userEvent.click(await screen.findByRole("button", { name: "Conectar Google Calendar" }));

    expect(apiFetch).toHaveBeenCalledWith(
      "/api/google-calendar/connect",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ code: "google-code" }),
        headers: { Authorization: "Bearer jwt" },
      }),
    );
    expect(await screen.findByRole("status")).toHaveTextContent("abc@group.calendar.google.com");
    expect(googleLoginConfig).toHaveBeenCalledWith(
      expect.objectContaining({ flow: "auth-code", include_granted_scopes: false }),
    );
  });

  it("shows a friendly message for a backend error code", async () => {
    localStorage.setItem("kioskitos_token", "jwt");
    apiFetch
      .mockResolvedValueOnce(ME)
      .mockRejectedValueOnce(new ApiError(400, { errorCode: "missing_calendar_scope" }));
    render(<CalendarPage />);

    await userEvent.click(await screen.findByRole("button", { name: "Conectar Google Calendar" }));

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("No diste el permiso de Google Calendar");
    expect(alert).toHaveTextContent("missing_calendar_scope");
  });
});
