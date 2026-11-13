"use client";

import { GoogleLogin, GoogleOAuthProvider } from "@react-oauth/google";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiFetch } from "../../services/api";
import { saveToken } from "../../services/auth";

type AuthResult = { accessToken: string; name: string; isNewUser: boolean };

export default function LoginPage() {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);

  async function handleLogin(idToken?: string) {
    if (!idToken) {
      setError("No pudimos obtener tu cuenta de Google.");
      return;
    }
    try {
      const result = await apiFetch<AuthResult>("/api/auth/google", {
        method: "POST",
        body: JSON.stringify({ idToken }),
      });
      saveToken(result.accessToken);
      router.replace("/home");
    } catch {
      setError("No pudimos iniciar sesión. Probá de nuevo.");
    }
  }

  return (
    <GoogleOAuthProvider clientId={process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID ?? ""}>
      <main className="flex flex-1 items-center justify-center p-6">
        <div className="flex w-full max-w-sm flex-col items-center gap-6 rounded-lg border border-slate-200 p-8">
          <h1 className="text-2xl font-bold">Ingresá a Kioskitos</h1>
          <GoogleLogin
            onSuccess={(c) => handleLogin(c.credential)}
            onError={() => setError("No pudimos conectar con Google.")}
          />
          {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
        </div>
      </main>
    </GoogleOAuthProvider>
  );
}