# Native BI Finance — SLA y Soporte

**DataBision · Junio 2026**

---

## 1. Horario de soporte

| Plan | Horario de soporte | Zona horaria |
|---|---|---|
| **Starter** | Lunes–Viernes, 9:00 am – 6:00 pm | PET (Lima, Perú) |
| **Professional** | Lunes–Viernes, 8:00 am – 7:00 pm | PET |
| **Enterprise** | Lunes–Viernes, 8:00 am – 8:00 pm + guardia fines de semana | PET |

**Feriados nacionales (Perú):** no hay soporte en horario normal — solo guardia para incidentes críticos Enterprise.

**Canales de soporte disponibles por plan:**

| Canal | Starter | Professional | Enterprise |
|---|---|---|---|
| Email | ✅ | ✅ | ✅ |
| WhatsApp | ❌ | ✅ | ✅ |
| Llamada telefónica | ❌ | ❌ | ✅ |
| Videollamada urgente | ❌ | Bajo solicitud | ✅ |

---

## 2. Niveles de severidad

### Crítica (P1)

**Definición:** El sistema está completamente inaccesible o los datos financieros están incorrectos de forma que impide la toma de decisiones del cliente.

**Ejemplos:**
- API DataBision no responde (HTTP 5xx o timeout)
- Dashboard muestra datos de otro cliente (fuga de datos)
- P&L o Balance con diferencia > 10% respecto al cierre confirmado
- Credenciales SAP comprometidas

**Tiempo de primera respuesta:**

| Plan | Tiempo de respuesta |
|---|---|
| Starter | 4 horas hábiles |
| Professional | 2 horas hábiles |
| Enterprise | 1 hora (hábil o guardia fines de semana) |

**Tiempo de resolución objetivo:**

| Plan | Tiempo de resolución |
|---|---|
| Starter | 24 horas hábiles |
| Professional | 8 horas hábiles |
| Enterprise | 4 horas |

---

### Alta (P2)

**Definición:** El sistema funciona pero con degradación significativa que afecta la operación diaria.

**Ejemplos:**
- Extractor no corrió en más de 25 horas (datos de ayer)
- MART refresh fallido (dashboard desactualizado)
- Uno o más dashboards no cargan pero otros sí
- Cuentas sin clasificar nuevas aparecieron después del go-live
- refresh-status retorna healthScore < 70

**Tiempo de primera respuesta:**

| Plan | Tiempo de respuesta |
|---|---|
| Starter | 8 horas hábiles |
| Professional | 4 horas hábiles |
| Enterprise | 2 horas hábiles |

**Tiempo de resolución objetivo:**

| Plan | Tiempo de resolución |
|---|---|
| Starter | 3 días hábiles |
| Professional | 1 día hábil |
| Enterprise | 8 horas hábiles |

---

### Media (P3)

**Definición:** El sistema funciona correctamente pero hay consultas, ajustes de clasificación u optimizaciones necesarias.

**Ejemplos:**
- Ajuste de clasificación de 1–5 cuentas
- Pregunta sobre la interpretación de un dato del dashboard
- Cambio en el horario del scheduler
- Solicitud de nuevo usuario del dashboard
- Actualización de credenciales SAP no urgente

**Tiempo de primera respuesta:**

| Plan | Tiempo de respuesta |
|---|---|
| Starter | 2 días hábiles |
| Professional | 1 día hábil |
| Enterprise | 4 horas hábiles |

**Tiempo de resolución objetivo:**

| Plan | Tiempo de resolución |
|---|---|
| Starter | 5 días hábiles |
| Professional | 3 días hábiles |
| Enterprise | 1 día hábil |

---

### Baja (P4)

**Definición:** Solicitudes de mejora, consultas informativas, coordinación de revisiones periódicas.

**Ejemplos:**
- Solicitud de nueva funcionalidad
- Consulta sobre el roadmap
- Coordinación de sesión de validación trimestral
- Solicitud de documentación adicional

**Tiempo de primera respuesta:** 5 días hábiles (todos los planes)

---

## 3. SLA de uptime

| Plan | SLA mensual | Tiempo máximo de inactividad/mes |
|---|---|---|
| Starter | 95% | ~36 horas |
| Professional | 98% | ~14 horas |
| Enterprise | 99% | ~7 horas |

**Qué está incluido en el cómputo de uptime:**
- Disponibilidad del API DataBision (endpoints de dashboard)
- Disponibilidad del proceso de refresh (extractor + MART)

**Qué NO está incluido (exclusiones de SLA):**
- Indisponibilidad de SAP Service Layer del cliente
- Indisponibilidad de la infraestructura del cliente (servidor, red)
- Mantenimientos programados avisados con 24 horas de anticipación
- Eventos de fuerza mayor (cortes de luz, desastres naturales, etc.)
- Indisponibilidad de Supabase (fuera del control de DataBision)

**Créditos por incumplimiento de SLA (solo Enterprise):**

| Uptime real en el mes | Crédito |
|---|---|
| 98–99% | Sin crédito |
| 95–98% | 10% del mensual |
| 90–95% | 25% del mensual |
| < 90% | 50% del mensual |

Los créditos se aplican al siguiente período de facturación. No se acumulan.

---

## 4. Qué está incluido en el soporte

| Categoría | Incluido | Excluido |
|---|---|---|
| Retry manual de extractor fallido | ✅ | |
| Re-ejecución de MART | ✅ | |
| Ajuste de clasificación contable (dentro del límite del plan) | ✅ | |
| Actualización de versión del extractor | ✅ | |
| Cambio de horario del scheduler | ✅ | |
| Agregar o desactivar usuario del dashboard | ✅ | |
| Rotación de API key | ✅ | |
| Consulta sobre interpretación de datos | ✅ | |
| Soporte para problemas del servidor del cliente | | ❌ |
| Soporte para problemas de SAP Business One | | ❌ |
| Desarrollo de nuevas funcionalidades | | ❌ (roadmap) |
| Consultoría financiera / contable | | ❌ (servicio adicional) |
| Soporte a licencias SAP | | ❌ |

---

## 5. Responsabilidad del cliente

El cliente es responsable de:

| Responsabilidad | Descripción |
|---|---|
| Disponibilidad de SAP Service Layer | Si SAP está caído, el extractor no puede correr |
| Vigencia del usuario SAP `DATABISION_RO` | Si el usuario es desactivado o la contraseña cambia, notificar inmediatamente |
| Apertura de firewall | Si cambia la IP del extractor, el cliente debe actualizar las reglas de firewall |
| Acceso de usuarios al dashboard | El cliente gestiona quién accede a su plataforma DataBision |
| Comunicar cambios en SAP | Si el cliente agrega cuentas nuevas, cambia el plan de cuentas o hace ajustes masivos en SAP, debe notificar a DataBision |
| Backup de reportes propios | Los reportes del contador del cliente son responsabilidad del cliente |
| Continuidad de operación interna | Si el cliente deja de pagar, el servicio se suspende — la continuidad es responsabilidad del cliente |

---

## 6. Proceso de escalamiento

```
Usuario cliente detecta problema
    ↓
Contacta DataBision por el canal del plan (email / WhatsApp)
    ↓
DataBision evalúa severidad y confirma recepción en el tiempo prometido
    ↓
Si es P1 o P2: DataBision inicia diagnóstico inmediatamente
    ↓
Si requiere acceso adicional o coordinación: DataBision contacta al TI del cliente
    ↓
Resolución dentro del SLA
    ↓
DataBision confirma resolución y documenta en el log de incidentes
```

**Escalamiento a Jonathan Campillay directamente:** Para cualquier incidente donde el soporte de primer nivel no ha respondido en el tiempo prometido.
