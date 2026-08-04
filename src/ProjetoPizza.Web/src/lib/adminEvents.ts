export interface AdminResourceChanged {
  resource: string
  action: string
  source: string
  occurredAt: string
}

export function isNewClientOrder(notification: AdminResourceChanged): boolean {
  return notification.resource === 'orders' &&
    notification.action === 'POST' &&
    notification.source === 'client'
}
