import { beforeEach, describe, expect, it, vi } from "vitest";

import { deleteClient, getClient, listClients, updateClient } from "./clients";

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

  it("updates a client with PUT and the bearer token", async () => {
    apiFetch.mockResolvedValueOnce({});
    const data = { name: "Acme", industry: null, notes: null, contacts: [] };

    await updateClient("abc", data);

    expect(apiFetch).toHaveBeenCalledWith("/api/clients/abc", {
      method: "PUT",
      headers: { Authorization: "Bearer jwt" },
      body: JSON.stringify(data),
    });
  });

  it("deletes a client with DELETE and the bearer token", async () => {
    apiFetch.mockResolvedValueOnce(undefined);

    await deleteClient("abc");

    expect(apiFetch).toHaveBeenCalledWith("/api/clients/abc", {
      method: "DELETE",
      headers: { Authorization: "Bearer jwt" },
    });
  });
});
