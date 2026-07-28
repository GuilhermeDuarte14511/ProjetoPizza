import { enumCssToken, translateEnum } from '../../utils/presentation'

interface StatusBadgeProps {
  status: string
}

export function StatusBadge({ status }: StatusBadgeProps) {
  return <span className={`status-badge status-${enumCssToken(status)}`}>{translateEnum(status)}</span>
}
