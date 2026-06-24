"use client";
import Link from "next/link";
import { useApiResource } from "../lib/useApiResource";

const fallbackDashboard = {
  totalContas: 0,
  limiteContas: 3,
  totalBoletos: 0,
  execucoesSucesso: 0,
  execucoesFalha: 0,
  proximaExecucao: null,
  boletosRecentes: [],
  historicoRecente: [],
};

function formatDate(dateStr) {
  return dateStr ? new Date(dateStr).toLocaleDateString("pt-BR") : "-";
}

function formatDateTime(dateStr) {
  const d = new Date(dateStr);
  return d.toLocaleDateString("pt-BR") + " " + d.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });
}

function formatCurrency(val) {
  return Number(val || 0).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function statusText(status) {
  if (status === "sucesso") return "Sucesso";
  if (status === "falha") return "Falha";
  return "Indisponível";
}

function statusClass(status) {
  if (status === "sucesso") return "status-success";
  if (status === "falha") return "status-error";
  return "status-warning";
}

export default function DashboardPage() {
  const { data: dashboard, loading, error, usingFallback } = useApiResource("/dashboard", fallbackDashboard);
  const recentBoletos = dashboard.boletosRecentes || [];
  const recentHistory = dashboard.historicoRecente || [];

  return (
    <>
      <div className="page-header">
        <h1>Visão geral</h1>
        <div className="page-header-actions">
          <Link href="/dashboard/contas" className="btn btn-secondary btn-sm">
            Gerenciar contas
          </Link>
        </div>
      </div>

      <div className="page-body">
        {(loading || error) && (
          <p style={{ fontSize: 13, color: usingFallback ? "var(--warning)" : "var(--text-muted)", marginBottom: 12 }}>
            {loading ? "Carregando dados..." : error || "Nao foi possivel carregar os dados agora."}
          </p>
        )}

        <div className="stats-row">
          <div className="stat-item">
            <div className="stat-label">Contas cadastradas</div>
            <div className="stat-value">{dashboard.totalContas}</div>
            <div className="stat-sub">de {dashboard.limiteContas} permitidas</div>
          </div>
          <div className="stat-item">
            <div className="stat-label">Boletos baixados</div>
            <div className="stat-value">{dashboard.totalBoletos}</div>
            <div className="stat-sub">total acumulado</div>
          </div>
          <div className="stat-item">
            <div className="stat-label">Execuções com sucesso</div>
            <div className="stat-value">{dashboard.execucoesSucesso}</div>
            <div className="stat-sub">{dashboard.execucoesFalha} falha(s)</div>
          </div>
          <div className="stat-item">
            <div className="stat-label">Próxima execução</div>
            <div className="stat-value" style={{ fontSize: 18 }}>{formatDate(dashboard.proximaExecucao)}</div>
            <div className="stat-sub">
              {dashboard.proximaExecucao ? "agendamento automático" : "nenhum agendamento ativo"}
            </div>
          </div>
        </div>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
          <div className="card">
            <div className="card-header">
              <h3>Boletos recentes</h3>
              <Link href="/dashboard/boletos" className="btn-link">Ver todos</Link>
            </div>
            <div>
              {recentBoletos.map((b) => (
                <div key={b.id} className="boleto-item">
                  <div className="boleto-info">
                    <div className="boleto-icon">{b.icon}</div>
                    <div className="boleto-details">
                      <h4>{b.operadora}</h4>
                      <span>{b.referencia} - Vence {formatDate(b.vencimento)}</span>
                    </div>
                  </div>
                  <div className="boleto-meta">
                    <span className="boleto-amount">{formatCurrency(b.valor)}</span>
                    <button className="btn btn-secondary btn-sm">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                        <polyline points="7 10 12 15 17 10" />
                        <line x1="12" y1="15" x2="12" y2="3" />
                      </svg>
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="card">
            <div className="card-header">
              <h3>Atividade recente</h3>
              <Link href="/dashboard/historico" className="btn-link">Ver tudo</Link>
            </div>
            <div className="table-container">
              <table>
                <thead>
                  <tr>
                    <th>Operadora</th>
                    <th>Status</th>
                    <th>Data</th>
                  </tr>
                </thead>
                <tbody>
                  {recentHistory.map((h) => (
                    <tr key={h.id}>
                      <td>{h.operadora}</td>
                      <td>
                        <span className={`status ${statusClass(h.status)}`}>
                          {statusText(h.status)}
                        </span>
                      </td>
                      <td style={{ color: "var(--text-muted)", fontSize: 12 }}>{formatDateTime(h.dataExecucao)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
