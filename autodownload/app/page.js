"use client";
import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import AppBrand from "./components/AppBrand";
import { apiRequest, saveSession } from "./lib/apiClient";

export default function LoginPage() {
  const router = useRouter();
  const formRef = useRef(null);
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    function clearLoginFields() {
      setEmail("");
      setSenha("");
      formRef.current?.reset();

      const emailInput = document.getElementById("login-email");
      const passwordInput = document.getElementById("login-password");

      if (emailInput) emailInput.value = "";
      if (passwordInput) passwordInput.value = "";
    }

    clearLoginFields();
    const autofillTimer = window.setTimeout(clearLoginFields, 150);
    window.addEventListener("pageshow", clearLoginFields);

    return () => {
      window.clearTimeout(autofillTimer);
      window.removeEventListener("pageshow", clearLoginFields);
    };
  }, []);

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    if (!email || !senha) {
      setError("Preencha todos os campos.");
      return;
    }

    try {
      const auth = await apiRequest("/auth/login", {
        method: "POST",
        auth: false,
        body: { email, password: senha },
      });
      saveSession(auth);
      router.push("/dashboard");
    } catch (err) {
      setError(err.message || "Não foi possível entrar.");
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-container">
        <div className="auth-logo">
          <AppBrand variant="auth" />
        </div>

        <div className="auth-box">
          <h2>Entrar na sua conta</h2>
          {error && (
            <p style={{ color: "var(--danger)", fontSize: 13, marginBottom: 12 }}>{error}</p>
          )}
          <form ref={formRef} onSubmit={handleSubmit} autoComplete="off">
            <div className="form-group">
              <label htmlFor="login-email">E-mail</label>
              <input
                id="login-email"
                name="autodownload-login-email"
                className="form-input"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="seu@email.com"
                autoComplete="off"
                autoCorrect="off"
                autoCapitalize="none"
                spellCheck="false"
              />
            </div>
            <div className="form-group">
              <label htmlFor="login-password">Senha</label>
              <input
                id="login-password"
                name="autodownload-login-password"
                className="form-input"
                type="password"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                autoComplete="new-password"
                placeholder="••••••••"
              />
            </div>
            <div style={{ marginBottom: 16, textAlign: "right" }}>
              <button
                type="button"
                className="btn-link"
                onClick={() => router.push("/recuperar-senha")}
              >
                Esqueceu a senha?
              </button>
            </div>
            <button type="submit" className="btn btn-primary btn-block">
              Entrar
            </button>
          </form>
        </div>

        <div className="auth-footer">
          Não tem conta?{" "}
          <a href="/cadastro">Criar conta</a>
        </div>
      </div>
    </div>
  );
}
