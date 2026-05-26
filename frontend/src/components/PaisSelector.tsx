import { PAISES_SUDAMERICA } from '../types';

interface PaisSelectorProps {
  value: string;
  onChange: (codigo: string) => void;
}

export default function PaisSelector({ value, onChange }: PaisSelectorProps) {
  const selected = PAISES_SUDAMERICA.find(p => p.codigo === value) ?? PAISES_SUDAMERICA[0];

  return (
    <div className="flex flex-col gap-1.5">
      <label className="text-sm font-medium text-slate-300">Código de País</label>
      <div className="relative">
        <select
          id="pais-selector"
          value={value}
          onChange={e => onChange(e.target.value)}
          className="w-full appearance-none rounded-xl border border-slate-700 bg-slate-800/80 px-4 py-3 pr-10
                     text-white text-sm font-medium cursor-pointer
                     focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                     transition-all duration-200 hover:border-slate-500"
        >
          {PAISES_SUDAMERICA.map(p => (
            <option key={p.codigo} value={p.codigo}>
              {p.bandera}  +{p.codigo} — {p.nombre}
            </option>
          ))}
        </select>
        {/* Flecha custom */}
        <div className="pointer-events-none absolute inset-y-0 right-3 flex items-center">
          <svg className="h-4 w-4 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
          </svg>
        </div>
      </div>
      <p className="text-xs text-slate-500">
        {selected.bandera} +{selected.codigo} · El código se prependerá a todos los números del archivo.
      </p>
    </div>
  );
}
