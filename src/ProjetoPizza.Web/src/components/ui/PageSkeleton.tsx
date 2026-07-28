export function PageSkeleton() {
  return (
    <div className="page-skeleton" role="status" aria-live="polite" aria-label="Carregando conteúdo">
      <div className="skeleton-line skeleton-title" />
      <div className="skeleton-line skeleton-subtitle" />
      <div className="skeleton-grid">
        {Array.from({ length: 6 }, (_, index) => <div className="skeleton-card" key={index} />)}
      </div>
      <span className="sr-only">Carregando...</span>
    </div>
  )
}
