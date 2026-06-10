import type { ReactNode } from 'react'

interface Props {
  title: string
  description?: string
  badge?: string
  actions?: ReactNode
}

export default function NativeBiPageHeader({ title, description, badge = 'Native BI', actions }: Props) {
  return (
    <div className="nb-page-header">
      <div className="nb-page-header__left">
        <div className="nb-page-header__title-row">
          <h1 className="cp-page-title" style={{ margin: 0 }}>{title}</h1>
          {badge && (
            <span
              className="db-badge db-badge--info"
              style={{ fontSize: 10.5, letterSpacing: '0.04em', textTransform: 'uppercase', alignSelf: 'center' }}
            >
              {badge}
            </span>
          )}
        </div>
        {description && (
          <p className="cp-page-subtitle" style={{ margin: '2px 0 0' }}>{description}</p>
        )}
      </div>
      {actions && <div className="nb-page-header__actions">{actions}</div>}
    </div>
  )
}
