import { QueryErrorResetBoundary } from '@tanstack/react-query'
import { AlertTriangle, RefreshCw } from 'lucide-react'
import { Component, type ErrorInfo, type ReactNode } from 'react'
import { getUserErrorMessage } from '../../utils/errors'

interface ErrorBoundaryProps {
  children: ReactNode
  onReset: () => void
}

interface ErrorBoundaryState {
  error?: Error
}

class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = {}

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Erro não tratado na interface administrativa.', error, info)
  }

  reset = () => {
    this.props.onReset()
    this.setState({ error: undefined })
  }

  render() {
    if (!this.state.error) return this.props.children
    return (
      <section className="page-error" role="alert">
        <AlertTriangle size={28} />
        <h2>Não foi possível carregar esta tela</h2>
        <p>{getUserErrorMessage(this.state.error)}</p>
        <button className="primary-button" onClick={this.reset}>
          <RefreshCw size={16} /> Tentar novamente
        </button>
      </section>
    )
  }
}

export function AppErrorBoundary({ children }: { children: ReactNode }) {
  return (
    <QueryErrorResetBoundary>
      {({ reset }) => <ErrorBoundary onReset={reset}>{children}</ErrorBoundary>}
    </QueryErrorResetBoundary>
  )
}
