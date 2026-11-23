import { beforeEach, describe, expect, it, vi } from "vitest";

import { getClient, listClients } from "./clients";

const apiFetch = vi.fn();

vi.mock("./api", () => ({
  apiFetch: (...args: unknown[]) => apiFetch(...args),
}));

describe("clients service", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.setItem("kioskitos_token", "jwt");
  });

  it("lists the clients with the bearer token", async () => {
    apiFetch.mockResolvedValueOnce([]);

    await listClients();

    expect(apiFetch).toHaveBeenCalledWith("/api/clients", {
      headers: { Authorization: "Bearer jwt" },
    });
  });

  it("gets a client by id with the bearer token", async () => {
    apiFetch.mockResolvedValueOnce({});

    await getClient("abc");

    expect(apiFetch).toHaveBeenCalledWith("/api/clients/abc", {
      headers: { Authorization: "Bearer jwt" },
    });
  });
});
