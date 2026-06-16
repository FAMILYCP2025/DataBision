import { Navigate } from 'react-router-dom'

// Native BI is the primary experience. The legacy Power BI "Módulos" section is
// only relevant when reports are assigned, so the client landing page always
// redirects to the Native BI dashboard instead of the first (often empty) module.
export default function ClientHomePage() {
  return <Navigate to="/client/bi/dashboard" replace />
}
