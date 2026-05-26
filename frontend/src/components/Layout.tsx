import { NavLink, Outlet } from 'react-router-dom';

const navItems = [
  { to: '/',              label: 'Dashboard',     icon: '📊', end: true  },
  { to: '/importar',      label: 'Importar',      icon: '📥', end: false },
  { to: '/historial',     label: 'Historial',     icon: '📋', end: false },
  { to: '/configuracion', label: 'Configuración', icon: '⚙️', end: false },
];

export default function Layout() {
  return (
    <div className="min-h-screen bg-slate-950 text-white flex flex-col">
      {/* ── Top bar ── */}
      <header className="border-b border-slate-800/80 bg-slate-900/60 backdrop-blur-sm sticky top-0 z-40">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between">
          {/* Logo */}
          <div className="flex items-center gap-3">
            <span className="text-2xl">💬</span>
            <div>
              <p className="text-sm font-bold text-white leading-none">WAHA Sender</p>
              <p className="text-xs text-slate-500 leading-none mt-0.5">WhatsApp Bot Manager</p>
            </div>
          </div>

          {/* Nav */}
          <nav className="flex items-center gap-1">
            {navItems.map(item => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  `px-3 py-2 rounded-lg text-sm font-medium transition-all duration-200 flex items-center gap-2
                   ${isActive
                     ? 'bg-slate-700/80 text-white'
                     : 'text-slate-400 hover:text-white hover:bg-slate-800/60'}`
                }
              >
                <span>{item.icon}</span>
                <span className="hidden sm:inline">{item.label}</span>
              </NavLink>
            ))}
          </nav>

          {/* Indicator WAHA */}
          <div className="flex items-center gap-2 text-xs text-slate-500">
            <span className="w-2 h-2 rounded-full bg-green-400 animate-pulse" />
            <span className="hidden md:inline">WAHA Docker</span>
          </div>
        </div>
      </header>

      {/* ── Contenido ── */}
      <main className="flex-1 max-w-7xl mx-auto w-full px-4 sm:px-6 py-8">
        <Outlet />
      </main>

      {/* ── Footer ── */}
      <footer className="border-t border-slate-800/60 py-4 text-center text-xs text-slate-700">
        WAHA Sender · Backend .NET 8 + SQLite · Frontend React + Vite
      </footer>
    </div>
  );
}
