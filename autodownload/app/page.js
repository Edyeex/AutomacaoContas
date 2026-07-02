"use client";
import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import AppBrand from "./components/AppBrand";
import PasswordInput from "./components/PasswordInput";
import ThemeToggle from "./components/ThemeToggle";
import { apiRequest, saveSession } from "./lib/apiClient";

export default function LoginPage() {
  const router = useRouter();
  const formRef = useRef(null);
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState({});
  const [validationAttempt, setValidationAttempt] = useState(0);

  useEffect(() => {
    function clearLoginFields() {
      setEmail("");
      setSenha("");
      setFieldErrors({});
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

  function markInvalidCredentials() {
    const message = "E-mail ou senha incorretos.";

    setFieldErrors({
      email: message,
      senha: message,
    });
    setValidationAttempt((current) => current + 1);
    setError(message);
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    const currentEmail = email || document.getElementById("login-email")?.value || "";
    const currentPassword = senha || document.getElementById("login-password")?.value || "";
    const nextFieldErrors = {};

    if (!currentEmail.trim()) {
      nextFieldErrors.email = "E-mail é obrigatório.";
    }

    if (!currentPassword) {
      nextFieldErrors.senha = "Senha é obrigatória.";
    }

    if (Object.keys(nextFieldErrors).length > 0) {
      setFieldErrors(nextFieldErrors);
      setValidationAttempt((current) => current + 1);
      setError("Preencha os campos obrigatórios.");
      return;
    }

    try {
      const auth = await apiRequest("/auth/login", {
        method: "POST",
        auth: false,
        body: { email: currentEmail, password: currentPassword },
      });
      saveSession(auth);
      router.push("/dashboard");
    } catch (err) {
      if (err?.code === "auth.invalid_credentials" || err?.status === 401) {
        markInvalidCredentials();
        return;
      }

      setError(err.message || "Não foi possível entrar.");
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-theme-control">
        <ThemeToggle />
      </div>

      <div className="auth-container">
        <div className="auth-logo">
          <AppBrand variant="auth" />
        </div>

        <div className="auth-box">
          <h2>Entrar na sua conta</h2>
          {error && (
            <p style={{ color: "var(--danger)", fontSize: 13, marginBottom: 12 }}>{error}</p>
          )}
          <form ref={formRef} onSubmit={handleSubmit} autoComplete="off" noValidate>
            <div className="form-group">
              <label htmlFor="login-email">E-mail</label>
              <input
                key={`login-email-${validationAttempt}`}
                id="login-email"
                name="autodownload-login-email"
                className={`form-input ${fieldErrors.email ? "is-invalid" : ""}`}
                type="email"
                value={email}
                onChange={(e) => {
                  setEmail(e.target.value);
                  setFieldErrors((prev) => ({ ...prev, email: "" }));
                  setError("");
                }}
                placeholder="seu@email.com"
                autoComplete="off"
                autoCorrect="off"
                autoCapitalize="none"
                spellCheck="false"
                aria-invalid={Boolean(fieldErrors.email)}
              />
              {fieldErrors.email && <p className="field-error">{fieldErrors.email}</p>}
            </div>
            <div className="form-group">
              <label htmlFor="login-password">Senha</label>
              <PasswordInput
                key={`login-password-${validationAttempt}`}
                id="login-password"
                name="autodownload-login-password"
                inputClassName={`form-input ${fieldErrors.senha ? "is-invalid" : ""}`}
                value={senha}
                onChange={(e) => {
                  setSenha(e.target.value);
                  setFieldErrors((prev) => ({ ...prev, senha: "" }));
                  setError("");
                }}
                autoComplete="new-password"
                placeholder="••••••••"
                aria-invalid={Boolean(fieldErrors.senha)}
              />
              {fieldErrors.senha && <p className="field-error">{fieldErrors.senha}</p>}
            </div>
            <div style={{ marginBottom: 16, textAlign: "right" }}>
              <button
                type="button"
                className="btn-link"
                onClick={() => router.push("/recuperar-senha")}
              >
                Solicitar suporte para recuperação
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
