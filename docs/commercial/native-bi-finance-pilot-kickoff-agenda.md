# Native BI Finance — Agenda Reunión Kickoff

**Sprint:** 22F  
**Duración estimada:** 60–90 minutos  
**Participantes:** Consultor DataBision + Gerente Financiero + Responsable TI del cliente

---

## Objetivo de la reunión

Alinear expectativas, confirmar datos técnicos, establecer cronograma y dejar al equipo del cliente preparado para el piloto de 5 días.

---

## Agenda

### 1. Bienvenida y contexto (5 min)

- Presentación del equipo DataBision
- Objetivo del piloto: validar que los datos financieros de SAP B1 se reflejan correctamente en los dashboards de Native BI Finance

### 2. Presentación de la solución (15 min)

**Mostrar en live (si hay conexión):**
- Dashboard P&L: estructura de ingresos, COGS, gastos operativos, utilidad neta
- Balance General: activos, pasivos, patrimonio
- EBITDA: utilidad + depreciación + amortización
- Widget de estado de actualización (refresh-status)

**Explicar:**
- Origen de los datos: SAP B1 Service Layer → extractor → DataBision
- Frecuencia de actualización: diaria/programada (scheduler)
- Qué NO hace: no modifica datos SAP, solo lectura

### 3. Confirmación de datos técnicos SAP (15 min)

**Recopilar en vivo:**

| Dato | Valor a confirmar |
|---|---|
| URL de Service Layer | https://... |
| CompanyDB (exacto) | |
| Usuario SAP | |
| Año fiscal activo | |
| Moneda principal | |
| Plan de cuentas | PCGE / CONCAR / otro |

**Validar:**
- ¿El usuario SAP ya fue creado con permisos de lectura?
- ¿Se permite conexión desde el servidor de DataBision?
- ¿El certificado SSL es válido o necesita `IgnoreSslCertificateErrors=true`?

### 4. Alcance y exclusiones del piloto (10 min)

**Incluido:**
- Extracción OACT + OJDT del año en curso
- Dashboards P&L, Balance, EBITDA
- 1 sesión de validación contable
- 1 sesión de capacitación

**Excluido:**
- Datos históricos > 1 año (negociable)
- Integración con Power BI Desktop
- Consolidación multi-empresa
- Módulo de compras o inventario

### 5. Cronograma del piloto (5 min)

| Día | Actividad |
|---|---|
| Día 0 (hoy) | Kickoff + test de conexión técnico |
| Día 1 | Primera extracción OACT + OJDT |
| Día 2 | Validación contable con contador |
| Día 3 | Ajustes de clasificación |
| Día 4 | Capacitación + demostración final |
| Día 5 | Go/No-Go + cierre del piloto |

### 6. Acuerdo de confidencialidad (5 min)

- DataBision no comparte datos del cliente con terceros
- Los datos viven en la base de datos del propio cliente (Supabase dedicado por tenant)
- El acceso SAP es de solo lectura, nunca se modifican datos en SAP
- El consultor no imprime ni registra contraseñas

### 7. Preguntas y siguientes pasos (10 min)

**Preguntas frecuentes:**
- ¿Qué pasa con los datos cuando termina el piloto? → Se eliminan o migran según acuerdo
- ¿Quién puede ver los dashboards? → Solo usuarios configurados por el admin del cliente
- ¿Qué pasa si los números no cuadran? → Día 3 está dedicado a ajustes

**Siguientes pasos:**
1. Confirmar usuario SAP y URL de Service Layer por email/chat
2. Dar acceso de red al servidor de DataBision
3. Consultor configura perfil de conexión en Admin
4. Ejecutar test-connection y confirmar éxito
5. Iniciar extracción Día 1

---

## Checklist pre-kickoff (consultor DataBision)

- [ ] Credenciales SAP recibidas y guardadas en SecretRef
- [ ] Perfil de conexión creado en Admin
- [ ] Test-connection exitoso antes de la reunión
- [ ] Dashboard de demo preparado para mostrar
- [ ] Cronograma compartido con cliente por email
