"use client";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import PasswordInput from "../../components/PasswordInput";
import { apiRequest, clearSession, getSession, saveSession } from "../../lib/apiClient";
import { useApiResource } from "../../lib/useApiResource";

const emptyProfile = {
  name: "",
  email: "",
};

export default function ConfiguracoesPage() {
  const router = useRouter();
  const { data: profile, usingFallback, reload } = useApiResource("/me", emptyProfile);
  const [form, setForm] = useState({
    nome: "",
    email: "",
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
  const [profileFieldErrors, setProfileFieldErrors] = useState({});
  const [senhaFieldErrors, setSenhaFieldErrors] = useState({});

  useEffect(() => {
    const sessionUser = getSession()?.user;
    setForm({
      nome: profile?.name || sessionUser?.name || "",
      email: profile?.email || sessionUser?.email || "",
    });
  }, [profile]);

  function updateForm(field, value) {
    setForm((prev) => ({ ...prev, [field]: value }));
    setProfileFieldErrors((prev) => ({ ...prev, [field]: "" }));
  }

  function updateSenha(field, value) {
    setSenhaForm((prev) => ({ ...prev, [field]: value }));
    setSenhaFieldErrors((prev) => ({ ...prev, [field]: "" }));
  }

  async function handleSaveProfile(e) {
    e.preventDefault();
    setError("");
    const nextFieldErrors = {};

    if (!form.nome.trim()) {
      nextFieldErrors.nome = "Nome completo é obrigatório.";
    }

    if (!form.email.trim()) {
      nextFieldErrors.email = "E-mail é obrigatório.";
    }

    if (Object.keys(nextFieldErrors).length > 0) {
      setProfileFieldErrors(nextFieldErrors);
      setError("Revise os campos destacados.");
      return;
    }

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
    const nextFieldErrors = {};

    if (!senhaForm.atual) {
      nextFieldErrors.atual = "Senha atual é obrigatória.";
    }

    if (!senhaForm.nova) {
      nextFieldErrors.nova = "Nova senha é obrigatória.";
    } else if (senhaForm.nova.length < 6) {
      nextFieldErrors.nova = "Nova senha deve ter no mínimo 6 caracteres.";
    }

    if (!senhaForm.confirmar) {
      nextFieldErrors.confirmar = "Confirmar nova senha é obrigatório.";
    } else if (senhaForm.nova && senhaForm.nova !== senhaForm.confirmar) {
      nextFieldErrors.confirmar = "Confirmação de senha não confere.";
    }

    if (Object.keys(nextFieldErrors).length > 0) {
      setSenhaFieldErrors(nextFieldErrors);
      setSenhaError("Revise os campos destacados.");
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
                  className={`form-input ${profileFieldErrors.nome ? "is-invalid" : ""}`}
                  type="text"
                  value={form.nome}
                  onChange={(e) => updateForm("nome", e.target.value)}
                  aria-invalid={Boolean(profileFieldErrors.nome)}
                />
                {profileFieldErrors.nome && <p className="field-error">{profileFieldErrors.nome}</p>}
              </div>
              <div className="form-group">
                <label>E-mail</label>
                <input
                  className={`form-input ${profileFieldErrors.email ? "is-invalid" : ""}`}
                  type="email"
                  value={form.email}
                  onChange={(e) => updateForm("email", e.target.value)}
                  aria-invalid={Boolean(profileFieldErrors.email)}
                />
                {profileFieldErrors.email && <p className="field-error">{profileFieldErrors.email}</p>}
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
              <PasswordInput
                inputClassName={`form-input ${senhaFieldErrors.atual ? "is-invalid" : ""}`}
                value={senhaForm.atual}
                onChange={(e) => updateSenha("atual", e.target.value)}
                aria-invalid={Boolean(senhaFieldErrors.atual)}
              />
              {senhaFieldErrors.atual && <p className="field-error">{senhaFieldErrors.atual}</p>}
            </div>
            <div className="settings-row">
              <div className="form-group">
                <label>Nova senha</label>
                <PasswordInput
                  inputClassName={`form-input ${senhaFieldErrors.nova ? "is-invalid" : ""}`}
                  value={senhaForm.nova}
                  onChange={(e) => updateSenha("nova", e.target.value)}
                  aria-invalid={Boolean(senhaFieldErrors.nova)}
                />
                {senhaFieldErrors.nova && <p className="field-error">{senhaFieldErrors.nova}</p>}
              </div>
              <div className="form-group">
                <label>Confirmar nova senha</label>
                <PasswordInput
                  inputClassName={`form-input ${senhaFieldErrors.confirmar ? "is-invalid" : ""}`}
                  value={senhaForm.confirmar}
                  onChange={(e) => updateSenha("confirmar", e.target.value)}
                  aria-invalid={Boolean(senhaFieldErrors.confirmar)}
                />
                {senhaFieldErrors.confirmar && <p className="field-error">{senhaFieldErrors.confirmar}</p>}
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
