"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";

export default function RecuperarSenhaPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);

  function handleSubmit(e) {
    e.preventDefault();
    if (email) {
      setSent(true);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-container">
        <div className="auth-logo">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
            <polyline points="7 10 12 15 17 10" />
            <line x1="12" y1="15" x2="12" y2="3" />
          </svg>
          <h1>AutoDownload</h1>
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
