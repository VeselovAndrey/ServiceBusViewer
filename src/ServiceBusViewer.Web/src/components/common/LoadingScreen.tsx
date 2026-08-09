export function LoadingScreen({ message }: { message: string }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 px-4 dark:bg-slate-950">
      <div className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-white/90 px-5 py-4 text-sm text-slate-600 shadow-lg shadow-slate-200/40 dark:border-slate-800 dark:bg-slate-900/90 dark:text-slate-300 dark:shadow-black/20">
        <span className="h-4 w-4 animate-spin rounded-full border-2 border-brand-200 border-t-brand-600 dark:border-brand-900 dark:border-t-brand-300" />
        <span>{message}</span>
      </div>
    </div>
  );
}
