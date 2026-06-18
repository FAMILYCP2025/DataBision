# DataBision — Módulo de Finanzas Contables

## Tus estados financieros directo desde SAP Business One

---

### El problema que resuelve

Los CFOs y Gerentes de Finanzas en empresas SAP B1 pasan semanas cada mes:

- Exportando datos de SAP a Excel
- Construyendo manualmente el P&L, Balance y EBITDA
- Reconciliando discrepancias entre reportes del equipo de contabilidad
- Esperando que el área de TI genere informes estáticos

**El resultado:** decisiones tardías basadas en datos desactualizados.

---

### Qué entrega DataBision Finance

| Reporte | Qué muestra | Frecuencia |
|---|---|---|
| **Estado de Resultados** | Revenue, COGS, Margen Bruto, OPEX, Utilidad — por mes | Diaria / semanal |
| **EBITDA** | Rentabilidad operacional por período, tendencia 12 meses | Diaria / semanal |
| **Balance General** | Activos, Pasivos, Patrimonio al cierre de cada período | Diaria / semanal |
| **Plan de Cuentas** | Cada cuenta SAP con su clasificación y saldo | Diaria / semanal |
| **Validaciones** | Health score contable, balance cuadra, cuentas sin clasificar | En tiempo real |

Todo calculado automáticamente desde el libro diario SAP — sin exportaciones manuales.

---

### Arquitectura

```
SAP Business One (libro diario)
        ↓  Extracción incremental diaria
   Capa RAW (copia exacta de SAP)
        ↓  ETL automático
   Capa MART (estados financieros)
        ↓  API segura por tenant
   DataBision Finance Dashboard
```

- **Tenant isolation:** cada empresa ve solo sus propios datos
- **Sign convention:** revenue positivo, costos positivos — listo para el CFO
- **Clasificación configurable:** el contador define qué cuenta va a qué línea del estado financiero
- **RLS en Power BI:** la misma seguridad aplica para todos los módulos

---

### Valor diferencial vs. reportes SAP estáticos

| | SAP estático | DataBision Finance |
|---|---|---|
| Actualización | Manual, semanal/mensual | Automática, diaria |
| Formato | Exportación cruda | Estado financiero ejecutivo |
| Acceso | Requiere SAP user | Navegador, cualquier dispositivo |
| Clasificación IFRS/local | Manual en Excel | Configurable por empresa |
| Validación contable | Ninguna | Health score automático |
| Tiempo CFO / mes | 4–8 horas | < 15 minutos |

---

### Implementación

1. **Configuración inicial (1 sesión):** clasificar cuentas contables con el contador del cliente
2. **Extracción histórica (1 vez):** OACT + 24 meses de OJDT
3. **Validación (1 sesión):** CFO revisa P&L + Balance + EBITDA vs. reporte manual
4. **Go-live:** extracción diaria automática, dashboard disponible

**Tiempo total de onboarding:** 2–5 días hábiles dependiendo de la complejidad del plan de cuentas.

---

### Contacto

DataBision — campillayparedes@gmail.com
