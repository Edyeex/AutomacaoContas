"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import AppBrand from "./components/AppBrand";
import { apiRequest, saveSession } from "./lib/apiClient";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [error, setError] = useState("");

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
          <form onSubmit={handleSubmit}>
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
            <div className="form-group">
              <label htmlFor="senha">Senha</label>
              <input
                id="senha"
                className="form-input"
                type="password"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
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
