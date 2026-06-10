# DataBision Native BI — Limitaciones Conocidas (Demo v1)

Este documento es para uso interno. Documenta honestamente qué NO está implementado aún, para responder con precisión si un prospecto pregunta durante una demo.

---

## Dashboard

| Limitación | Estado | Roadmap |
|---|---|---|
| Solo 30 días fijos — sin selector de período | Falta implementar | Fase 2 |
| Sin comparación vs período anterior | Falta implementar | Fase 2 |
| Sin DonutChart de ventas por categoría | Falta implementar | Fase 2 |
| Top clientes: solo 5 fijos, sin paginación | Falta implementar | Fase 2 |
| Sin sparklines de tendencia en KPI cards | Falta implementar | Fase 2 |

## Ventas

| Limitación | Estado | Roadmap |
|---|---|---|
| Sin gráfico de tendencia (solo tablas) | Falta implementar | Fase 2 |
| Sin drilldown de cliente/vendedor (perfil) | Falta implementar | Fase 2 |
| Sin exportación a CSV / Excel | Falta implementar | Fase 2 |
| Sin filtro por vendedor o categoría | Falta implementar | Fase 2 |
| Paginación sin "saltar a página N" | Limitación conocida | Baja prioridad |

## Módulos no disponibles aún

| Módulo | Estado |
|---|---|
| Clientes / CxC (cuentas por cobrar) | No implementado — Fase 3 |
| Inventario / Productos | No implementado — Fase 3 |
| Vendedores (perfil detallado) | No implementado — Fase 2 |
| Compras / Proveedores | No implementado — Fase 4 |
| Flujo de Caja | No implementado — Fase 4 |
| Margen / Rentabilidad | No implementado — Fase 3 |
| Cockpit gerencial (resumen multi-módulo) | No implementado — Fase 2 |

## Funcionalidades transversales

| Funcionalidad | Estado |
|---|---|
| Exportación a PDF | No implementado |
| Modo dark | No implementado |
| Mobile app nativa (iOS/Android) | No planificado — web responsiva solamente |
| Alertas por email / WhatsApp | No implementado — Fase 3 |
| Drill-down a documentos SAP originales | No implementado — Fase 4 |
| Multi-empresa (consolidado) | No implementado — Fase 4 |
| Configuración de KPIs por el usuario | No implementado — Fase 4 |

## Datos y extractor

| Limitación | Detalle |
|---|---|
| Latencia de datos | Los datos se actualizan con el ciclo del extractor (mínimo cada 15 min en producción). No son "en tiempo real" desde SAP. |
| Solo SAP Business One | No hay conectores para otros ERP aún. |
| Solo datos de ventas e inventario básico | Módulos financieros avanzados (contabilidad, nómina) están fuera del alcance actual. |
| Historial limitado al período extraído | El primer run del extractor define el punto de inicio del historial. |

---

## Cómo responder a preguntas sobre limitaciones

**Si preguntan "¿puedo exportar a Excel?"**
> *"Exportación CSV está en nuestra hoja de ruta para el próximo trimestre. Por ahora los datos están en pantalla con ordenamiento y paginación."*

**Si preguntan "¿puedo ver las cuentas por cobrar?"**
> *"Módulo de clientes y CxC está en la hoja de ruta — es el siguiente después de ventas. Si te interesa eso específicamente, podemos priorizar."*

**Si preguntan "¿los datos son en tiempo real?"**
> *"Se actualizan cada 15 minutos automáticamente. No es en tiempo real milisegundo a milisegundo como SAP, pero para decisiones de negocio 15 minutos es completamente suficiente — y además el sistema te avisa si la sincronización falla."*

**Si preguntan algo que no está ni en roadmap**
> *"Eso no está en nuestra hoja de ruta actual. Podemos anotarlo — si varios clientes lo piden, sube en prioridad."*

---

*Documento interno — no compartir con prospectos — versión 1.0 — 2026-06-08*
