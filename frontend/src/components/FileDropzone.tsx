import { useCallback, useState } from 'react';

interface FileDropzoneProps {
  onFile: (file: File) => void;
  isLoading?: boolean;
}

const ACCEPTED = ['.xlsx', '.csv'];

export default function FileDropzone({ onFile, isLoading = false }: FileDropzoneProps) {
  const [isDragging, setIsDragging] = useState(false);
  const [fileName, setFileName]     = useState<string | null>(null);
  const [error, setError]           = useState<string | null>(null);

  const handleFile = useCallback((file: File) => {
    const ext = file.name.split('.').pop()?.toLowerCase();
    if (!ext || !ACCEPTED.includes(`.${ext}`)) {
      setError('Solo se aceptan archivos .xlsx o .csv');
      return;
    }
    setError(null);
    setFileName(file.name);
    onFile(file);
  }, [onFile]);

  const onDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files[0];
    if (file) handleFile(file);
  }, [handleFile]);

  const onInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFile(file);
  };

  return (
    <div className="flex flex-col gap-2">
      <label
        htmlFor="file-dropzone-input"
        onDragOver={e => { e.preventDefault(); setIsDragging(true); }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={onDrop}
        className={`
          relative flex flex-col items-center justify-center gap-4 rounded-2xl border-2 border-dashed
          cursor-pointer transition-all duration-200 p-10
          ${isDragging
            ? 'border-blue-400 bg-blue-500/10 scale-[1.01]'
            : 'border-slate-600 bg-slate-800/40 hover:border-slate-500 hover:bg-slate-800/60'}
          ${isLoading ? 'pointer-events-none opacity-60' : ''}
        `}
      >
        <input
          id="file-dropzone-input"
          type="file"
          accept=".xlsx,.csv"
          className="sr-only"
          onChange={onInputChange}
          disabled={isLoading}
        />
        <div className="text-5xl">{isLoading ? '⏳' : isDragging ? '📂' : '📁'}</div>
        <div className="text-center">
          {isLoading ? (
            <p className="text-blue-400 font-medium animate-pulse">Procesando archivo...</p>
          ) : fileName ? (
            <>
              <p className="text-green-400 font-semibold">✅ {fileName}</p>
              <p className="text-xs text-slate-500 mt-1">Haz clic para cambiar el archivo</p>
            </>
          ) : (
            <>
              <p className="text-slate-300 font-semibold">Arrastra tu archivo aquí</p>
              <p className="text-slate-500 text-sm mt-1">o haz clic para seleccionar</p>
              <p className="text-slate-600 text-xs mt-2">Soporta .xlsx y .csv · Máx. 10 MB</p>
            </>
          )}
        </div>
      </label>
      {error && (
        <p className="text-red-400 text-sm flex items-center gap-1.5">
          <span>⚠️</span> {error}
        </p>
      )}
    </div>
  );
}
