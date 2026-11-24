import { apiFetch } from "./api";
import { getToken } from "./auth";

export type ClientContact = { id: string; name: string; email: string; phone: string };

export type Client = {
  id: string;
  name: string;
  industry: string | null;
  notes: string | null;
  createdAt: string;
  contacts: ClientContact[];
};

/** Row of the clients list: only the primary contact (the first one registered). */
export type ClientSummary = {
  id: string;
  name: string;
  industry: string | null;
  primaryContact: ClientContact | null;
};

/** Body to register or update a client. */
export type ClientInput = {
  name: string;
  industry: string | null;
  notes: string | null;
  contacts: { name: string; email: string; phone: string }[];
};

const authHeaders = () => ({ Authorization: `Bearer ${getToken()}` });

export const registerClient = (data: ClientInput) =>
  apiFetch<Client>("/api/clients", {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify(data),
  });

export const updateClient = (id: string, data: ClientInput) =>
  apiFetch<Client>(`/api/clients/${encodeURIComponent(id)}`, {
    method: "PUT",
    headers: authHeaders(),
    body: JSON.stringify(data),
  });

export const listClients = () =>
  apiFetch<ClientSummary[]>("/api/clients", { headers: authHeaders() });

export const getClient = (id: string) =>
  apiFetch<Client>(`/api/clients/${encodeURIComponent(id)}`, { headers: authHeaders() });
