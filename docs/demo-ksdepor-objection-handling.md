# Demo KSDEPOR — Manejo de Objeciones

Sprint 8P — Junio 2026

Las 12 objeciones más comunes en demos de DataBision con clientes SAP Business One, con respuestas preparadas y argumentos de cierre.

---

## Objeción 1: "SAP ya tiene reportes integrados"

**Versión típica:** "Nosotros ya tenemos Crystal Reports / reportes de SAP. ¿Por qué necesitaríamos esto?"

**Respuesta:**
> "Los reportes de SAP son excelentes para operaciones — para imprimir una orden, ver el historial de un cliente. Lo que DataBision hace es diferente: agrega datos de múltiples módulos y los presenta para análisis de tendencias y comparación. Crystal Reports no tiene dashboards interactivos donde pueda filtrar por fecha, ordenar por proveedor y ver la evolución de cartera vencida en el tiempo. Eso es lo que hacemos nosotros."

**Argumento de cierre:** Mostrar el tab de Fulfillment en Ventas — ningún Crystal Report estándar cruza pedidos, entregas y tasa de cumplimiento en una sola pantalla sin desarrollo personalizado.

---

## Objeción 2: "Podemos hacer esto con Excel"

**Versión típica:** "Nuestro analista ya exporta datos de SAP a Excel. Esto no agrega mucho."

**Respuesta:**
> "El problema de Excel no es la herramienta — es el proceso. ¿Cuánto tiempo tarda el analista en preparar ese reporte? ¿Está disponible el lunes a las 8am cuando el gerente lo necesita? ¿Qué pasa cuando el analista está de vacaciones? DataBision elimina ese cuello de botella: los datos están siempre actualizados, disponibles para cualquier gerente con acceso, sin depender de una persona."

**Argumento de cierre:** "¿Cuántas horas a la semana invierte su equipo en generar esos reportes? Multiplíquelo por el costo de la persona. DataBision tiene ese costo cubierto en el ROI del primer mes."

---

## Objeción 3: "¿La seguridad? ¿Mis datos en la nube?"

**Versión típica:** "No me siento cómodo con los datos financieros fuera de nuestros servidores."

**Respuesta:**
> "Entendemos la preocupación — es legítima. Los datos se almacenan en Supabase (PostgreSQL) en Azure, con cifrado en reposo y en tránsito. El acceso es por JWT con firma RS256 y tokens de refresco rotados. Cada empresa tiene total aislamiento de datos — el modelo de tenancy garantiza que los datos de KSDEPOR nunca sean accesibles por otro cliente, incluso a nivel de query."

> "Nuestro modelo es similar al de cualquier ERP en la nube — Salesforce, HubSpot, SAP Cloud. Si están cómodos con esas plataformas, DataBision tiene el mismo nivel de garantías."

**Argumento de cierre:** "Podemos revisar la arquitectura técnica con su equipo de IT antes de firmar. Tenemos documentación completa del modelo de seguridad."

---

## Objeción 4: "¿Qué pasa si mi SAP cambia?"

**Versión típica:** "Estamos actualizando SAP / cambiando la versión. ¿Esto seguiría funcionando?"

**Respuesta:**
> "El extractor usa la Service Layer de SAP Business One — la API oficial de SAP, que es estable entre versiones. Si cambian de versión SAP, el equipo de DataBision valida la compatibilidad y actualiza el extractor sin costo adicional en el plan de soporte."

**Argumento de cierre:** "Hemos diseñado el sistema pensando en que SAP evoluciona. El contrato de soporte incluye actualizaciones por cambios en la versión de SAP."

---

## Objeción 5: "El precio es muy alto"

**Versión típica:** "¿Por cuánto sale esto? No creo que podamos justificarlo."

**Respuesta:**
> "El modelo de precio para el pilot es por empresa — no por usuario. Todos en la empresa pueden acceder. Y pensemos en el ROI: si esto reemplaza 5 horas semanales de un analista junior que prepara reportes, el costo del pilot se recupera en el primer trimestre."

**Argumento de cierre:** "El pilot es un compromiso de 90 días — no es un contrato de 3 años. Si en 90 días no ven el valor, no renuevan. Es un riesgo bajo para ver si esto funciona en su operación."

> Ver propuesta detallada en `docs/databision-ksdepor-pilot-scope.md`.

---

## Objeción 6: "¿Cuánto tiempo toma implementar?"

**Versión típica:** "No tenemos tiempo para un proyecto de implementación larga."

**Respuesta:**
> "El pilot toma 2-3 semanas desde la firma. Lo que necesitamos de ustedes: credenciales de Service Layer de SAP (acceso de lectura solamente), acceso a internet desde el servidor SAP, y 1-2 horas para validar los datos iniciales. El resto lo hace el equipo de DataBision."

**Argumento de cierre:** "No hay instalación de software en sus servidores, no hay consultores en su oficina durante semanas. Es todo remoto."

---

## Objeción 7: "¿Qué datos necesitan de SAP?"

**Versión típica:** "¿Qué información van a extraer? ¿Pueden acceder a todo?"

**Respuesta:**
> "El acceso es de lectura solamente — DataBision nunca escribe ni modifica datos en SAP. Extraemos objetos específicos: facturas de venta (OINV), órdenes de compra (OPOR), recepciones (OPDN), artículos (OITM), clientes (OCRD), proveedores (OCRD), y algunos maestros más. Solo los necesarios para los módulos contratados."

**Argumento de cierre:** "El catalogo completo de objetos que accedemos está en la documentación técnica. Su equipo SAP puede revisarlo antes de autorizar."

---

## Objeción 8: "¿Power BI no hace lo mismo?"

**Versión típica:** "Ya tenemos licencias de Power BI. ¿Por qué no usarlo directamente?"

**Respuesta:**
> "Power BI es una herramienta de visualización excelente. Lo que DataBision agrega es el pipeline de datos: la extracción automática desde SAP, la transformación, el almacenamiento estructurado y la validación de calidad. Power BI necesita que los datos ya estén limpios y accesibles — eso es lo que nosotros hacemos."

> "De hecho, DataBision es compatible con Power BI como capa adicional: los mismos datos que alimentan nuestros dashboards pueden alimentar reportes de Power BI de su equipo. No son excluyentes."

**Argumento de cierre:** "El costo de conectar Power BI directamente a SAP de forma confiable y actualizada es mayor que usar DataBision como capa de datos."

---

## Objeción 9: "¿Qué pasa con los datos históricos?"

**Versión típica:** "Llevamos 10 años en SAP. ¿Pueden cargar todo el historial?"

**Respuesta:**
> "Sí. La carga histórica se configura con una fecha de inicio — pueden ser 12 meses, 3 años o todo el historial disponible en SAP. La carga inicial tarda más (puede ser 1-2 días dependiendo del volumen), y luego el sistema corre en modo incremental diariamente."

**Argumento de cierre:** "Para la demo ven datos de prueba. En el pilot, cargaríamos su historial real — eso es lo que hace que los análisis de tendencias sean realmente útiles."

---

## Objeción 10: "¿Y si DataBision deja de existir?"

**Versión típica:** "¿Qué garantía tenemos de que esto seguirá existiendo? Son una empresa nueva."

**Respuesta:**
> "Es una pregunta legítima. Los datos de KSDEPOR viven en una base de datos PostgreSQL estándar — no en un formato propietario. Si DataBision desapareciera, tienen acceso directo a sus datos y pueden portarlos a cualquier otra herramienta. No hay lock-in tecnológico."

**Argumento de cierre:** "El contrato incluye una cláusula de portabilidad de datos — en cualquier momento pueden solicitar un export completo de sus datos en formato CSV."

---

## Objeción 11: "¿Está integrado con otras herramientas?"

**Versión típica:** "¿Esto se conecta con nuestro CRM / ERP / plataforma de e-commerce?"

**Respuesta:**
> "En esta etapa, DataBision se conecta exclusivamente con SAP Business One. Las integraciones con otras fuentes (CRM, e-commerce, planillas de Excel) están en el roadmap para el segundo semestre 2026. El valor principal es extraer todo el potencial de SAP que ya tienen."

**Argumento de cierre:** "El punto de partida son los datos de SAP — que ya tienen y que son los más críticos para la operación. Integraciones adicionales se agregan sobre esa base."

---

## Objeción 12: "¿Podemos personalizar los dashboards?"

**Versión típica:** "Necesitamos un reporte específico que esto no tiene. ¿Pueden hacerlo?"

**Respuesta:**
> "Los módulos base cubren los KPIs más comunes en distribución. Para reportes específicos a la operación de KSDEPOR, tenemos dos caminos: si el dato está en SAP y en nuestro modelo de datos, podemos agregar el indicador en el sprint siguiente. Si es un análisis muy particular, evaluamos el desarrollo como proyecto adicional."

**Argumento de cierre:** "¿Qué reporte específico necesitarían que no vieron en la demo? Lo evaluamos y les decimos si es viable para el pilot."

---

## Resumen de respuestas clave

| Objeción | Argumento central |
|---|---|
| "SAP ya tiene reportes" | DataBision es análisis agregado, no operación |
| "Usamos Excel" | Elimina el cuello de botella de la persona |
| "Seguridad en la nube" | ISO, cifrado, aislamiento total por tenant |
| "SAP va a cambiar" | Service Layer es la API oficial, estable entre versiones |
| "Precio alto" | ROI en primer trimestre, pilot de bajo riesgo |
| "Implementación larga" | 2-3 semanas, sin instalación en sus servidores |
| "¿Qué datos acceden?" | Solo lectura, catálogo documentado, auditado |
| "Power BI lo hace" | DataBision es el pipeline de datos, no excluyente |
| "Historial antiguo" | Carga configurable desde cualquier fecha |
| "¿Y si dejan de existir?" | Datos en PostgreSQL estándar, portabilidad garantizada |
| "Integración con otras tools" | SAP es el primer paso, roadmap Q3 2026 |
| "Necesitamos reportes custom" | Evaluamos en el pilot, posible sprint adicional |
