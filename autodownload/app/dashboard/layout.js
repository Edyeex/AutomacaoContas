import Sidebar from "../components/Sidebar";
import TopBar from "../components/TopBar";
import DashboardPageShell from "../components/DashboardPageShell";

export default function DashboardLayout({ children }) {
  return (
    <>
      <TopBar />
      <div className="app-layout">
        <Sidebar />
        <main className="main-content">
          <DashboardPageShell>{children}</DashboardPageShell>
        </main>
      </div>
    </>
  );
}
