"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import AppBrand from "../components/AppBrand";
import { apiRequest } from "../lib/apiClient";

export default function RecuperarSenhaPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);
  const [fieldErrors, setFieldErrors] = useState({});

  async function handleSubmit(e) {
    e.preventDefault();
    if (!email.trim()) {
      setFieldErrors({ email: "E-mail é obrigatório." });
      return;
    }

    try {
      await apiRequest("/auth/recover-password", {
        method: "POST",
        auth: false,
        body: { email },
      });
    } finally {
      setSent(true);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-container">
        <div className="auth-logo">
          <AppBrand variant="auth" />
        </div>

        <div className="auth-box">
          <h2>Suporte para recuperação</h2>
          {sent ? (
            <div>
              <p style={{ fontSize: 13, color: "var(--text)", marginBottom: 16 }}>
                Registramos sua solicitação para <strong>{email}</strong>. O suporte deverá orientar a recuperação da conta.
              </p>
              <button
                type="button"
                className="btn btn-secondary btn-block"
                onClick={() => router.push("/")}
              >
                Voltar ao login
              </button>
            </div>
          ) : (
            <form onSubmit={handleSubmit}>
              <p style={{ fontSize: 13, color: "var(--text-muted)", marginBottom: 16 }}>
                Informe seu e-mail cadastrado para registrar uma solicitação de suporte para recuperação da conta.
              </p>
              <div className="form-group">
                <label htmlFor="email">E-mail</label>
                <input
                  id="email"
                  className={`form-input ${fieldErrors.email ? "is-invalid" : ""}`}
                  type="email"
                  value={email}
                  onChange={(e) => {
                    setEmail(e.target.value);
                    setFieldErrors((prev) => ({ ...prev, email: "" }));
                  }}
                  placeholder="seu@email.com"
                  aria-invalid={Boolean(fieldErrors.email)}
                />
                {fieldErrors.email && <p className="field-error">{fieldErrors.email}</p>}
              </div>
              <button type="submit" className="btn btn-primary btn-block">
                Solicitar suporte para recuperação
              </button>
            </form>
          )}
        </div>

        <div className="auth-footer">
          Lembrou a senha?{" "}
          <a href="/">Entrar</a>
        </div>
      </div>
    </div>
  );
}
