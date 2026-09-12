import { useState } from "react";
import { Outlet } from "react-router-dom";
import { Header } from "./Header";

export function AuthenticatedLayout() {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  return (
    <div className="min-h-screen bg-[#f6f7fb]">
      <Header
        collapsed={sidebarCollapsed}
        onToggleCollapsed={() => setSidebarCollapsed((value) => !value)}
      />
      <main
        className={`min-h-screen px-4 pb-8 pt-[94px] transition-[margin] sm:px-6 ${
          sidebarCollapsed ? "lg:ml-[76px]" : "lg:ml-[240px]"
        }`}>
        <Outlet />
      </main>
    </div>
  );
};