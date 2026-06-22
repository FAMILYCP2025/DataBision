# Native BI Finance — Alcance Contractual del Servicio

**DataBision · Junio 2026**  
**Versión:** 1.0  
*Este documento define el alcance del servicio. No es un contrato legal. Para contratos formales, consultar con asesor legal.*

---

## 1. Descripción del servicio

DataBision Native BI Finance es un servicio SaaS de analítica financiera para empresas que operan SAP Business One. El servicio extrae datos contables de SAP de forma no invasiva (solo lectura) y los presenta en dashboards financieros actualizados diariamente.

El servicio incluye:
- Software de extracción (Extractor)
- API de procesamiento y clasificación (DataBision API)
- Dashboard web accesible por navegador
- Clasificación contable PCGE configurable por empresa
- Proceso de refresh diario automatizado
- Soporte técnico y operativo según el plan contratado

---

## 2. Lo que el servicio hace

| Actividad | Descripción |
|---|---|
| Extracción de datos | Lee OACT (plan de cuentas), OJDT (asientos) y JDT1 (líneas) desde SAP B1 via Service Layer |
| Clasificación | Mapea cada cuenta SAP a una categoría financiera según PCGE, definida con el contador del cliente |
| Procesamiento | Calcula P&L, Balance, EBITDA y Flujo de Efectivo desde los datos extraídos |
| Visualización | Presenta los estados financieros en un dashboard web por tenant |
| Operación | Ejecuta el proceso de refresh diario de forma automatizada |
| Soporte | Responde incidentes, ajusta clasificaciones y actualiza el software según el SLA del plan |

---

## 3. Lo que el servicio NO hace

| Restricción | Explicación |
|---|---|
| No modifica datos en SAP | El extractor solo realiza peticiones GET — ninguna escritura |
| No es un ERP | No reemplaza SAP Business One ni ningún sistema contable |
| No emite estados financieros con valor legal | Los estados son para gestión interna — no reemplazan los estados firmados por el contador |
| No toma decisiones contables | La clasificación de cuentas la decide el contador del cliente |
| No accede a datos fuera de OACT y OJDT/JDT1 | El extractor no lee facturas, órdenes, inventario ni datos de RRHH en el módulo Finance |
| No reemplaza al contador | El contador sigue siendo responsable del cierre contable, auditorías y estados financieros oficiales |
| No ofrece garantía de exactitud 100% | La exactitud depende de que la clasificación contable sea correcta — validada con el contador del cliente |

---

## 4. Seguridad del servicio

### Compromiso de DataBision

| Control | Implementación |
|---|---|
| Credenciales SAP no expuestas | Almacenadas como SecretRef (`env:` o `azure-kv://`) — nunca en el dashboard ni en código |
| Aislamiento por empresa | Cada cliente tiene un `company_id` único — los datos de un cliente no son accesibles para otro |
| HTTPS en todas las comunicaciones | API y dashboard solo accesibles via HTTPS con certificado válido |
| No almacenamiento de datos sensibles de usuarios | No se guarda información personal más allá del email y rol del usuario del dashboard |
| SSL en conexión SAP | Verificación de certificado SSL del Service Layer (excepto TST explícitamente documentado) |

### Lo que el cliente debe garantizar

| Control | Responsabilidad del cliente |
|---|---|
| Contraseña SAP segura | El cliente define la contraseña del usuario `DATABISION_RO` |
| Acceso restringido al servidor del extractor | TI del cliente es responsable de proteger el servidor donde corre el extractor |
| No compartir API keys | El cliente no debe compartir la API key de ingest con terceros |
| Notificar cambios en SAP | Si SAP cambia la URL del Service Layer, el CompanyDB o los permisos del usuario, el cliente debe notificar a DataBision |

---

## 5. Tratamiento de datos

| Aspecto | Política DataBision |
|---|---|
| **Propiedad de datos** | Los datos SAP extraídos son propiedad exclusiva del cliente |
| **Uso de datos** | DataBision usa los datos únicamente para prestar el servicio al cliente — nunca para análisis propios, benchmarking o venta a terceros |
| **Retención** | Los datos se retienen mientras el servicio esté activo. Al cancelar: eliminación en máximo 30 días hábiles |
| **Exportación** | El cliente puede solicitar exportación de sus datos en formato CSV/JSON en cualquier momento |
| **Backup** | DataBision mantiene backups operacionales en Supabase para continuidad del servicio |
| **Confidencialidad** | DataBision trata los datos del cliente como confidenciales y no los divulga sin autorización expresa del cliente |

---

## 6. Continuidad del servicio

| Escenario | Acción de DataBision |
|---|---|
| Mantenimiento programado | Aviso con 48 horas de anticipación. Duración máxima 4 horas fuera de horario hábil |
| Actualización del extractor | Sin impacto en datos — el extractor se actualiza sin eliminar datos existentes |
| Migración de base de datos | Aviso con 5 días hábiles. Sin pérdida de datos — proceso validado previamente |
| Indisponibilidad de Supabase | Fuera del control de DataBision — se comunica al cliente y se espera resolución del proveedor |
| Cierre de DataBision | En caso de cierre del servicio, se entrega exportación completa de datos del cliente con 60 días de aviso |

---

## 7. Salida del servicio

Si el cliente decide cancelar o DataBision cierra el servicio:

| Paso | Responsable | Plazo |
|---|---|---|
| Notificación de cancelación | Cliente | Según plazo del plan (30/60/90 días) |
| Exportación de datos del cliente | DataBision | Dentro de 5 días hábiles post-cancelación |
| Entrega de runbook final | DataBision | Dentro de 5 días hábiles |
| Desactivación del extractor | DataBision | Confirmado con el cliente |
| Eliminación de credenciales SAP | DataBision | Inmediata post-desactivación |
| Eliminación de datos en Supabase | DataBision | Dentro de 30 días hábiles |
| Desactivación del usuario en DataBision | DataBision | Inmediata |
| Eliminación de perfiles de conexión | DataBision | Dentro de 30 días hábiles |

El cliente recibe confirmación escrita de que todos los datos han sido eliminados.

---

## 8. Propiedad intelectual

| Aspecto | Detalle |
|---|---|
| **Software DataBision** | Propiedad exclusiva de DataBision. El cliente recibe licencia de uso — no de código fuente |
| **Clasificación contable** | Las reglas de clasificación definidas con el contador del cliente son propiedad del cliente |
| **Datos exportados** | Son propiedad del cliente |
| **Runbook operativo** | El cliente puede usar el runbook entregado incluso después de cancelar el servicio |

---

## 9. Confidencialidad

DataBision se compromete a:
- No divulgar el nombre del cliente ni detalles de su operación a terceros sin autorización expresa
- No usar datos del cliente para materiales de marketing sin consentimiento
- Tratar los estados financieros y clasificaciones contables como información confidencial

El cliente se compromete a:
- No revelar la configuración técnica interna de DataBision a competidores
- No intentar replicar el sistema sin autorización
- No compartir accesos del dashboard con personas externas a la organización sin coordinar con DataBision

---

## 10. Limitación de responsabilidad

DataBision no es responsable por:
- Decisiones financieras tomadas con base en los datos del dashboard
- Errores en los datos de SAP del cliente que se repliquen en el dashboard
- Indisponibilidad de SAP Service Layer del cliente
- Pérdidas por interrupciones dentro del SLA pactado
- Cambios en SAP B1 por actualizaciones del proveedor SAP que requieran ajustes en el extractor (estos ajustes se incluyen en la suscripción pero tienen SLA propio de corrección)

La responsabilidad máxima de DataBision en cualquier reclamación está limitada al monto mensual del plan contratado.
