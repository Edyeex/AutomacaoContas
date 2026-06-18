"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import AppBrand from "../components/AppBrand";
import { apiRequest } from "../lib/apiClient";

export default function RecuperarSenhaPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    if (email) {
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
  }

  return (
    <div className="auth-page">
      <div className="auth-container">
        <div className="auth-logo">
          <AppBrand variant="auth" />
        </div>

        <div className="auth-box">
          <h2>Recuperar senha</h2>
          {sent ? (
            <div>
              <p style={{ fontSize: 13, color: "var(--text)", marginBottom: 16 }}>
                Enviamos um link de recuperação para <strong>{email}</strong>. Verifique sua caixa de entrada.
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
                Informe seu e-mail cadastrado para receber instruções de recuperação.
              </p>
              <div className="form-group">
                <label htmlFor="email">E-mail</label>
                <input
                  id="email"
                  className="form-input"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="seu@email.com"
                />
              </div>
              <button type="submit" className="btn btn-primary btn-block">
                Enviar link de recuperação
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
