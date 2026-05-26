import { type ReactNode } from 'react';

interface MetricCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: string;
  color: 'green' | 'blue' | 'yellow' | 'red' | 'purple';
  children?: ReactNode;
}

const colorMap = {
  green:  { bg: 'bg-green-500/10',  border: 'border-green-500/30',  text: 'text-green-400',  icon: 'text-green-400' },
  blue:   { bg: 'bg-blue-500/10',   border: 'border-blue-500/30',   text: 'text-blue-400',   icon: 'text-blue-400' },
  yellow: { bg: 'bg-yellow-500/10', border: 'border-yellow-500/30', text: 'text-yellow-400', icon: 'text-yellow-400' },
  red:    { bg: 'bg-red-500/10',    border: 'border-red-500/30',    text: 'text-red-400',    icon: 'text-red-400' },
  purple: { bg: 'bg-purple-500/10', border: 'border-purple-500/30', text: 'text-purple-400', icon: 'text-purple-400' },
};

export default function MetricCard({ title, value, subtitle, icon, color, children }: MetricCardProps) {
  const c = colorMap[color];
  return (
    <div className={`rounded-2xl border ${c.border} ${c.bg} p-5 backdrop-blur-sm flex flex-col gap-3`}>
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-slate-400">{title}</span>
        <span className={`text-2xl ${c.icon}`}>{icon}</span>
      </div>
      <p className={`text-3xl font-bold ${c.text}`}>{value}</p>
      {subtitle && <p className="text-xs text-slate-500">{subtitle}</p>}
      {children}
    </div>
  );
}
