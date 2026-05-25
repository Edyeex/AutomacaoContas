"use client";
import { useEffect, useState } from "react";
import { contas, operadoras } from "../../lib/mockData";
import { apiRequest } from "../../lib/apiClient";
import { useApiResource } from "../../lib/useApiResource";
import { publishDashboardCountsChanged } from "../../lib/dashboardState";
import { fetchUnreadCount, publishUnreadCount } from "../../lib/notificationState";

function formatDate(d) {
  return d ? new Date(d).toLocaleDateString("pt-BR") : "-";
}

export default function ContasPage() {
  const { data: apiContas, loading, error, usingFallback, reload } = useApiResource("/accounts", contas);
  const { data: apiOperadoras } = useApiResource("/operators", operadoras);
  const [list, setList] = useState(contas);
  const [modal, setModal] = useState(null);
  const [editItem, setEditItem] = useState(null);
  const [form, setForm] = useState({ operadoraId: "", login: "", senha: "", unidade: "" });
  const [formError, setFormError] = useState("");
  const [pageMessage, setPageMessage] = useState("");
  const accountCount = list.length;

  useEffect(() => {
    setList(apiContas || []);
  }, [apiContas]);

  function openAdd() {
    if (accountCount >= 3) return;
    setForm({ operadoraId: "", login: "", senha: "", unidade: "" });
    setFormError("");
    setEditItem(null);
    setModal("add");
  }

  function openEdit(conta) {
    setForm({
      operadoraId: String(conta.operadoraId),
      login: conta.loginPortal,
      senha: "",
      unidade: conta.unidadeConsumidora,
    });
    setFormError("");
    setEditItem(conta);
    setModal("edit");
  }

  function closeModal() {
    setModal(null);
    setEditItem(null);
    setFormError("");
  }

  function update(field, value) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSave(e) {
    e.preventDefault();
    setFormError("");
    const op = apiOperadoras.find((o) => String(o.id) === form.operadoraId);
    if (!op || !form.login || !form.unidade || (modal === "add" && !form.senha)) {
      setFormError("Preencha os campos obrigatórios.");
      return;
    }

    if (usingFallback) {
      saveLocally(op);
      closeModal();
      return;
    }

    try {
      const body = {
        operadoraId: op.id,
        loginPortal: form.login,
        senhaPortal: form.senha || null,
        unidadeConsumidora: form.unidade,
      };

      if (modal === "add") {
        await apiRequest("/accounts", { method: "POST", body });
      } else {
        await apiRequest(`/accounts/${editItem.id}`, { method: "PUT", body });
      }

      await reload();
      publishDashboardCountsChanged();
      await refreshUnreadCount();
      closeModal();
    } catch (err) {
      setFormError(err.message || "Não foi possível salvar a conta.");
    }
  }

  function saveLocally(op) {
    if (modal === "add") {
      const newConta = {
        id: Date.now(),
        operadoraId: op.id,
        operadora: op.name,
        tipo: op.type,
        icon: op.icon,
        loginPortal: form.login,
        unidadeConsumidora: form.unidade,
        status: "ativa",
        ultimaExecucao: null,
        proximaExecucao: null,
      };
      setList((prev) => [...prev, newConta]);
    } else {
      setList((prev) =>
        prev.map((c) =>
          c.id === editItem.id
            ? { ...c, operadoraId: op.id, operadora: op.name, tipo: op.type, icon: op.icon, loginPortal: form.login, unidadeConsumidora: form.unidade }
            : c
        )
      );
    }
  }

  async function handleRemove(id) {
    setPageMessage("");
    if (usingFallback) {
      setList((prev) => prev.filter((c) => c.id !== id));
      return;
    }

    try {
      await apiRequest(`/accounts/${id}`, { method: "DELETE" });
      await reload();
      publishDashboardCountsChanged();
      await refreshUnreadCount();
    } catch (err) {
      setPageMessage(err.message || "Não foi possível remover a conta.");
    }
  }

  async function handleRun(id) {
    setPageMessage("");
    if (usingFallback) {
      setPageMessage("API indisponível; execução manual disponível quando o backend estiver rodando.");
      return;
    }

    try {
      await apiRequest(`/accounts/${id}/run`, { method: "POST" });
      await reload();
      publishDashboardCountsChanged();
      await refreshUnreadCount();
      setPageMessage("Automação executada.");
    } catch (err) {
      setPageMessage(err.message || "Não foi possível executar a automação.");
    }
  }

  async function refreshUnreadCount() {
    try {
      publishUnreadCount(await fetchUnreadCount());
    } catch {
    }
  }

  return (
    <>
      <div className="page-header">
        <h1>Contas cadastradas</h1>
        <div className="page-header-actions">
          <span style={{ fontSize: 13, color: "var(--text-muted)", marginRight: 8 }}>
            {accountCount} {accountCount === 1 ? "conta cadastrada" : "contas cadastradas"}
          </span>
          <button className="btn btn-primary btn-sm" onClick={openAdd} disabled={accountCount >= 3}>
            Adicionar conta
          </button>
        </div>
      </div>

      <div className="page-body">
        {(loading || error || pageMessage) && (
          <p style={{ fontSize: 13, color: error || pageMessage ? "var(--warning)" : "var(--text-muted)", marginBottom: 12 }}>
            {loading ? "Carregando contas..." : pageMessage || "API indisponível; exibindo dados do protótipo."}
          </p>
        )}

        {list.length === 0 ? (
          <div className="empty-state">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
              <circle cx="12" cy="7" r="4" />
            </svg>
            <p>Nenhuma conta cadastrada ainda.</p>
            <button className="btn btn-primary btn-sm" onClick={openAdd}>
              Adicionar primeira conta
            </button>
          </div>
        ) : (
          <div className="accounts-grid">
            {list.map((conta) => (
              <div key={conta.id} className="account-card">
                <div className="account-card-header">
                  <div className="account-card-title">
                    <div className="account-icon">{conta.icon}</div>
                    <div>
                      <h4>{conta.operadora}</h4>
                      <span>{conta.tipo}</span>
                    </div>
                  </div>
                  <div className="account-card-actions">
                    <button className="btn btn-secondary btn-sm" onClick={() => handleRun(conta.id)}>
                      Executar
                    </button>
                    <button className="btn btn-secondary btn-sm" onClick={() => openEdit(conta)}>
                      Editar
                    </button>
                    <button className="btn btn-danger btn-sm" onClick={() => handleRemove(conta.id)}>
                      Remover
                    </button>
                  </div>
                </div>
                <div className="account-detail">
                  <span className="account-detail-label">Login no portal</span>
                  <span className="account-detail-value">{conta.loginPortal}</span>
                </div>
                <div className="account-detail">
                  <span className="account-detail-label">Identificador</span>
                  <span className="account-detail-value">{conta.unidadeConsumidora}</span>
                </div>
                <div className="account-detail">
                  <span className="account-detail-label">Status</span>
                  <span className="account-detail-value">
                    <span className="status status-success">Ativa</span>
                  </span>
                </div>
                <div className="account-detail">
                  <span className="account-detail-label">Última execução</span>
                  <span className="account-detail-value">{formatDate(conta.ultimaExecucao)}</span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {modal && (
        <div className="modal-overlay" onClick={closeModal}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{modal === "add" ? "Adicionar conta" : "Editar conta"}</h2>
              <button className="modal-close" onClick={closeModal}>
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <line x1="18" y1="6" x2="6" y2="18" />
                  <line x1="6" y1="6" x2="18" y2="18" />
                </svg>
              </button>
            </div>
            <form onSubmit={handleSave}>
              <div className="modal-body">
                {formError && (
                  <p style={{ color: "var(--danger)", fontSize: 13, marginBottom: 12 }}>{formError}</p>
                )}
                <div className="form-group">
                  <label>Operadora</label>
                  <select
                    className="form-select"
                    value={form.operadoraId}
                    onChange={(e) => update("operadoraId", e.target.value)}
                  >
                    <option value="">Selecione...</option>
                    {apiOperadoras.map((op) => (
                      <option key={op.id} value={op.id}>
                        {op.icon} {op.name} ({op.type})
                      </option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label>Login no portal</label>
                  <input
                    className="form-input"
                    type="text"
                    value={form.login}
                    onChange={(e) => update("login", e.target.value)}
                    placeholder="Usuário ou e-mail do portal"
                  />
                </div>
                <div className="form-group">
                  <label>Senha do portal</label>
                  <input
                    className="form-input"
                    type="password"
                    value={form.senha}
                    onChange={(e) => update("senha", e.target.value)}
                    placeholder={modal === "edit" ? "Deixe em branco para manter" : "Senha de acesso"}
                  />
                  <p className="form-hint">Armazenada com criptografia. Nunca será exibida.</p>
                </div>
                <div className="form-group">
                  <label>Identificador (UC, matrícula, contrato...)</label>
                  <input
                    className="form-input"
                    type="text"
                    value={form.unidade}
                    onChange={(e) => update("unidade", e.target.value)}
                    placeholder="Ex: UC-123456"
                  />
                </div>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={closeModal}>
                  Cancelar
                </button>
                <button type="submit" className="btn btn-primary">
                  {modal === "add" ? "Adicionar" : "Salvar"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </>
  );
}
