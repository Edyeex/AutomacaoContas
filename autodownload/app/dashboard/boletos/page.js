"use client";
import { useState } from "react";
import { boletos } from "../../lib/mockData";
import { useApiResource } from "../../lib/useApiResource";
import { apiDownload } from "../../lib/apiClient";

function normalize(text) {
  return String(text || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase();
}

function formatDate(d) {
  return new Date(d).toLocaleDateString("pt-BR");
}

function formatDateTime(d) {
  const dt = new Date(d);
  return dt.toLocaleDateString("pt-BR") + " " + dt.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });
}

function formatCurrency(val) {
  return Number(val || 0).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export default function BoletosPage() {
  const { data, loading, error, usingFallback } = useApiResource("/bills", boletos);
  const [filter, setFilter] = useState("todos");
  const [search, setSearch] = useState("");
  const [pageMessage, setPageMessage] = useState("");

  const filtered = data.filter((b) => {
    if (filter !== "todos" && normalize(b.tipo) !== filter) return false;
    if (search && !normalize(b.operadora).includes(normalize(search)) && !normalize(b.referencia).includes(normalize(search))) return false;
    return true;
  });

  async function downloadBill(boleto) {
    setPageMessage("");

    if (usingFallback) {
      setPageMessage("Download disponível quando a API estiver rodando.");
      return;
    }

    try {
      await apiDownload(`/bills/${boleto.id}/download`, boleto.arquivo);
    } catch (err) {
      setPageMessage(err.message || "Não foi possível baixar o boleto.");
    }
  }

  return (
    <>
      <div className="page-header">
        <h1>Boletos</h1>
      </div>

      <div className="page-body">
        {(loading || error || pageMessage) && (
          <p style={{ fontSize: 13, color: usingFallback ? "var(--warning)" : "var(--text-muted)", marginBottom: 12 }}>
            {loading ? "Carregando boletos..." : pageMessage || "API indisponível; exibindo dados do protótipo."}
          </p>
        )}

        <div className="filter-bar">
          <input
            className="form-input"
            type="text"
            placeholder="Buscar por operadora ou referência..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <select
            className="form-select"
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
          >
            <option value="todos">Todos os tipos</option>
            <option value="energia">Energia</option>
            <option value="agua">Água</option>
            <option value="internet">Internet</option>
          </select>
        </div>

        <div className="card">
          {filtered.length === 0 ? (
            <div className="empty-state">
              <p>Nenhum boleto encontrado.</p>
            </div>
          ) : (
            <div className="table-container">
              <table>
                <thead>
                  <tr>
                    <th>Operadora</th>
                    <th>Referência</th>
                    <th>Vencimento</th>
                    <th>Valor</th>
                    <th>Baixado em</th>
                    <th style={{ textAlign: "right" }}>Ação</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((b) => (
                    <tr key={b.id}>
                      <td>
                        <span style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
                          <span>{b.icon}</span> {b.operadora}
                        </span>
                      </td>
                      <td>{b.referencia}</td>
                      <td>{formatDate(b.vencimento)}</td>
                      <td style={{ fontWeight: 600, color: "var(--text-heading)" }}>
                        {formatCurrency(b.valor)}
                      </td>
                      <td style={{ color: "var(--text-muted)", fontSize: 12 }}>
                        {formatDateTime(b.baixadoEm)}
                      </td>
                      <td style={{ textAlign: "right" }}>
                        <button className="btn btn-secondary btn-sm" onClick={() => downloadBill(b)}>
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                            <polyline points="7 10 12 15 17 10" />
                            <line x1="12" y1="15" x2="12" y2="3" />
                          </svg>
                          {b.arquivo}
                        </button>
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
