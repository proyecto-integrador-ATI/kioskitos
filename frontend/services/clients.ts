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

export type RegisterClient = {
  name: string;
  industry: string | null;
  notes: string | null;
  contacts: { name: string; email: string; phone: string }[];
};

const authHeaders = () => ({ Authorization: `Bearer ${getToken()}` });

export const registerClient = (data: RegisterClient) =>
  apiFetch<Client>("/api/clients", {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify(data),
  });
