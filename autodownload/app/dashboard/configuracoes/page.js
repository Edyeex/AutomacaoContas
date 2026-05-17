"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { currentUser } from "../../lib/mockData";

export default function ConfiguracoesPage() {
  const router = useRouter();
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

  function updateForm(field, value) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  function updateSenha(field, value) {
    setSenhaForm((prev) => ({ ...prev, [field]: value }));
  }

  function handleSaveProfile(e) {
    e.preventDefault();
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  }

  function handleSavePassword(e) {
    e.preventDefault();
    setSenhaSaved(true);
    setSenhaForm({ atual: "", nova: "", confirmar: "" });
    setTimeout(() => setSenhaSaved(false), 2000);
  }

  function handleLogout() {
    router.push("/");
  }

  return (
    <>
      <div className="page-header">
        <h1>Configurações</h1>
      </div>

      <div className="page-body" style={{ maxWidth: 600 }}>
        {/* Profile */}
        <div className="settings-section">
          <h3>Perfil</h3>
          <p>Informações da sua conta.</p>
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

        {/* Password */}
        <div className="settings-section">
          <h3>Alterar senha</h3>
          <p>Use uma senha forte com pelo menos 6 caracteres.</p>
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

        {/* Danger Zone */}
        <div className="settings-section">
          <h3>Sessão</h3>
          <p>Encerrar sua sessão atual.</p>
          <button className="btn btn-danger btn-sm" onClick={handleLogout}>
            Sair da conta
          </button>
        </div>
      </div>
    </>
  );
}
