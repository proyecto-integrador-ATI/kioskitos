import { apiFetch } from "./api";
import { getToken } from "./auth";

export type UserProfile = {
  id: string;
  email: string;
  name: string;
  industry: string | null;
  hourlyRate: number | null;
  createdAt: string;
};

/** Only the fields the user is allowed to edit. */
export type UpdateUserProfile = {
  name: string;
  industry: string | null;
  hourlyRate: number | null;
};

const authHeaders = () => ({ Authorization: `Bearer ${getToken()}` });

export const getMyProfile = () =>
  apiFetch<UserProfile>("/api/users/me", { headers: authHeaders() });

export const updateMyProfile = (data: UpdateUserProfile) =>
  apiFetch<UserProfile>("/api/users/me", {
    method: "PUT",
    headers: authHeaders(),
    body: JSON.stringify(data),
  });
