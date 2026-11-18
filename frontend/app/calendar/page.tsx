"use client";

import { GoogleOAuthProvider, useGoogleLogin } from "@react-oauth/google";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { apiFetch } from "../../services/api";
import { ApiError } from "../../services/apiError";
import { clearToken, getToken } from "../../services/auth";

// Same scope as GoogleCalendarScopes.AppCreated in the backend (least privilege).
const CALENDAR_SCOPE = "https://www.googleapis.com/auth/calendar.app.created";

type ConnectResponse = { calendarId: string; connected: boolean };

type Status =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "success"; calendarId: string }
  | { kind: "error"; message: string; errorCode?: string };

const ERROR_MESSAGES: Record<string, string> = {
  missing_calendar_scope:
    "No diste el permiso de Google Calendar. Volvé a intentarlo y marcá la casilla del calendario.",
  missing_refresh_token:
    "Google no devolvió el permiso permanente. Quitá Kioskitos de los accesos de tu cuenta de Google y probá de nuevo.",
  invalid_code: "El código de Google venció o no es válido. Probá de nuevo.",
};

function ConnectCalendar() {
  const [status, setStatus] = useState<Status>({ kind: "idle" });

  async function connect(code: string) {
    setStatus({ kind: "loading" });
    try {
      const result = await apiFetch<ConnectResponse>("/api/google-calendar/connect", {
        method: "POST",
        headers: { Authorization: `Bearer ${getToken()}` },
        body: JSON.stringify({ code }),
      });
      setStatus({ kind: "success", calendarId: result.calendarId });
    } catch (e) {
      const errorCode = e instanceof ApiError ? e.problem?.errorCode : undefined;
      const message =
        (errorCode && ERROR_MESSAGES[errorCode]) ??
        (e instanceof ApiError && e.status === 401
          ? "Tu sesión venció. Volvé a iniciar sesión."
          : "No pudimos conectar Google Calendar.");
      setStatus({ kind: "error", message, errorCode });
    }
  }

  // "auth-code" flow in a popup: Google returns a code that the backend exchanges for tokens
  // (redirect_uri "postmessage"), so the refresh token never reaches the browser.
  const login = useGoogleLogin({
    flow: "auth-code",
    scope: CALENDAR_SCOPE,
    onSuccess: (r) => connect(r.code),
    onError: (r) =>
      setStatus({ kind: "error", message: "Google rechazó la autorización.", errorCode: r.error }),
    onNonOAuthError: () =>
      setStatus({ kind: "error", message: "Se cerró la ventana de Google antes de terminar." }),
  });

  return (
    <div className="mt-6 flex flex-col gap-4">
      <button
        onClick={() => login()}
        disabled={status.kind === "loading"}
        className="w-fit rounded-md bg-slate-900 px-4 py-2 text-sm text-white hover:bg-slate-700 disabled:opacity-50"
      >
        {status.kind === "loading" ? "Conectando…" : "Conectar Google Calendar"}
      </button>

      {status.kind === "success" && (
        <div role="status" className="rounded-md border border-green-300 bg-green-50 p-4 text-sm">
          <p className="font-medium text-green-800">¡Listo! Tu calendario “Kioskitos” está conectado.</p>
          <p className="mt-1 break-all text-green-900">
            Id del calendario: <code>{status.calendarId}</code>
          </p>
          <a
            href="https://calendar.google.com"
            target="_blank"
            rel="noreferrer"
            className="mt-2 inline-block text-green-800 underline"
          >
            Abrir Google Calendar
          </a>
        </div>
      )}

      {status.kind === "error" && (
        <div role="alert" className="rounded-md border border-red-300 bg-red-50 p-4 text-sm text-red-800">
          <p>{status.message}</p>
          {status.errorCode && (
            <p className="mt-1 text-red-700">
              Código: <code>{status.errorCode}</code>
            </p>
          )}
        </div>
      )}
    </div>
  );
}

type Me = { userId: string; email: string; name: string };

export default function CalendarPage() {
  const router = useRouter();
  const [me, setMe] = useState<Me | null>(null);

  useEffect(() => {
    const token = getToken();
    if (!token) {
      router.replace("/login");
      return;
    }
    apiFetch<Me>("/api/auth/me", { headers: { Authorization: `Bearer ${token}` } })
      .then(setMe)
      .catch(() => {
        clearToken(); // expired or invalid token
        router.replace("/login");
      });
  }, [router]);

  if (!me) return <p className="p-8 text-slate-600">Cargando…</p>;

  return (
    <GoogleOAuthProvider clientId={process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID ?? ""}>
      <main className="mx-auto w-full max-w-3xl p-8">
        <Link href="/home" className="text-sm text-slate-600 hover:underline">
          ← Volver al inicio
        </Link>
        <h1 className="mt-4 text-2xl font-bold">Google Calendar</h1>
        <p className="mt-2 text-slate-600">
          Conectá tu cuenta para que Kioskitos cree el calendario “Kioskitos” en tu Google Calendar.
        </p>
        <p className="mt-1 text-sm text-slate-500">Sesión iniciada como {me.email}</p>
        <ConnectCalendar />
      </main>
    </GoogleOAuthProvider>
  );
}
