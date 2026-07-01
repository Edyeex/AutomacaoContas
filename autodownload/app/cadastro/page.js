"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import AppBrand from "../components/AppBrand";
import PasswordInput from "../components/PasswordInput";
import { apiRequest, saveSession } from "../lib/apiClient";

export default function CadastroPage() {
  const router = useRouter();
  const [form, setForm] = useState({ nome: "", email: "", senha: "", confirmar: "" });
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState({});

  function update(field, value) {
    setForm((prev) => ({ ...prev, [field]: value }));
    setFieldErrors((prev) => ({ ...prev, [field]: "" }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    const nextFieldErrors = {};

    if (!form.nome.trim()) {
      nextFieldErrors.nome = "Nome completo é obrigatório.";
    }

    if (!form.email.trim()) {
      nextFieldErrors.email = "E-mail é obrigatório.";
    }

    if (!form.senha) {
      nextFieldErrors.senha = "Senha é obrigatória.";
    } else if (form.senha.length < 6) {
      nextFieldErrors.senha = "Senha deve ter no mínimo 6 caracteres.";
    }

    if (!form.confirmar) {
      nextFieldErrors.confirmar = "Confirmar senha é obrigatório.";
    } else if (form.senha && form.senha !== form.confirmar) {
      nextFieldErrors.confirmar = "Confirmação de senha não confere.";
    }

    if (Object.keys(nextFieldErrors).length > 0) {
      setFieldErrors(nextFieldErrors);
      setError("Revise os campos destacados.");
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
                className={`form-input ${fieldErrors.nome ? "is-invalid" : ""}`}
                type="text"
                value={form.nome}
                onChange={(e) => update("nome", e.target.value)}
                aria-invalid={Boolean(fieldErrors.nome)}
              />
              {fieldErrors.nome && <p className="field-error">{fieldErrors.nome}</p>}
            </div>
            <div className="form-group">
              <label htmlFor="email">E-mail</label>
              <input
                id="email"
                className={`form-input ${fieldErrors.email ? "is-invalid" : ""}`}
                type="email"
                value={form.email}
                onChange={(e) => update("email", e.target.value)}
                aria-invalid={Boolean(fieldErrors.email)}
              />
              {fieldErrors.email && <p className="field-error">{fieldErrors.email}</p>}
            </div>
            <div className="form-group">
              <label htmlFor="senha">Senha</label>
              <PasswordInput
                id="senha"
                inputClassName={`form-input ${fieldErrors.senha ? "is-invalid" : ""}`}
                value={form.senha}
                onChange={(e) => update("senha", e.target.value)}
                aria-invalid={Boolean(fieldErrors.senha)}
              />
              {fieldErrors.senha && <p className="field-error">{fieldErrors.senha}</p>}
              <p className="form-hint">A senha deve conter no mínimo 6 caracteres</p>
            </div>
            <div className="form-group">
              <label htmlFor="confirmar">Confirmar senha</label>
              <PasswordInput
                id="confirmar"
                inputClassName={`form-input ${fieldErrors.confirmar ? "is-invalid" : ""}`}
                value={form.confirmar}
                onChange={(e) => update("confirmar", e.target.value)}
                aria-invalid={Boolean(fieldErrors.confirmar)}
              />
              {fieldErrors.confirmar && <p className="field-error">{fieldErrors.confirmar}</p>}
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
