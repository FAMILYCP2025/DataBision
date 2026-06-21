# DataBision Native BI Finance — Email de Propuesta (Template)

**Sprint:** 21F  
**Fecha:** 2026-06-20  
**Uso:** Template para primer contacto post-demo

---

## Asunto del email

```
DataBision — Propuesta piloto reportería financiera SAP B1 [EMPRESA]
```

---

## Cuerpo del email

```
Estimado/a [NOMBRE],

Muchas gracias por su tiempo en la demo de hoy.

Como vimos, DataBision conecta directamente a SAP Business One y genera 
automáticamente su Estado de Resultados, Balance General y EBITDA, clasificados 
según PCGE, sin ninguna intervención manual.

---

PROPUESTA PILOTO — [EMPRESA]

Recomendamos comenzar con el Piloto 2 semanas (Opción B):

• Precio: S/ 4,500 + IGV
• Duración: 10 días hábiles desde el inicio del trabajo técnico
• Incluye:
  - Conexión a su SAP B1 ([COMPANYDB])
  - Dashboard financiero con sus datos reales
  - Clasificación PCGE revisada con su contador
  - Export CSV para Excel
  - Actualización semanal automática
  - 2 sesiones de capacitación

• No incluye: Modificación de SAP, auditoría contable, garantía de exactitud
  sin validación de su contador.

---

PRÓXIMOS PASOS (si desean continuar)

1. Confirmar que tienen acceso a SAP Business One Service Layer
   (URL, usuario con permisos de solo lectura)
2. Coordinar disponibilidad del contador para la validación del Día 2 (2 horas)
3. Firmar la propuesta → iniciamos en [FECHA_INICIO]

---

PREGUNTAS FRECUENTES

¿Modifican datos en SAP? 
No. Solo lectura vía Service Layer REST API.

¿El balance cuadrará?
Si SAP tiene asientos de cierre de ejercicio, sí. Si no, el patrimonio aparecerá 
en cero (comportamiento normal de SAP sin asientos patrimoniales).

¿Puedo exportar a Excel?
Sí. Estado de Resultados, Balance, EBITDA y Plan de Cuentas tienen export CSV.

---

Quedo a disposición para cualquier consulta.

Saludos,
[NOMBRE_CONSULTOR]
DataBision
[EMAIL] | [TELÉFONO]
```

---

## Variante — Para cliente que solo quiere ver si conecta primero

**Asunto:**
```
DataBision — Diagnóstico técnico SAP B1 [EMPRESA] (5 días, S/ 1,500)
```

**Cuerpo alternativo (sección propuesta):**
```
PROPUESTA DIAGNÓSTICO TÉCNICO — [EMPRESA]

Para validar técnicamente antes de comprometerse con el piloto completo:

• Precio: S/ 1,500 + IGV
• Duración: 5 días hábiles
• Incluye:
  - Validación conectividad SAP B1 Service Layer
  - Primera extracción de datos (OACT + Libro Diario)
  - Dashboard con sus datos reales (acceso 7 días)
  - Informe técnico: cuentas detectadas, clasificación PCGE automática
  - Recomendación Go/No-Go para piloto completo

Si el diagnóstico es exitoso, el monto se descuenta del Piloto 2 semanas (Opción B).
```
