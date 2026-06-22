# Native BI Finance — Guion Demo Ejecutivo (20 minutos)

**Audiencia:** CFO, Gerente Financiero, Gerente General  
**Formato:** Pantalla compartida — dashboard en vivo + conversación  
**Evidencia base:** Validación TST ksdepor — healthScore 100, 7/7 endpoints HTTP 200

---

## Preparación previa (10 min antes)

- [ ] API local corriendo: `dotnet run --project src/DataBision.Api`
- [ ] Frontend dev server: `npm run dev` (databision-frontend)
- [ ] Dashboard abierto en navegador: `http://localhost:5173?tenant=ksdepor`
- [ ] Todos los endpoints respondiendo HTTP 200
- [ ] refresh-status mostrando última actualización reciente
- [ ] Borrar historial del navegador visible

---

## 0–3 min: El dolor del cliente

**Objetivo:** hacer que el cliente diga "eso es exactamente lo que me pasa"

**Guion:**

> "¿Cuánto tiempo pasa usted o su equipo cada mes armando el estado de resultados en Excel? ¿Cuántas veces han tenido una reunión de directorio con un P&L que luego resultó tener un error de fórmula?"

*(Pausa. Dejar que el cliente responda.)*

> "Lo que vemos en la mayoría de empresas SAP B1 es que el sistema tiene toda la información — los asientos están ahí, el libro diario está completo — pero nadie puede verlos en formato gerencial sin pasar por el contador o por TI."

> "Eso es exactamente lo que Native BI Finance resuelve. ¿Le parece si lo veo en pantalla directamente?"

**Señales positivas a escuchar:**
- "Sí, tardamos días en cerrar el mes"
- "TI nos genera reportes estáticos"
- "Nuestro contador hace todo en Excel"

---

## 3–6 min: Arquitectura simple

**Objetivo:** demostrar que no toca SAP, que es seguro, que es simple

**Guion:**

> "Antes de mostrar el dashboard, quiero explicarle en 2 minutos cómo funciona para que esté tranquilo con la seguridad."

*(Mostrar diagrama o describir verbalmente:)*

> "Tenemos un extractor que se conecta a SAP Business One usando un usuario de solo lectura — exactamente como si fuera un usuario que solo puede consultar. No escribe, no modifica, no toca transacciones. Solo lee el plan de cuentas y el libro diario."

> "Esos datos van a nuestra base de datos segura, se clasifican automáticamente según el PCGE, y aparecen en el dashboard que le voy a mostrar ahora."

> "El proceso corre diariamente de forma automática — programado a las 2am para no interferir con la operación."

**Puntos a enfatizar:**
- Usuario SAP solo lectura — nada se modifica
- Credenciales nunca expuestas en el dashboard
- Cada cliente tiene sus datos completamente aislados

---

## 6–12 min: El dashboard financiero

**Objetivo:** mostrar valor real, datos reales, no mocks

**Secuencia de pantallas:**

### 6:00 — Estado de Resultados (P&L)

*(Navegar a Finance Dashboard → P&L)*

> "Este es el Estado de Resultados directo desde SAP. Aquí ve ingresos, costos, margen bruto, gastos operativos, utilidad neta. Todo calculado automáticamente desde los asientos del libro diario."

> "Puede filtrar por período — vea, cambio a enero, a febrero, al trimestre completo."

*(Demostrar filtro de período)*

> "¿Este formato se parece al que trabaja con su contador?"

### 8:00 — Balance General

*(Navegar a Balance Sheet)*

> "El balance muestra activos, pasivos y patrimonio. El sistema valida automáticamente que cuadre — si hay una diferencia, aparece una alerta aquí mismo."

*(Señalar el indicador de balance cuadrado)*

### 9:30 — EBITDA

*(Navegar a EBITDA)*

> "El EBITDA lo calcula a partir de la utilidad operativa ajustando depreciación y amortización según las cuentas que el contador identifique. Es configurable por empresa."

### 10:30 — Plan de Cuentas con clasificación

*(Navegar a Account Classification o similar)*

> "Aquí puede ver cómo está clasificada cada cuenta del plan de cuentas de SAP. Si hay cuentas sin clasificar, aparecen señaladas para que las revisen con el contador."

---

## 12–15 min: Refresh-status y trazabilidad

**Objetivo:** mostrar operación confiable, no una demo frágil

*(Navegar a refresh-status o health check)*

**Guion:**

> "Una pregunta que siempre surge: ¿cómo sé si los datos están actualizados? Aquí está la respuesta."

*(Mostrar endpoint o UI de refresh-status)*

> "Este indicador muestra cuándo fue la última extracción, cuántas cuentas se procesaron, cuántos asientos, y si hubo algún error. Es la trazabilidad completa del proceso."

> "Si algún día la extracción falla — digamos por mantenimiento de SAP — el sistema lo registra aquí y ustedes o nosotros podemos correr el proceso manualmente con un solo comando."

**Métricas a mencionar:**
- Última extracción: fecha y hora
- OACT procesado: N cuentas
- OJDT procesado: N asientos
- MART actualizado: fecha
- Estado general: healthy / warning / error

---

## 15–18 min: Seguridad y no-modificación de SAP

**Objetivo:** disipar la objeción de seguridad antes de que la planteen

**Guion:**

> "Quiero ser muy explícito sobre seguridad porque sé que es importante cuando hay un sistema externo interactuando con SAP."

> "Primero: el usuario SAP que usamos tiene permisos de solo lectura sobre el libro contable. No puede crear asientos, no puede modificar documentos, no puede aprobar nada."

> "Segundo: las credenciales de ese usuario SAP nunca viajan al navegador. Están en el servidor de extracción, encriptadas como variable de entorno o en Azure Key Vault. Nadie que use el dashboard puede ver esas credenciales."

> "Tercero: cada empresa tiene sus propios datos completamente aislados en nuestra base de datos. El sistema valida en cada consulta que solo se acceda a los datos del tenant correcto."

> "Y cuarto: si en algún momento deciden discontinuar el servicio, los datos de extracción se eliminan y la conexión SAP se desactiva. No retenemos datos contables."

---

## 18–20 min: Propuesta piloto

**Objetivo:** cerrar con un siguiente paso concreto y de bajo riesgo

**Guion:**

> "Lo que acabaron de ver es exactamente lo que entregaríamos para su empresa, con sus propios datos de SAP."

> "La propuesta es un piloto de 30 días: implementamos el sistema completo con sus datos reales, validamos los estados financieros junto a su contador, y al final del mes tienen un dashboard operativo funcionando."

> "El costo del piloto es [USD 800–1,200] — pago único. No hay contrato de largo plazo en esta etapa. Si no les convence, no continúan. Si les convence, pasamos a suscripción mensual."

> "Lo que necesitamos de su parte para empezar: un usuario SAP de solo lectura, la URL del Service Layer, y una ventana de 2 horas con su contador para revisar la clasificación de cuentas."

> "¿Tienen acceso al Service Layer de SAP? ¿Trabajan con ambiente de prueba o producción?"

**Siguiente paso a acordar:**
- Fecha de llamada técnica (30 min con TI)
- Fecha de revisión con contador (2 horas)
- Firma de NDA si aplica
- Fecha de inicio: 48 horas después de credenciales

---

## Cierre

> "¿Alguna pregunta? ¿Hay algo del proceso que les preocupe antes de avanzar?"

*(Dejar espacio para objeciones — ver native-bi-finance-objection-handling.md)*

---

## Notas post-demo

- Enviar one-pager dentro de las 2 horas siguientes
- Enviar propuesta de piloto dentro de las 24 horas
- Agendar llamada técnica dentro de los próximos 3 días
