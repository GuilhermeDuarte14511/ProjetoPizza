import { ArrowRight, ChefHat, Eye, EyeOff, LockKeyhole, UserRound } from 'lucide-react'
import { type FormEvent, useState } from 'react'
import { useLocation } from 'wouter'
import { adminService } from '../services/adminService'
import { saveAuthentication } from '../services/authSession'

export function LoginPage() {
  const [showPassword, setShowPassword] = useState(false)
  const [email, setEmail] = useState('admin@projetopizza.local')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setSubmitting] = useState(false)
  const [, navigate] = useLocation()

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setError('')
    try {
      const result = await adminService.login(email, password)
      saveAuthentication(result)
      navigate('/admin/dashboard')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Não foi possível autenticar.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="login-page">
      <section className="login-brand-panel">
        <div className="login-brand"><ChefHat size={24} /> <strong>Forno 27</strong></div>
        <div className="login-pitch">
          <span className="eyebrow">ProjetoPizza</span>
          <h1>Gestão simples para uma operação mais eficiente.</h1>
          <p>Acesse o painel de controle da sua unidade principal.</p>
        </div>
      </section>
      <section className="login-form-panel">
        <form className="login-card" onSubmit={submit}>
          <span className="brand-mark large"><ChefHat /></span>
          <h2>Bem-vindo de volta</h2>
          <p>Insira suas credenciais para acessar o sistema.</p>
          <div className="login-field"><label htmlFor="login-email">Usuário ou e-mail</label><div className="input-with-icon"><UserRound size={18} /><input id="login-email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="admin@projetopizza.local" required /></div></div>
          <div className="login-field"><label htmlFor="login-password">Senha</label><div className="input-with-icon"><LockKeyhole size={18} /><input id="login-password" type={showPassword ? 'text' : 'password'} value={password} onChange={(event) => setPassword(event.target.value)} placeholder="••••••••" required /><button type="button" aria-label={showPassword ? 'Ocultar senha' : 'Exibir senha'} onClick={() => setShowPassword((value) => !value)}>{showPassword ? <EyeOff size={18} /> : <Eye size={18} />}</button></div></div>
          <div className="login-options"><label className="check-label"><input type="checkbox" /> Manter conectado</label><a href="#recuperar">Esqueceu a senha?</a></div>
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="primary-button login-submit" disabled={isSubmitting}>{isSubmitting ? 'Entrando...' : 'Entrar no sistema'} <ArrowRight size={18} /></button>
          <div className="server-status"><span className="status-dot" /> Servidor conectado</div>
        </form>
      </section>
    </main>
  )
}
