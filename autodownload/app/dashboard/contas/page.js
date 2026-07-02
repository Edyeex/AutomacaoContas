"use client";
import { useEffect, useRef, useState } from "react";
import { apiRequest } from "../../lib/apiClient";
import { useApiResource } from "../../lib/useApiResource";
import { publishDashboardCountsChanged } from "../../lib/dashboardState";
import { fetchUnreadCount, publishUnreadCount } from "../../lib/notificationState";
import OperatorLogo from "../../components/OperatorLogo";
import PasswordInput from "../../components/PasswordInput";

const AUTOMATION_RUN_TIMEOUT_MS = 210000;
const AUTOMATION_SLOW_NOTICE_SECONDS = 45;
const SAVED_PASSWORD_MASK = "********";
const emptyAccountForm = { operadoraId: "", login: "", senha: "", unidade: "" };

function formatDate(d) {
  return d ? new Date(d).toLocaleDateString("pt-BR") : "-";
}

function formatDateTime(d) {
  if (!d) return "-";
  return new Date(d).toLocaleString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export default function ContasPage() {
  const { data: apiContas, loading, error, usingFallback, reload } = useApiResource("/accounts", []);
  const { data: apiOperadoras } = useApiResource("/operators", []);
  const [list, setList] = useState([]);
  const [modal, setModal] = useState(null);
  const [editItem, setEditItem] = useState(null);
  const [scheduleItem, setScheduleItem] = useState(null);
  const [confirmation, setConfirmation] = useState(null);
  const [confirmationBusy, setConfirmationBusy] = useState(false);
  const [form, setForm] = useState(emptyAccountForm);
  const [formNonce, setFormNonce] = useState(() => Date.now());
  const [scheduleForm, setScheduleForm] = useState({ enabled: true, mode: "day", day: "1", time: "09:00" });
  const [formError, setFormError] = useState("");
  const [fieldErrors, setFieldErrors] = useState({});
  const [scheduleFieldErrors, setScheduleFieldErrors] = useState({});
  const [pageMessage, setPageMessage] = useState("");
  const [pageMessageType, setPageMessageType] = useState("info");
  const [operatorPickerOpen, setOperatorPickerOpen] = useState(false);
  const [runningAccountId, setRunningAccountId] = useState(null);
  const [runningSeconds, setRunningSeconds] = useState(0);
  const accountFormRef = useRef(null);
  const accountCount = list.length;
  const selectedOperator = apiOperadoras.find((op) => String(op.id) === form.operadoraId);

  useEffect(() => {
    setList(apiContas || []);
  }, [apiContas]);

  useEffect(() => {
    if (!runningAccountId) {
      return undefined;
    }

    setRunningSeconds(0);
    const timer = window.setInterval(() => {
      setRunningSeconds((current) => current + 1);
    }, 1000);

    return () => window.clearInterval(timer);
  }, [runningAccountId]);

  useEffect(() => {
    if (modal !== "add") return undefined;

    function clearAddForm() {
      accountFormRef.current?.reset();
      setForm({ ...emptyAccountForm });
      setFieldErrors({});
      setFormError("");
    }

    clearAddForm();
    const autofillTimer = window.setTimeout(clearAddForm, 120);
    return () => window.clearTimeout(autofillTimer);
  }, [modal, formNonce]);

  function openAdd() {
    if (accountCount >= 3) return;
    clearPageMessage();
    setForm({ ...emptyAccountForm });
    setFormNonce(Date.now());
    setFormError("");
    setFieldErrors({});
    setOperatorPickerOpen(false);
    setEditItem(null);
    setModal("add");
  }

  function openEdit(conta) {
    clearPageMessage();
    setForm({
      operadoraId: String(conta.operadoraId),
      login: conta.loginPortal,
      senha: SAVED_PASSWORD_MASK,
      unidade: conta.unidadeConsumidora,
    });
    setFormError("");
    setFieldErrors({});
    setOperatorPickerOpen(false);
    setEditItem(conta);
    setModal("edit");
  }

  function openSchedule(conta) {
    clearPageMessage();
    setScheduleItem(conta);
    setScheduleForm({
      enabled: conta.agendamentoAtivo ?? true,
      mode: conta.ultimoDiaDoMes ? "last" : "day",
      day: String(conta.diaAgendamento || 1),
      time: String(conta.horarioAgendamento || "09:00").slice(0, 5),
    });
    setFormError("");
    setScheduleFieldErrors({});
  }

  function closeModal() {
    setModal(null);
    setEditItem(null);
    setFormError("");
    setFieldErrors({});
    setOperatorPickerOpen(false);
  }

  function closeSchedule() {
    setScheduleItem(null);
    setFormError("");
    setScheduleFieldErrors({});
  }

  function closeConfirmation() {
    if (confirmationBusy) return;
    setConfirmation(null);
  }

  function update(field, value) {
    setForm((prev) => ({ ...prev, [field]: value }));
    setFieldErrors((prev) => ({ ...prev, [field]: "" }));
  }

  async function revealPortalPassword() {
    if (modal !== "edit" || !editItem || form.senha !== SAVED_PASSWORD_MASK) {
      return;
    }

    if (usingFallback) {
      setFormError("A senha real só pode ser exibida quando o backend estiver conectado.");
      throw new Error("Backend indisponível.");
    }

    try {
      setFormError("");
      const response = await apiRequest(`/accounts/${editItem.id}/portal-password`);
      update("senha", response?.senhaPortal || "");
    } catch (err) {
      setFormError(err.message || "Não foi possível exibir a senha cadastrada.");
      throw err;
    }
  }

  function selectOperator(op) {
    update("operadoraId", String(op.id));
    setOperatorPickerOpen(false);
  }

  function updateSchedule(field, value) {
    setScheduleForm((prev) => ({ ...prev, [field]: value, enabled: true }));
    setScheduleFieldErrors((prev) => ({ ...prev, [field]: "" }));
  }

  function showPageMessage(message, type = "warning") {
    setPageMessage(message);
    setPageMessageType(type);
  }

  function clearPageMessage() {
    setPageMessage("");
    setPageMessageType("info");
  }

  function validateAccountForm() {
    setFormError("");
    const op = apiOperadoras.find((o) => String(o.id) === form.operadoraId);
    const nextFieldErrors = {};

    if (!op) {
      nextFieldErrors.operadoraId = "Operadora é obrigatória.";
    }

    if (!form.login.trim()) {
      nextFieldErrors.login = "Login no portal é obrigatório.";
    }

    if (modal === "add" && !form.senha) {
      nextFieldErrors.senha = "Senha do portal é obrigatória.";
    }

    if (!form.unidade.trim()) {
      nextFieldErrors.unidade = "Documento/contrato da conta é obrigatório.";
    }

    if (Object.keys(nextFieldErrors).length > 0) {
      setFieldErrors(nextFieldErrors);
      setFormError("Revise os campos obrigatórios destacados.");
      return null;
    }

    return op;
  }

  async function handleSave(e) {
    e.preventDefault();
    const op = validateAccountForm();
    if (!op) return;

    if (modal === "edit") {
      setConfirmation({ type: "edit", conta: editItem });
      return;
    }

    await persistAccount(op);
  }

  async function persistAccount(op) {
    const isAdding = modal === "add";

    if (usingFallback) {
      saveLocally(op);
      closeModal();
      showPageMessage(
        isAdding ? `Conta ${op.name} adicionada com sucesso.` : `Conta ${op.name} editada com sucesso.`,
        "success"
      );
      return;
    }

    try {
      const body = {
        operadoraId: op.id,
        loginPortal: form.login,
        senhaPortal: modal === "edit" && form.senha === SAVED_PASSWORD_MASK ? null : form.senha || null,
        unidadeConsumidora: form.unidade,
      };

      if (isAdding) {
        await apiRequest("/accounts", { method: "POST", body });
      } else {
        await apiRequest(`/accounts/${editItem.id}`, { method: "PUT", body });
      }

      await reload();
      publishDashboardCountsChanged();
      await refreshUnreadCount();
      closeModal();
      showPageMessage(
        isAdding ? `Conta ${op.name} adicionada com sucesso.` : `Conta ${op.name} editada com sucesso.`,
        "success"
      );
    } catch (err) {
      setFormError(err.message || "Não foi possível salvar a conta. Confira os dados preenchidos e tente novamente.");
    }
  }

  async function handleScheduleSave(e) {
    e.preventDefault();
    setFormError("");
    const day = Number(scheduleForm.day);
    const nextFieldErrors = {};

    if (scheduleForm.enabled && scheduleForm.mode === "day" && (!Number.isInteger(day) || day < 1 || day > 31)) {
      nextFieldErrors.day = "Dia do mês deve ser entre 1 e 31.";
    }

    if (scheduleForm.enabled && !scheduleForm.time) {
      nextFieldErrors.time = "Horário é obrigatório.";
    }

    if (Object.keys(nextFieldErrors).length > 0) {
      setScheduleFieldErrors(nextFieldErrors);
      setFormError("Revise os campos destacados.");
      return;
    }

    const body = {
      enabled: scheduleForm.enabled,
      dayOfMonth: scheduleForm.enabled && scheduleForm.mode === "day" ? day : null,
      lastDayOfMonth: scheduleForm.enabled && scheduleForm.mode === "last",
      time: `${scheduleForm.time || "09:00"}:00`,
    };

    if (usingFallback) {
      setList((prev) => prev.map((conta) => conta.id === scheduleItem.id
        ? {
            ...conta,
            agendamentoAtivo: body.enabled,
            diaAgendamento: body.dayOfMonth,
            ultimoDiaDoMes: body.lastDayOfMonth,
            horarioAgendamento: body.time,
          }
        : conta));
      closeSchedule();
      return;
    }

    try {
      await apiRequest(`/accounts/${scheduleItem.id}/schedule`, { method: "PUT", body });
      await reload();
      publishDashboardCountsChanged();
      await refreshUnreadCount();
      showPageMessage(body.enabled ? "Agendamento mensal ativado." : "Agendamento mensal desativado.", "success");
      closeSchedule();
    } catch (err) {
      setFormError(err.message || "Não foi possível salvar o agendamento.");
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
    clearPageMessage();
    if (usingFallback) {
      setList((prev) => prev.filter((c) => c.id !== id));
      showPageMessage("Conta removida com sucesso.", "success");
      return;
    }

    try {
      await apiRequest(`/accounts/${id}`, { method: "DELETE" });
      await reload();
      publishDashboardCountsChanged();
      await refreshUnreadCount();
      showPageMessage("Conta removida com sucesso.", "success");
    } catch (err) {
      showPageMessage(err.message || "Não foi possível remover a conta.");
    }
  }

  function requestRemove(conta) {
    if (runningAccountId) return;
    clearPageMessage();
    setConfirmation({ type: "remove", conta });
  }

  async function confirmAccountOperation() {
    if (!confirmation) return;

    setConfirmationBusy(true);
    try {
      if (confirmation.type === "edit") {
        const op = validateAccountForm();
        if (op) {
          await persistAccount(op);
        }
      }

      if (confirmation.type === "remove") {
        await handleRemove(confirmation.conta.id);
      }
    } finally {
      setConfirmationBusy(false);
      setConfirmation(null);
    }
  }

  async function handleRun(id) {
    if (runningAccountId) {
      return;
    }

    clearPageMessage();
    if (usingFallback) {
      showPageMessage("API indisponível; execução manual disponível quando o backend estiver rodando.");
      return;
    }

    try {
      setRunningAccountId(id);
      const run = await apiRequest(`/accounts/${id}/run`, {
        method: "POST",
        timeoutMs: AUTOMATION_RUN_TIMEOUT_MS,
      });

      showPageMessage(buildRunMessage(run), run?.status === "sucesso" ? "success" : "warning");
    } catch (err) {
      const timeoutMessage = err?.code === "request.timeout"
        ? "A automação demorou mais do que o esperado. O servidor pode continuar finalizando a tentativa; confira Histórico e Notificações em instantes."
        : err?.message || "Não foi possível executar a automação.";

      showPageMessage(timeoutMessage);
    } finally {
      await refreshAfterRun();
      setRunningAccountId(null);
    }
  }

  function buildRunMessage(run) {
    if (run?.status === "sucesso") {
      return "Automação concluída e boleto registrado. Confira Boletos, Histórico e Notificações.";
    }

    if (run?.status === "indisponivel") {
      return run.mensagem || "A automação terminou, mas não encontrou boleto disponível para pagamento.";
    }

    return run?.mensagem || "A automação terminou com falha. Confira Notificações para mais detalhes.";
  }

  function formatElapsed(seconds) {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;

    return `${String(minutes).padStart(2, "0")}:${String(remainingSeconds).padStart(2, "0")}`;
  }

  async function refreshUnreadCount() {
    try {
      publishUnreadCount(await fetchUnreadCount());
    } catch {
    }
  }

  async function refreshAfterRun() {
    try {
      await reload();
      publishDashboardCountsChanged();
      await refreshUnreadCount();
    } catch {
    }
  }

  function runningHint() {
    if (runningSeconds >= AUTOMATION_SLOW_NOTICE_SECONDS) {
      return `Automação em andamento, aguarde por favor. Tempo decorrido: ${formatElapsed(runningSeconds)}`;
    }

    return `Aguarde por favor. Tempo decorrido: ${formatElapsed(runningSeconds)}`;
  }

  function pageMessageColor() {
    if (error) return "var(--warning)";
    if (pageMessageType === "success") return "var(--success)";
    if (pageMessageType === "danger") return "var(--danger)";
    if (pageMessageType === "info") return "var(--text-muted)";

    return "var(--warning)";
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
          <p style={{ fontSize: 13, color: pageMessageColor(), marginBottom: 12 }}>
            {loading ? "Carregando contas..." : pageMessage || error || "Não foi possível carregar as contas agora."}
          </p>
        )}

        {runningAccountId && (
          <div className="automation-run-alert" role="status" aria-live="polite">
            <span className="automation-spinner" aria-hidden="true" />
            <div>
              <strong>Automação em andamento</strong>
              <span>{runningHint()}</span>
            </div>
          </div>
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
              <div key={conta.id} className={`account-card ${runningAccountId === conta.id ? "is-running" : ""}`}>
                <div className="account-card-header">
                  <div className="account-card-title">
                    <OperatorLogo operator={conta} icon={conta.icon} className="account-icon" />
                    <div>
                      <h4>{conta.operadora}</h4>
                      <span>{conta.tipo}</span>
                    </div>
                  </div>
                  <div className="account-card-actions">
                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={() => handleRun(conta.id)}
                      disabled={Boolean(runningAccountId)}
                    >
                      {runningAccountId === conta.id ? "Executando..." : "Executar"}
                    </button>
                    <button className="btn btn-secondary btn-sm" onClick={() => openEdit(conta)} disabled={Boolean(runningAccountId)}>
                      Editar
                    </button>
                    <button className="btn btn-secondary btn-sm" onClick={() => openSchedule(conta)} disabled={Boolean(runningAccountId)}>
                      Agendar
                    </button>
                    <button className="btn btn-danger btn-sm" onClick={() => requestRemove(conta)} disabled={Boolean(runningAccountId)}>
                      Remover
                    </button>
                  </div>
                </div>
                {runningAccountId === conta.id && (
                  <div className="account-running-state">
                    <span className="automation-spinner automation-spinner-sm" aria-hidden="true" />
                    <span>Automação em andamento</span>
                  </div>
                )}
                <div className="account-detail">
                  <span className="account-detail-label">Login no portal</span>
                  <span className="account-detail-value">{conta.loginPortal}</span>
                </div>
                <div className="account-detail">
                  <span className="account-detail-label">Documento/contrato</span>
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
                <div className="account-detail">
                  <span className="account-detail-label">Agendamento</span>
                  <span className="account-detail-value">
                    <span className={`status ${conta.agendamentoAtivo ? "status-info" : "status-neutral"}`}>
                      {conta.agendamentoAtivo ? "Ativo" : "Inativo"}
                    </span>
                  </span>
                </div>
                <div className="account-detail">
                  <span className="account-detail-label">Próxima execução</span>
                  <span className="account-detail-value">
                    {formatDateTime(conta.agendamentoAtivo ? conta.proximaExecucao : null)}
                  </span>
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
            <form ref={accountFormRef} onSubmit={handleSave} autoComplete="off">
              <div className="modal-body">
                {formError && (
                  <p style={{ color: "var(--danger)", fontSize: 13, marginBottom: 12 }}>{formError}</p>
                )}
                <div className="form-group">
                  <label>Operadora</label>
                  <div className="operator-picker">
                    <button
                      type="button"
                      className={`operator-picker-trigger ${fieldErrors.operadoraId ? "is-invalid" : ""}`}
                      onClick={() => setOperatorPickerOpen((current) => !current)}
                      aria-expanded={operatorPickerOpen}
                      aria-invalid={Boolean(fieldErrors.operadoraId)}
                    >
                      <span className="operator-picker-value">
                        {selectedOperator ? (
                          <>
                            <OperatorLogo operator={selectedOperator} icon={selectedOperator.icon} size="sm" />
                            <span>{selectedOperator.name} ({selectedOperator.type})</span>
                          </>
                        ) : (
                          <span>Selecione...</span>
                        )}
                      </span>
                      <span className="operator-picker-chevron" aria-hidden="true">⌄</span>
                    </button>

                    {operatorPickerOpen && (
                      <div className="operator-picker-list" role="listbox">
                        {apiOperadoras.map((op) => (
                          <button
                            key={op.id}
                            type="button"
                            className={`operator-picker-option ${String(op.id) === form.operadoraId ? "is-selected" : ""}`}
                            onClick={() => selectOperator(op)}
                            role="option"
                            aria-selected={String(op.id) === form.operadoraId}
                          >
                            <span className="operator-picker-option-content">
                              <OperatorLogo operator={op} icon={op.icon} size="sm" />
                              <span>{op.name}</span>
                            </span>
                            <span className="operator-picker-type">{op.type}</span>
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                  {selectedOperator && (
                    <div className="operator-picker-preview">
                      <OperatorLogo operator={selectedOperator} icon={selectedOperator.icon} size="sm" />
                      <span>Operadora selecionada: {selectedOperator.name}</span>
                    </div>
                  )}
                  {fieldErrors.operadoraId && <p className="field-error">{fieldErrors.operadoraId}</p>}
                </div>
                <div className="form-group">
                  <label>Login no portal</label>
                  <input
                    key={`account-login-${modal}-${formNonce}`}
                    name={`account-portal-login-${formNonce}`}
                    className={`form-input ${fieldErrors.login ? "is-invalid" : ""}`}
                    type="text"
                    value={form.login}
                    onChange={(e) => update("login", e.target.value)}
                    autoComplete="off"
                    autoCorrect="off"
                    autoCapitalize="none"
                    spellCheck="false"
                    aria-invalid={Boolean(fieldErrors.login)}
                  />
                  {fieldErrors.login && <p className="field-error">{fieldErrors.login}</p>}
                </div>
                <div className="form-group">
                  <label>Senha do portal</label>
                  <PasswordInput
                    key={`account-password-${modal}-${formNonce}`}
                    name={`account-portal-password-${formNonce}`}
                    inputClassName={`form-input ${fieldErrors.senha ? "is-invalid" : ""}`}
                    value={form.senha}
                    onChange={(e) => update("senha", e.target.value)}
                    onFocus={() => {
                      if (modal === "edit" && form.senha === SAVED_PASSWORD_MASK) {
                        update("senha", "");
                      }
                    }}
                    onReveal={modal === "edit" ? revealPortalPassword : undefined}
                    placeholder={modal === "edit" ? "Informe uma nova senha para alterar" : ""}
                    autoComplete="new-password"
                    aria-invalid={Boolean(fieldErrors.senha)}
                  />
                  {fieldErrors.senha && <p className="field-error">{fieldErrors.senha}</p>}
                  <p className="form-hint">
                    {modal === "edit"
                      ? "Senha cadastrada. Clique no olho para visualizar ou no campo para trocar."
                      : "Armazenada com criptografia. Nunca será exibida."}
                  </p>
                </div>
                <div className="form-group">
                  <label>Documento/contrato da conta</label>
                  <input
                    key={`account-identifier-${modal}-${formNonce}`}
                    name={`account-identifier-${formNonce}`}
                    className={`form-input ${fieldErrors.unidade ? "is-invalid" : ""}`}
                    type="text"
                    value={form.unidade}
                    onChange={(e) => update("unidade", e.target.value)}
                    autoComplete="off"
                    autoCorrect="off"
                    autoCapitalize="none"
                    spellCheck="false"
                    aria-invalid={Boolean(fieldErrors.unidade)}
                  />
                  {fieldErrors.unidade && <p className="field-error">{fieldErrors.unidade}</p>}
                  <p className="form-hint">Usado pela automação quando o portal pede um documento ou código da conta.</p>
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

      {scheduleItem && (
        <div className="modal-overlay" onClick={closeSchedule}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h2>Agendamento mensal</h2>
                <p className="modal-subtitle">{scheduleItem.operadora}</p>
              </div>
              <button className="modal-close" onClick={closeSchedule} aria-label="Fechar">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <line x1="18" y1="6" x2="6" y2="18" />
                  <line x1="6" y1="6" x2="18" y2="18" />
                </svg>
              </button>
            </div>
            <form onSubmit={handleScheduleSave}>
              <div className="modal-body">
                {formError && <p className="form-error">{formError}</p>}
                <label className="schedule-toggle">
                  <input
                    type="checkbox"
                    checked={scheduleForm.enabled}
                    onChange={(e) => setScheduleForm((prev) => ({ ...prev, enabled: e.target.checked }))}
                  />
                  <span>
                    <strong>Executar automaticamente</strong>
                    <small>Continua ativo todos os meses até que você desative.</small>
                  </span>
                </label>

                <div className="form-group">
                  <label>Quando executar</label>
                  <select
                    className="form-select"
                    value={scheduleForm.mode}
                    onChange={(e) => updateSchedule("mode", e.target.value)}
                  >
                    <option value="day">Em um dia do mês</option>
                    <option value="last">No último dia do mês</option>
                  </select>
                </div>

                {scheduleForm.mode === "day" && (
                  <div className="form-group">
                    <label>Dia do mês</label>
                    <input
                      className={`form-input ${scheduleFieldErrors.day ? "is-invalid" : ""}`}
                      type="number"
                      min="1"
                      max="31"
                      value={scheduleForm.day}
                      onChange={(e) => updateSchedule("day", e.target.value)}
                      aria-invalid={Boolean(scheduleFieldErrors.day)}
                    />
                    {scheduleFieldErrors.day && <p className="field-error">{scheduleFieldErrors.day}</p>}
                    <p className="form-hint">Em meses mais curtos, será usado o último dia disponível.</p>
                  </div>
                )}

                <div className="form-group">
                  <label>Horário</label>
                  <input
                    className={`form-input ${scheduleFieldErrors.time ? "is-invalid" : ""}`}
                    type="time"
                    value={scheduleForm.time}
                    onChange={(e) => updateSchedule("time", e.target.value)}
                    aria-invalid={Boolean(scheduleFieldErrors.time)}
                  />
                  {scheduleFieldErrors.time && <p className="field-error">{scheduleFieldErrors.time}</p>}
                </div>

                {scheduleItem.agendamentoAtivo && (
                  <p className="schedule-next">
                    Próxima execução atual: <strong>{formatDateTime(scheduleItem.proximaExecucao)}</strong>
                  </p>
                )}
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={closeSchedule}>Cancelar</button>
                <button type="submit" className="btn btn-primary">Salvar agendamento</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {confirmation && (
        <div className="modal-overlay" onClick={closeConfirmation}>
          <div className="modal modal-confirm" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{confirmation.type === "edit" ? "Salvar alterações?" : "Remover conta?"}</h2>
              <button className="modal-close" onClick={closeConfirmation} disabled={confirmationBusy} aria-label="Fechar">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <line x1="18" y1="6" x2="6" y2="18" />
                  <line x1="6" y1="6" x2="18" y2="18" />
                </svg>
              </button>
            </div>
            <div className="modal-body">
              <p className="confirm-message">
                {confirmation.type === "edit"
                  ? "Deseja realmente salvar as alterações desta conta?"
                  : "Deseja realmente remover esta conta? Essa operação também remove os dados vinculados a ela no sistema."}
              </p>
              <div className="confirm-account-summary">
                <OperatorLogo operator={confirmation.conta} icon={confirmation.conta.icon} size="sm" />
                <div>
                  <strong>{confirmation.conta.operadora}</strong>
                  <span>{confirmation.conta.loginPortal}</span>
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={closeConfirmation} disabled={confirmationBusy}>
                Cancelar
              </button>
              <button
                type="button"
                className={`btn ${confirmation.type === "remove" ? "btn-danger" : "btn-primary"}`}
                onClick={confirmAccountOperation}
                disabled={confirmationBusy}
              >
                {confirmationBusy
                  ? "Processando..."
                  : confirmation.type === "edit"
                    ? "Sim, salvar"
                    : "Sim, remover"}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
