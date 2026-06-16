# Demo KSDEPOR — Screenshot Guide

Sprint 8O — Junio 2026

Guía para capturar screenshots que documenten el estado visual de la demo para uso en propuestas comerciales y seguimiento post-demo.

---

## Setup para screenshots

**Browser recomendado:** Chrome o Edge en modo ventana (no maximizado para control de tamaño).

**Resolución recomendada:** 1440×900 o 1280×800.

**Extensión útil:** GoFullPage o similar para capturas de página completa.

**Herramienta:** Snip & Sketch (Win+Shift+S) para capturas de región.

**Carpeta destino:** `docs/demo-screenshots/`

---

## Screenshots mínimos requeridos (pre-demo)

### 1. Login Screen

**Nombre archivo:** `01-login.png`

**Cómo:**
1. Abrir `http://localhost:5173/client/login?tenant=ksdepor`
2. No ingresar credenciales todavía
3. Capturar pantalla completa

**Qué debe verse:** Pantalla de login limpia, sin errores.

---

### 2. Ventas — Vista principal

**Nombre archivo:** `02-ventas-overview.png`

**Cómo:**
1. Navegar a `/client/bi/sales?tenant=ksdepor`
2. Esperar que carguen los KPI cards (sin skeleton)
3. Dejar el tab "Clientes" activo
4. Capturar pantalla completa (scroll arriba)

**Qué debe verse:** Header "Ventas", DateRangePicker, 4 KPI cards, tabla de clientes.

---

### 3. Ventas — Tab Fulfillment

**Nombre archivo:** `03-ventas-fulfillment.png`

**Cómo:**
1. En la página de Ventas, hacer click en tab "Fulfillment"
2. Esperar que cargue la tabla
3. Capturar la sección de la tabla

**Qué debe verse:** Tabla de cumplimiento con "Tasa cumplimiento" coloreada.

---

### 4. Compras — Vista principal

**Nombre archivo:** `04-compras-overview.png`

**Cómo:**
1. Navegar a `/client/bi/purchasing?tenant=ksdepor`
2. Esperar que carguen los KPI cards
3. Tab "Proveedores" activo
4. Capturar pantalla completa

**Qué debe verse:** Header "Compras", KPI cards, tabla de proveedores.

---

### 5. Inventario — Tab Rotación

**Nombre archivo:** `05-inventario-rotacion.png`

**Cómo:**
1. Navegar a `/client/bi/inventory?tenant=ksdepor`
2. Esperar que cargue el tab "Rotación"
3. Capturar pantalla completa

**Qué debe verse:** KPI cards con conteos de rotación, tabla con badges de colores.

---

### 6. Finanzas — Tab AR

**Nombre archivo:** `06-finanzas-ar.png`

**Cómo:**
1. Navegar a `/client/bi/finance?tenant=ksdepor`
2. Tab "Cuentas por cobrar (AR)" activo (default)
3. Capturar pantalla completa

**Qué debe verse:** KPI cards con montos, tabla de AR aging con montos vencidos en rojo.

---

### 7. Operaciones — Vista principal

**Nombre archivo:** `07-operaciones-overview.png`

**Cómo:**
1. Navegar a `/client/bi/operations?tenant=ksdepor`
2. Esperar que cargue el health score y estado del pipeline
3. Capturar pantalla completa

**Qué debe verse:** Health score, status Extractor/Transform con puntos de color, tab Alertas.

---

### 8. Sidebar — Sección Análisis completa

**Nombre archivo:** `08-sidebar-analisis.png`

**Cómo:**
1. Estar en cualquier pantalla del portal
2. Capturar solo el sidebar (región izquierda)
3. Asegurarse que todos los items de la sección "Análisis" sean visibles

**Qué debe verse:** Ventas, Compras, Inventario, Finanzas, Operaciones en la sección Análisis.

---

## Nomenclatura de archivos

| Pantalla | Archivo | Uso |
|---|---|---|
| Login | `01-login.png` | Referencia inicial |
| Ventas overview | `02-ventas-overview.png` | Propuesta comercial |
| Ventas fulfillment | `03-ventas-fulfillment.png` | Diferenciador |
| Compras | `04-compras-overview.png` | Propuesta comercial |
| Inventario rotación | `05-inventario-rotacion.png` | Propuesta comercial |
| Finanzas AR | `06-finanzas-ar.png` | Propuesta comercial |
| Operaciones | `07-operaciones-overview.png` | Argumento técnico |
| Sidebar | `08-sidebar-analisis.png` | Arquitectura visual |

---

## Notas

- Screenshots se guardan en `docs/demo-screenshots/` — esta carpeta **no** se sube a git por defecto (agregar a `.gitignore` si se desea).
- Los screenshots son para uso interno y en propuestas — no contienen datos sensibles (datos son del ambiente de prueba de KSDEPOR).
- Si hay datos reales de clientes visibles, ofuscar con herramienta de edición antes de compartir externamente.
- Para una propuesta formal, usar Figma o PowerPoint para componer los screenshots en una presentación con el branding de DataBision.
