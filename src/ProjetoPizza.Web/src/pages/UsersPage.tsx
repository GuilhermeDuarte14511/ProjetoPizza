import { zodResolver } from '@hookform/resolvers/zod'
import { KeyRound, Plus, Save, ShieldCheck, UserRound, UsersRound } from 'lucide-react'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { FieldError } from '../components/ui/FieldError'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { roleSchema, userSchema, type RoleFormData, type UserFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { AdminRole, AdminUser } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { translateEnum } from '../utils/presentation'

const permissions = ['admin:read', 'admin:write', 'operations:read', 'operations:write']
const permissionLabels: Record<string, string> = {
  'admin:read': 'Consultar administração',
  'admin:write': 'Alterar administração',
  'operations:read': 'Consultar operação',
  'operations:write': 'Alterar operação',
}

export function UsersPage({ tab }: { tab: 'users' | 'roles' }) {
  const { data: users, setData: setUsers } = useAdminQuery(queryKeys.users, adminService.users)
  const { data: roles, setData: setRoles } = useAdminQuery(queryKeys.roles, adminService.roles)
  const [editingUserId, setEditingUserId] = useState<string>()
  const [editingRoleId, setEditingRoleId] = useState<string>()
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const userForm = useForm<UserFormData>({
    resolver: zodResolver(userSchema),
    defaultValues: { displayName: '', email: '', employeeCode: '', password: '', phone: '', isActive: true, roles: [] },
  })
  const roleForm = useForm<RoleFormData>({
    resolver: zodResolver(roleSchema),
    defaultValues: { name: '', permissions: [], userCount: 0 },
  })
  const selectedPermissions = useWatch({ control: roleForm.control, name: 'permissions' }) ?? []

  function editUser(user?: AdminUser) {
    userForm.reset(user ? { ...user, password: '', phone: user.phone ?? '' } : { displayName: '', email: '', employeeCode: '', password: '', phone: '', isActive: true, roles: [] })
    setEditingUserId(user?.id ?? 'new')
  }

  function editRole(role?: AdminRole) {
    roleForm.reset(role ?? { name: '', permissions: [], userCount: 0 })
    setEditingRoleId(role?.id ?? 'new')
  }

  async function saveUser(draft: UserFormData) {
    if (!draft.id && !draft.password) {
      userForm.setError('password', { message: 'A senha é obrigatória no cadastro.' }, { shouldFocus: true })
      return
    }
    setSaving(true)
    try {
      const id = await adminService.saveUser(draft)
      const saved = { id: draft.id ?? id, email: draft.email!, displayName: draft.displayName!, employeeCode: draft.employeeCode!, phone: draft.phone, isActive: draft.isActive ?? true, roles: draft.roles ?? [] }
      setUsers((current) => draft.id ? current.map((item) => item.id === draft.id ? saved : item) : [...current, saved])
      setEditingUserId(undefined)
      toast.success(draft.id ? 'Usuário atualizado' : 'Usuário adicionado', `${draft.displayName} foi salvo com sucesso.`)
    } catch (error) {
      toast.error('Não foi possível salvar o usuário', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  async function saveRole(draft: RoleFormData) {
    setSaving(true)
    try {
      const id = await adminService.saveRole(draft)
      const saved = { id: draft.id ?? id, name: draft.name!, permissions: draft.permissions ?? [], userCount: draft.userCount ?? 0 }
      setRoles((current) => draft.id ? current.map((item) => item.id === draft.id ? saved : item) : [...current, saved])
      setEditingRoleId(undefined)
      toast.success(draft.id ? 'Perfil atualizado' : 'Perfil adicionado', `${draft.name} foi salvo com sucesso.`)
    } catch (error) {
      toast.error('Não foi possível salvar o perfil', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <PageHeader title="Usuários e permissões" description="Gerencie acessos e responsabilidades da equipe." actions={hasPermission('admin:write') && <button className="primary-button" onClick={() => tab === 'users' ? editUser() : editRole()}><Plus size={16} /> {tab === 'users' ? 'Novo usuário' : 'Novo perfil'}</button>} />
      <nav className="settings-tabs" aria-label="Usuários e permissões" role="tablist"><ViewTransitionLink role="tab" aria-selected={tab === 'users'} href="/admin/users" className={tab === 'users' ? 'active' : ''}><UsersRound size={15} /> Usuários</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={tab === 'roles'} href="/admin/roles" className={tab === 'roles' ? 'active' : ''}><ShieldCheck size={15} /> Perfis e permissões</ViewTransitionLink></nav>
      {editingUserId && <Modal open title={userForm.getValues('id') ? 'Editar usuário' : 'Novo usuário'} description="A senha é obrigatória somente no cadastro." size="large" isBusy={saving} onClose={() => setEditingUserId(undefined)}>
        <form onSubmit={userForm.handleSubmit(saveUser)} autoComplete="off" noValidate>
          <div className="modal-body"><div className="form-grid three-columns">
            <label className="field-label">Nome<input autoFocus aria-invalid={Boolean(userForm.formState.errors.displayName)} {...userForm.register('displayName')} /><FieldError message={userForm.formState.errors.displayName?.message} /></label>
            <label className="field-label">E-mail<input type="email" autoComplete="off" aria-invalid={Boolean(userForm.formState.errors.email)} {...userForm.register('email')} /><FieldError message={userForm.formState.errors.email?.message} /></label>
            <label className="field-label">Código<input aria-invalid={Boolean(userForm.formState.errors.employeeCode)} {...userForm.register('employeeCode')} /><FieldError message={userForm.formState.errors.employeeCode?.message} /></label>
            <label className="field-label">Telefone<input {...userForm.register('phone')} /><FieldError message={userForm.formState.errors.phone?.message} /></label>
            <label className="field-label">Senha<input type="password" autoComplete="new-password" aria-invalid={Boolean(userForm.formState.errors.password)} {...userForm.register('password')} /><FieldError message={userForm.formState.errors.password?.message} /></label>
            <label className="field-label">Perfil<select {...userForm.register('roles.0')}><option value="">Sem perfil</option>{roles.map((role) => <option key={role.id} value={role.name}>{translateEnum(role.name)}</option>)}</select></label>
            <label className="check-label wide"><input type="checkbox" {...userForm.register('isActive')} /> Usuário ativo</label>
          </div></div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={() => setEditingUserId(undefined)}>Cancelar</button><button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar usuário'}</button></div>
        </form>
      </Modal>}
      {editingRoleId && <Modal open title={roleForm.getValues('id') ? 'Editar perfil' : 'Novo perfil'} description="As permissões serão validadas pelo servidor." isBusy={saving} onClose={() => setEditingRoleId(undefined)}>
        <form onSubmit={roleForm.handleSubmit(saveRole)} noValidate>
          <div className="modal-body"><div className="form-grid">
            <label className="field-label">Nome do perfil<input autoFocus aria-invalid={Boolean(roleForm.formState.errors.name)} {...roleForm.register('name')} /><FieldError message={roleForm.formState.errors.name?.message} /></label>
            <fieldset className="permissions-grid"><legend>Permissões</legend>{permissions.map((permission) => <label className="check-label" key={permission}><input type="checkbox" checked={selectedPermissions.includes(permission)} onChange={(event) => roleForm.setValue('permissions', event.target.checked ? [...selectedPermissions, permission] : selectedPermissions.filter((item) => item !== permission), { shouldDirty: true, shouldValidate: true })} /> {permissionLabels[permission]}</label>)}<FieldError message={roleForm.formState.errors.permissions?.message} /></fieldset>
          </div></div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={() => setEditingRoleId(undefined)}>Cancelar</button><button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar perfil'}</button></div>
        </form>
      </Modal>}
      {tab === 'users' ? <section className="management-grid">{users.map((user) => <article className="surface-card management-card" key={user.id}><div className="avatar large-avatar"><UserRound /></div><div><span className={`status-pill ${user.isActive ? 'success' : 'danger'}`}>{user.isActive ? 'Ativo' : 'Inativo'}</span><h2>{user.displayName}</h2><p>{user.email}</p><small>{user.employeeCode} · {user.roles.map(translateEnum).join(', ') || 'Sem perfil'}</small></div>{hasPermission('admin:write') && <button className="secondary-button" onClick={() => editUser(user)}><KeyRound size={15} /> Editar acesso</button>}</article>)}</section>
        : <section className="management-grid">{roles.map((role) => <article className="surface-card management-card" key={role.id}><div className="device-icon"><ShieldCheck /></div><div><h2>{translateEnum(role.name)}</h2><p>{role.userCount} usuário(s)</p><small>{role.permissions.map((permission) => permissionLabels[permission] ?? permission).join(' · ') || 'Sem permissões'}</small></div>{hasPermission('admin:write') && <button className="secondary-button" onClick={() => editRole(role)}>Editar perfil</button>}</article>)}</section>}
    </>
  )
}
