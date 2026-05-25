"use client";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { currentUser } from "../../lib/mockData";
import { apiRequest, clearSession, getSession, saveSession } from "../../lib/apiClient";
import { useApiResource } from "../../lib/useApiResource";

export default function ConfiguracoesPage() {
  const router = useRouter();
  const { data: profile, usingFallback, reload } = useApiResource("/me", getSession()?.user || currentUser);
  const [form, setForm] = useState({
    nome: currentUser.name,
    email: currentUser.email,
  });
  const [senhaForm, setSenhaForm] = useState({
    atual: "",
    nova: "",
    confirmar: "",
  });
  const [saved, setSaved] = useState(false);
  const [senhaSaved, setSenhaSaved] = useState(false);
  const [error, setError] = useState("");
  const [senhaError, setSenhaError] = useState("");

  useEffect(() => {
    setForm({
      nome: profile?.name || currentUser.name,
      email: profile?.email || currentUser.email,
    });
  }, [profile]);

  function updateForm(field, value) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  function updateSenha(field, value) {
    setSenhaForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSaveProfile(e) {
    e.preventDefault();
    setError("");

    if (usingFallback) {
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
      return;
    }

    try {
      const updated = await apiRequest("/me", {
        method: "PUT",
        body: { name: form.nome, email: form.email },
      });
      const session = getSession();
      if (session) {
        saveSession({ ...session, user: updated });
      }
      await reload();
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    } catch (err) {
      setError(err.message || "Não foi possível salvar o perfil.");
    }
  }

  async function handleSavePassword(e) {
    e.preventDefault();
    setSenhaError("");

    if (senhaForm.nova !== senhaForm.confirmar) {
      setSenhaError("As senhas não coincidem.");
      return;
    }

    if (usingFallback) {
      setSenhaSaved(true);
      setSenhaForm({ atual: "", nova: "", confirmar: "" });
      setTimeout(() => setSenhaSaved(false), 2000);
      return;
    }

    try {
      await apiRequest("/me/password", {
        method: "PUT",
        body: { currentPassword: senhaForm.atual, newPassword: senhaForm.nova },
      });
      setSenhaSaved(true);
      setSenhaForm({ atual: "", nova: "", confirmar: "" });
      setTimeout(() => setSenhaSaved(false), 2000);
    } catch (err) {
      setSenhaError(err.message || "Não foi possível alterar a senha.");
    }
  }

  function handleLogout() {
    clearSession();
    router.push("/");
  }

  return (
    <div className="settings-page">
      <div className="page-header">
        <h1>Configurações</h1>
      </div>

      <div className="page-body" style={{ maxWidth: 600 }}>
        <div className="settings-section">
          <h3>Perfil</h3>
          <p>Informações da sua conta.</p>
          {error && <p style={{ color: "var(--danger)", fontSize: 13 }}>{error}</p>}
          <form onSubmit={handleSaveProfile}>
            <div className="settings-row">
              <div className="form-group">
                <label>Nome completo</label>
                <input
                  className="form-input"
                  type="text"
                  value={form.nome}
                  onChange={(e) => updateForm("nome", e.target.value)}
                />
              </div>
              <div className="form-group">
                <label>E-mail</label>
                <input
                  className="form-input"
                  type="email"
                  value={form.email}
                  onChange={(e) => updateForm("email", e.target.value)}
                />
              </div>
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
              <button type="submit" className="btn btn-primary btn-sm">
                Salvar alterações
              </button>
              {saved && (
                <span style={{ fontSize: 13, color: "var(--success)" }}>Salvo.</span>
              )}
            </div>
          </form>
        </div>

        <hr className="settings-divider" />

        <div className="settings-section">
          <h3>Alterar senha</h3>
          <p>Use uma senha forte com pelo menos 6 caracteres.</p>
          {senhaError && <p style={{ color: "var(--danger)", fontSize: 13 }}>{senhaError}</p>}
          <form onSubmit={handleSavePassword}>
            <div className="form-group">
              <label>Senha atual</label>
              <input
                className="form-input"
                type="password"
                value={senhaForm.atual}
                onChange={(e) => updateSenha("atual", e.target.value)}
              />
            </div>
            <div className="settings-row">
              <div className="form-group">
                <label>Nova senha</label>
                <input
                  className="form-input"
                  type="password"
                  value={senhaForm.nova}
                  onChange={(e) => updateSenha("nova", e.target.value)}
                />
              </div>
              <div className="form-group">
                <label>Confirmar nova senha</label>
                <input
                  className="form-input"
                  type="password"
                  value={senhaForm.confirmar}
                  onChange={(e) => updateSenha("confirmar", e.target.value)}
                />
              </div>
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
              <button type="submit" className="btn btn-primary btn-sm">
                Alterar senha
              </button>
              {senhaSaved && (
                <span style={{ fontSize: 13, color: "var(--success)" }}>Senha alterada.</span>
              )}
            </div>
          </form>
        </div>

        <hr className="settings-divider" />

        <div className="settings-section">
          <h3>Sessão</h3>
          <p>Encerrar sua sessão atual.</p>
          <button className="btn btn-danger btn-sm" onClick={handleLogout}>
            Sair da conta
          </button>
        </div>
      </div>
    </div>
  );
}
