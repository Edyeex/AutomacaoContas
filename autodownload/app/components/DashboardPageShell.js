"use client";

import { usePathname } from "next/navigation";

export default function DashboardPageShell({ children }) {
  const pathname = usePathname();

  return (
    <div key={pathname} className="page-transition-shell">
      {children}
    </div>
  );
}
