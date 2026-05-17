"use client";
import { useState } from "react";
import { historico } from "../../lib/mockData";

function formatDateTime(d) {
  const dt = new Date(d);
  return dt.toLocaleDateString("pt-BR") + " " + dt.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });
}

function statusLabel(s) {
  if (s === "sucesso") return "Sucesso";
  if (s === "falha") return "Falha";
  return "Indisponível";
}

function statusClass(s) {
  if (s === "sucesso") return "status-success";
  if (s === "falha") return "status-error";
  return "status-warning";
}

export default function HistoricoPage() {
  const [filter, setFilter] = useState("todos");

  const filtered = historico.filter((h) => {
    if (filter === "todos") return true;
    return h.status === filter;
  });

  return (
    <>
      <div className="page-header">
        <h1>Histórico de execuções</h1>
      </div>

      <div className="page-body">
        <div className="tabs">
          {["todos", "sucesso", "falha", "indisponivel"].map((f) => (
            <button
              key={f}
              className={`tab${filter === f ? " active" : ""}`}
              onClick={() => setFilter(f)}
            >
              {f === "todos" ? "Todos" : f === "sucesso" ? "Sucesso" : f === "falha" ? "Falha" : "Indisponível"}
            </button>
          ))}
        </div>

        <div className="card">
          {filtered.length === 0 ? (
            <div className="empty-state">
              <p>Nenhum registro encontrado para este filtro.</p>
            </div>
          ) : (
            <div className="table-container">
              <table>
                <thead>
                  <tr>
                    <th>Operadora</th>
                    <th>Data da execução</th>
                    <th>Status</th>
                    <th>Duração</th>
                    <th>Mensagem</th>
                    <th>Arquivo</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((h) => (
                    <tr key={h.id}>
                      <td style={{ fontWeight: 500 }}>{h.operadora}</td>
                      <td style={{ color: "var(--text-muted)", fontSize: 12, whiteSpace: "nowrap" }}>
                        {formatDateTime(h.dataExecucao)}
                      </td>
                      <td>
                        <span className={`status ${statusClass(h.status)}`}>
                          {statusLabel(h.status)}
                        </span>
                      </td>
                      <td style={{ color: "var(--text-muted)" }}>{h.duracao}</td>
                      <td style={{ maxWidth: 280, fontSize: 13 }}>{h.mensagem}</td>
                      <td>
                        {h.arquivo ? (
                          <button className="btn btn-secondary btn-sm">
                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                              <polyline points="7 10 12 15 17 10" />
                              <line x1="12" y1="15" x2="12" y2="3" />
                            </svg>
                          </button>
                        ) : (
                          <span style={{ color: "var(--text-muted)", fontSize: 12 }}>—</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </>
  );
}
