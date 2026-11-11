import { useState } from "react";
import { Outlet, useLocation } from "react-router-dom";
import { Header } from "./Header";
import { TermsAcceptanceGate } from "./legal/TermsAcceptanceGate";
import { Footer } from "./Footer";

export function AuthenticatedLayout() {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const location = useLocation();

  return (
    <TermsAcceptanceGate>
      <div className="flex min-h-screen flex-col bg-[#f6f7fb]">
        <Header
          collapsed={sidebarCollapsed}
          onToggleCollapsed={() => setSidebarCollapsed((value) => !value)}
        />
        <main
          className={`flex-1 px-4 pb-8 pt-[94px] transition-[margin] duration-300 ease-[cubic-bezier(0.22,1,0.36,1)] sm:px-6 ${
            sidebarCollapsed ? "lg:ml-[76px]" : "lg:ml-[240px]"
          }`}
        >
          <div key={location.pathname} className="page-enter">
            <Outlet />
          </div>
        </main>
        <Footer />
      </div>
    </TermsAcceptanceGate>
  );
}
