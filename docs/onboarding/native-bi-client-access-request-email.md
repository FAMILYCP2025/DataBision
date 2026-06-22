# Native BI Finance — Correo de Solicitud de Acceso SAP

**Sprint 29 · DataBision · Junio 2026**  
**Uso:** Enviar a TI del cliente antes de iniciar la implementación

---

## Correo a TI del cliente (técnico)

**Asunto:** DataBision — Requisitos técnicos de acceso SAP B1 para implementación Native BI Finance

---

Hola [Nombre TI],

Para iniciar la implementación del módulo Native BI Finance de DataBision necesito que nos proporcionen la siguiente información de acceso SAP Business One. Todo el acceso es de solo lectura — no se crea ni modifica ningún dato en SAP.

**Información requerida:**

**1. SAP Service Layer**
- URL completa del Service Layer:  
  `https://[host]:[puerto]/b1s/v1`  
  (ejemplo: `https://192.168.1.100:50000/b1s/v1`)

- CompanyDB (nombre de la base de datos SAP):  
  (ejemplo: `CLTSTKSDEPOR`)

- Ambiente: ☐ TST (Test) ☐ PRD (Producción)

**2. Usuario SAP de solo lectura**

Necesitamos que creen un usuario SAP con las siguientes características:
- Username sugerido: `DATABISION_RO`
- Permisos: Solo lectura sobre Plan de Cuentas (OACT) y Asientos Contables (OJDT)
- Sin permisos de creación, modificación ni administración

Cuando el usuario esté creado, envíen el password por un canal seguro (preferiblemente mensaje directo por WhatsApp o Signal, no por email).

**3. Certificado SSL**
- ¿El Service Layer tiene certificado SSL válido (CA reconocida)?  
  ☐ Sí, certificado válido  
  ☐ No, es autofirmado  
  ☐ No tiene SSL

En entorno TST con certificado autofirmado está bien — documentamos la excepción. Para PRD necesitamos SSL válido.

**4. Firewall**

El servidor donde instalaremos el extractor DataBision necesita poder conectarse a:
- SAP Service Layer: `[HOST_SAP]:[PUERTO_SAP]` (TCP)
- DataBision API: `https://[API_URL]` (HTTPS, puerto 443)

¿Necesitan la IP del servidor extractor para whitelist? Confirmamos apenas levantemos el servidor.

**5. Ventana horaria**

¿Cuál es el horario en que SAP tiene menos actividad de usuarios?  
El extractor corre automáticamente fuera de horario de oficina (sugerimos 2am–4am). ¿Hay algún mantenimiento SAP programado en ese horario?

---

**Contacto para coordinación técnica:**  
Jonathan Campillay — campillayparedes@gmail.com

**Tiempo estimado para proporcionar acceso:** 1–2 días hábiles

Avísanos si tienen alguna consulta o si necesitan más información sobre qué hace exactamente el extractor.

Gracias,  
Jonathan Campillay  
DataBision

---

## Correo a Finanzas del cliente (no técnico)

**Asunto:** DataBision — Coordinación sesión de clasificación contable

---

Hola [Nombre Contador / Jefe de Finanzas],

Junto al equipo de TI estamos preparando los accesos técnicos para la implementación de DataBision. Me escribo directamente a usted para coordinar la parte financiera.

**¿Qué necesitamos de su parte?**

1. **Sesión de clasificación contable (2 horas)**  
   En esta sesión revisamos el plan de cuentas de SAP y definimos juntos qué cuenta va al Estado de Resultados, al Balance o al EBITDA. Usted es quien decide — el sistema implementa exactamente lo que indique.  
   ¿Cuándo tendría disponibilidad? Sugerimos realizarla en los días 5–6 de la implementación.

2. **Período de validación**  
   Para validar que los datos están correctos, necesitamos que nos compartan (cuando lleguemos a esa etapa) el P&L o Estado de Resultados del período que usaremos como referencia.  
   ¿Qué período sugiere? (ej: Diciembre 2025 o el trimestre más reciente cerrado)

3. **Sesión de validación financiera (90 minutos)**  
   Al final de la implementación, revisamos juntos el P&L, Balance y EBITDA generados por DataBision vs. los reportes que usted ya tiene. Usted aprueba o nos indica las diferencias a corregir.

---

**No necesitamos de su parte:**
- Exportaciones manuales de SAP
- Archivos Excel
- Acceso a ningún sistema

El extractor obtiene los datos directamente de SAP. La sesión con usted es para asegurarnos de que la clasificación contable es correcta.

¿Tiene disponibilidad para la semana [SEMANA_SUGERIDA]?

Saludos,  
Jonathan Campillay  
DataBision — campillayparedes@gmail.com

---

## Checklist post-envío

- [ ] Correo enviado a TI — fecha: ________________
- [ ] Correo enviado a Finanzas — fecha: ________________
- [ ] Respuesta TI recibida con URL SL — fecha: ________________
- [ ] CompanyDB confirmada — fecha: ________________
- [ ] Usuario SAP creado — fecha: ________________
- [ ] Password SAP recibido por canal seguro — fecha: ________________
- [ ] Firewall confirmado — fecha: ________________
- [ ] Sesión clasificación agendada — fecha: ________________
- [ ] Sesión validación agendada — fecha: ________________
