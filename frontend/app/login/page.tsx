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
      <main className="flex min-h-screen flex-1 flex-col md:flex-row">
        <section className="flex min-w-0 flex-1 flex-col items-start md:basis-0 justify-center gap-[10px] bg-green px-8 py-16 md:px-[72px]">
          <div className="border-[5px] border-red bg-cream px-[10px]">
            <p className="text-header1 text-blue">KIOSKITOS</p>
          </div>
          <p className="max-w-[374px] text-special-header text-ash">
            Ordená todos tus proyectos en un solo lugar
          </p>
        </section>

        <section className="flex min-w-0 flex-1 items-center md:basis-0 justify-center bg-ash p-6">
          <div className="flex max-w-full flex-col items-center gap-[10px] rounded-[20px] border border-green p-3 text-green">
            <div className="flex flex-col items-center gap-[10px] px-6 py-3 text-center md:whitespace-nowrap">
              <h1 className="text-header2">Ingresá a Kioskitos</h1>
              <p className="text-normal">
                <span className="md:block">Iniciá sesión en tu cuenta. Si no está creada, </span>
                <span className="md:block">Kioskitos la creará automáticamente.</span>
              </p>
            </div>
            <div className="flex w-full flex-col items-center gap-[10px]">
              <GoogleLogin
                text="continue_with"
                onSuccess={(c) => handleLogin(c.credential)}
                onError={() => setError("No pudimos conectar con Google.")}
              />
              <p className="text-center text-small text-blue">
                Al continuar, aceptás los Términos de uso y la Política de privacidad
              </p>
              {error && (
                <p role="alert" className="text-center text-sm text-red">
                  {error}
                </p>
              )}
            </div>
          </div>
        </section>
      </main>
    </GoogleOAuthProvider>
  );
}