import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "../services/apiError";
import { ClientProjectsList } from "./ClientProjectsList";

const apiFetch = vi.fn();

vi.mock("../services/api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

describe("ClientProjectsList", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.setItem("kioskitos_token", "jwt");
  });

  it("lists the client's projects with status and due date", async () => {
    apiFetch.mockResolvedValueOnce([
      { id: "p1", name: "Website", status: "InProgress", createdAt: "", dueDate: "2027-03-01", tags: [] },
    ]);

    render(<ClientProjectsList clientId="c1" />);

    const link = await screen.findByRole("link", { name: "Website" });
    expect(link).toHaveAttribute("href", "/projects/p1");
    expect(screen.getByText("En curso · Entrega: 01/03/2027")).toBeInTheDocument();
    expect(apiFetch).toHaveBeenCalledWith(
      "/api/clients/c1/projects",
      expect.objectContaining({ headers: { Authorization: "Bearer jwt" } }),
    );
  });

  it("shows an empty message when there are no projects", async () => {
    apiFetch.mockResolvedValueOnce([]);

    render(<ClientProjectsList clientId="c1" />);

    expect(await screen.findByText("Este cliente todavía no tiene proyectos.")).toBeInTheDocument();
  });

  it("shows an error when the client does not exist", async () => {
    apiFetch.mockRejectedValueOnce(new ApiError(404, null));

    render(<ClientProjectsList clientId="c1" />);

    expect(await screen.findByRole("alert")).toHaveTextContent("El cliente no existe.");
  });
});
