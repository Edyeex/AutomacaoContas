"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import AppBrand from "../components/AppBrand";
import { apiRequest, saveSession } from "../lib/apiClient";

export default function CadastroPage() {
  const router = useRouter();
  const [form, setForm] = useState({ nome: "", email: "", senha: "", confirmar: "" });
  const [error, setError] = useState("");

  function update(field, value) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    if (!form.nome || !form.email || !form.senha || !form.confirmar) {
      setError("Preencha todos os campos.");
      return;
    }
    if (form.senha !== form.confirmar) {
      setError("As senhas não coincidem.");
      return;
    }
    if (form.senha.length < 6) {
      setError("A senha deve ter no mínimo 6 caracteres.");
      return;
    }
    try {
      const auth = await apiRequest("/auth/register", {
        method: "POST",
        auth: false,
        body: { name: form.nome, email: form.email, password: form.senha },
      });
      saveSession(auth);
      router.push("/dashboard");
    } catch (err) {
      setError(err.message || "Não foi possível criar a conta.");
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-container">
        <div className="auth-logo">
          <AppBrand variant="auth" />
        </div>

        <div className="auth-box">
          <h2>Criar uma conta</h2>
          {error && (
            <p style={{ color: "var(--danger)", fontSize: 13, marginBottom: 12 }}>{error}</p>
          )}
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="nome">Nome completo</label>
              <input
                id="nome"
                className="form-input"
                type="text"
                value={form.nome}
                onChange={(e) => update("nome", e.target.value)}
              />
            </div>
            <div className="form-group">
              <label htmlFor="email">E-mail</label>
              <input
                id="email"
                className="form-input"
                type="email"
                value={form.email}
                onChange={(e) => update("email", e.target.value)}
              />
            </div>
            <div className="form-group">
              <label htmlFor="senha">Senha</label>
              <input
                id="senha"
                className="form-input"
                type="password"
                value={form.senha}
                onChange={(e) => update("senha", e.target.value)}
              />
              <p className="form-hint">A senha deve conter no mínimo 6 caracteres</p>
            </div>
            <div className="form-group">
              <label htmlFor="confirmar">Confirmar senha</label>
              <input
                id="confirmar"
                className="form-input"
                type="password"
                value={form.confirmar}
                onChange={(e) => update("confirmar", e.target.value)}
              />
            </div>
            <button type="submit" className="btn btn-primary btn-block">
              Criar conta
            </button>
          </form>
        </div>

        <div className="auth-footer">
          Já tem conta?{" "}
          <a href="/">Entrar</a>
        </div>
      </div>
    </div>
  );
}
